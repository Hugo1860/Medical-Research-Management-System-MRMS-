using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using ScientificResearch.Controllers;
using ScientificResearch.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ScientificResearch.Infrastucture;

// Runs the production worker against isolated PostgreSQL tables and a local
// in-memory HttpMessageHandler. No supplier request is sent. The caller must
// explicitly select a disposable test database; credentials are never printed.
var database = Environment.GetEnvironmentVariable("EXTERNAL_SYNC_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(database)
    || Environment.GetEnvironmentVariable("EXTERNAL_SYNC_TEST_ALLOW_SCHEMA_CREATE") != "YES")
{
    Console.Error.WriteLine("Set EXTERNAL_SYNC_TEST_CONNECTION for a disposable database and EXTERNAL_SYNC_TEST_ALLOW_SCHEMA_CREATE=YES.");
    return 2;
}

var schema = "external_sync_regression_" + Guid.NewGuid().ToString("N");
var connectionString = new NpgsqlConnectionStringBuilder(database) { SearchPath = schema, Pooling = false }.ConnectionString;
await using var admin = new NpgsqlConnection(database);
await admin.OpenAsync();
await admin.ExecuteAsync($"CREATE SCHEMA \"{schema}\";");
var controllerServices = new List<ServiceProvider>();
try
{
    await using var db = new NpgsqlConnection(connectionString);
    await db.OpenAsync();
    await db.ExecuteAsync("""
        CREATE TABLE "医院运营配置" ("编号" integer PRIMARY KEY, "运营模式" text, "启用功能" jsonb);
        CREATE TABLE "角色" ("编号" integer PRIMARY KEY, "名称" text);
        CREATE TABLE "人员角色" ("人员编号" integer, "角色编号" integer);
        INSERT INTO "角色" VALUES (1, '系统管理员');
        INSERT INTO "人员角色" VALUES (1, 1);
        CREATE TABLE "外部系统" (
            "编号" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            "系统类型" text NOT NULL, "系统代码" text NOT NULL, "系统名称" text NOT NULL DEFAULT 'regression',
            "建立人编号" integer,
            "基础地址" text NOT NULL, "认证方式" text NOT NULL, "认证引用" text,
            "超时秒数" integer NOT NULL, "最大重试次数" integer NOT NULL,
            "是否启用" boolean NOT NULL, "接入方式" text NOT NULL,
            "状态代码" text NOT NULL CHECK ("状态代码" IN ('connected','failed','unconfigured','manual_import')),
            "最近连接状态" text NOT NULL CHECK ("最近连接状态" IN ('已连接','失败','未测试')),
            "最近连接时间" timestamp, "最近连接错误" text,
            "更新时间" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE "外部同步任务" (
            "编号" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            "外部系统编号" bigint NOT NULL REFERENCES "外部系统"("编号"),
            "业务类型" text NOT NULL, "方向" text NOT NULL, "业务主键" text NOT NULL,
            "幂等键" text NOT NULL UNIQUE, "请求负载" jsonb NOT NULL,
            "状态" text NOT NULL CHECK ("状态" IN ('待处理','处理中','成功','失败')),
            "尝试次数" integer NOT NULL DEFAULT 0, "最大重试次数" integer NOT NULL,
            "请求ID" text, "下次重试时间" timestamp, "最后错误" text,
            "外部响应摘要" text, "完成时间" timestamp,
            "创建时间" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "更新时间" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        """);

    foreach (var blocked in new[] { "unconfigured", "failed", "disabled", "manual_import" })
    {
        var handler = new ResponseSequence(HttpStatusCode.OK);
        await ResetAsync();
        var systemId = await SeedAsync();
        await db.ExecuteAsync("""
            UPDATE "外部系统" SET "状态代码"=@State,"是否启用"=@Enabled,"接入方式"=@Mode WHERE "编号"=@Id;
            """, new { State = blocked == "disabled" ? "connected" : blocked, Enabled = blocked != "disabled", Mode = blocked == "manual_import" ? "人工导入" : "接口", Id = systemId });
        var worker = CreateWorker(handler);
        Check(!(bool)(await InvokeAsync(worker, "HasCompleteOutboundConfigurationAsync", CancellationToken.None))!, $"startup rejects {blocked}");
        Check(await InvokeAsync(worker, "ClaimAsync", CancellationToken.None) == null, $"claim rejects {blocked}");
        Check(handler.Attempts == 0, $"no outbound call for {blocked}");
        Console.WriteLine($"PASS connection gate: {blocked}");
    }

    foreach (var transient in new[] { "503", "429", "408", "network", "timeout" })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        var handler = transient switch
        {
            "network" => new ResponseSequence(new HttpRequestException("synthetic network interruption"), HttpStatusCode.OK),
            "timeout" => new ResponseSequence(new TaskCanceledException("synthetic request timeout"), HttpStatusCode.OK),
            _ => new ResponseSequence((HttpStatusCode)int.Parse(transient), HttpStatusCode.OK)
        };
        var worker = CreateWorker(handler);
        var work = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
        Check(work != null, $"claim before {transient}");
        await InvokeAsync(worker, "ExecuteOneAsync", work!, CancellationToken.None);
        var pending = await ReadTaskAsync();
        Check(pending.State == "待处理" && pending.Attempts == 1 && pending.NextRetry != null && pending.CompletedAt == null, $"retry scheduled for {transient}");
        Check(await ReadSystemStateAsync(systemId) == "connected", $"connection verification retained for {transient}");
        Check(await InvokeAsync(worker, "ClaimAsync", CancellationToken.None) == null, $"backoff observed for {transient}");
        if (transient is "503" or "429" or "408")
            Check(pending.Error!.Contains("[REDACTED]") && !pending.Error.Contains("test-secret"), "failure diagnostics redact response credentials");
        await db.ExecuteAsync("UPDATE \"外部同步任务\" SET \"下次重试时间\"=CURRENT_TIMESTAMP - interval '1 second';");
        // Recreate the worker to prove eligibility survives a process restart,
        // rather than relying on an in-memory list of trusted connections.
        worker = CreateWorker(handler);
        Check((bool)(await InvokeAsync(worker, "HasCompleteOutboundConfigurationAsync", CancellationToken.None))!, $"restart readiness for {transient}");
        work = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
        Check(work != null, $"retry claim for {transient}");
        await InvokeAsync(worker, "ExecuteOneAsync", work!, CancellationToken.None);
        var done = await ReadTaskAsync();
        Check(done.State == "成功" && done.Attempts == 2 && done.CompletedAt != null, $"retry completes for {transient}");
        Check(handler.Attempts == 2 && handler.IdempotencyKeys.Distinct().Count() == 1 && handler.RequestIds.Distinct().Count() == 2, "retry retains idempotency key and renews request ID");
        Console.WriteLine($"PASS transient {transient}: queued -> backoff -> restart -> success");
    }

    foreach (var terminal in new[] { "400", "exhausted" })
    {
        await ResetAsync();
        var systemId = await SeedAsync(terminal == "exhausted" ? 1 : 3);
        var handler = new ResponseSequence(terminal == "400" ? HttpStatusCode.BadRequest : HttpStatusCode.ServiceUnavailable);
        var worker = CreateWorker(handler);
        var work = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
        await InvokeAsync(worker, "ExecuteOneAsync", work!, CancellationToken.None);
        var failed = await ReadTaskAsync();
        Check(failed.State == "失败" && failed.NextRetry == null && failed.CompletedAt != null, $"terminal completion for {terminal}");
        Check(await ReadSystemStateAsync(systemId) == "failed", "terminal failure closes connection gate");
        Check(await InvokeAsync(worker, "ClaimAsync", CancellationToken.None) == null && handler.Attempts == 1, "terminal task is not resent");
        Console.WriteLine($"PASS terminal {terminal}: explicit connection revalidation required");
    }

    foreach (var change in new[] { "unconfigured", "failed", "disabled", "manual_import" })
    {
        foreach (var response in new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest })
        {
            await ResetAsync();
            var systemId = await SeedAsync();
            var handler = new ResponseSequence(response);
            handler.OnSend = async () => await db.ExecuteAsync("""
                UPDATE "外部系统" SET "状态代码"=@State,"是否启用"=@Enabled,"接入方式"=@Mode WHERE "编号"=@Id;
                """, new { State = change == "disabled" ? "connected" : change, Enabled = change != "disabled", Mode = change == "manual_import" ? "人工导入" : "接口", Id = systemId });
            var worker = CreateWorker(handler);
            var work = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
            await InvokeAsync(worker, "ExecuteOneAsync", work!, CancellationToken.None);
            Check(await ReadSystemStateAsync(systemId) == (change == "disabled" ? "connected" : change), "in-flight completion does not overwrite an operator change");
            Console.WriteLine($"PASS in-flight {(int)response} response preserves {change}");
        }
    }

    foreach (var completion in new[] { "MarkSuccessAsync", "MarkFailureAsync" })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        var worker = CreateWorker(new ResponseSequence(HttpStatusCode.OK));
        var oldWork = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
        await db.ExecuteAsync("UPDATE \"外部同步任务\" SET \"请求ID\"='newer-lease-request';");
        if (completion == "MarkSuccessAsync")
            await InvokeAsync(worker, completion, oldWork!, "stale receipt", CancellationToken.None);
        else
            await InvokeAsync(worker, completion, oldWork!, "stale error", false, CancellationToken.None, null);
        var current = await ReadTaskAsync();
        Check(current.State == "处理中" && current.Error == null && current.CompletedAt == null, "stale completion cannot change a newer request lease");
        Check(await ReadSystemStateAsync(systemId) == "connected", "stale completion cannot close connection gate");
        Console.WriteLine($"PASS stale request ID fenced: {completion}");
    }

    foreach (var saveBeforeTest in new[] { false, true })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        SystemState? verified = null;
        var handler = new ResponseSequence(HttpStatusCode.BadRequest);
        handler.OnSend = async () =>
        {
            var controller = CreateController(new ResponseSequence(HttpStatusCode.OK));
            if (saveBeforeTest)
                Check(Status(await controller.保存外部系统(SaveModel(systemId))) == 200, "same-address configuration save succeeds");
            Check(Status(await controller.测试外部系统连接(new() { 编号 = systemId })) == 200, "same-address revalidation succeeds");
            verified = await ReadSystemAsync(systemId);
        };
        var worker = CreateWorker(handler);
        var oldWork = await InvokeAsync(worker, "ClaimAsync", CancellationToken.None);
        await InvokeAsync(worker, "ExecuteOneAsync", oldWork!, CancellationToken.None);
        var current = await ReadSystemAsync(systemId);
        Check(current.State == "connected" && current.UpdatedAt == verified!.UpdatedAt && current.VerifiedAt == verified.VerifiedAt && current.Error == null,
            "old terminal failure cannot overwrite the new verification generation");
        Check((await ReadTaskAsync()).State == "失败", "old task retains its real terminal failure");
        Console.WriteLine($"PASS same-address revalidation fences old delivery; save first: {saveBeforeTest}");
    }

    foreach (var oldResponse in new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        var handler = new ResponseSequence(oldResponse);
        handler.OnSend = async () =>
        {
            Check(await ReadSystemStateAsync(systemId) == "unconfigured", "connection test closes gate while verification is pending");
            var saved = await CreateController(new ResponseSequence()).保存外部系统(SaveModel(systemId, "http://127.0.0.1/new-unverified-address"));
            Check(Status(saved) == 200, "replacement configuration is saved while old test is in flight");
        };
        var result = await CreateController(handler).测试外部系统连接(new() { 编号 = systemId });
        var current = await ReadSystemAsync(systemId);
        Check(Status(result) == 409 && current.State == "unconfigured" && current.VerifiedAt == null && current.Error == null,
            "old connection test receipt is rejected without changing the saved configuration");
        Check(current.Address.EndsWith("/new-unverified-address") && handler.Addresses.Single().EndsWith("/regression-no-network"),
            "receipt belongs to the old address, never the newly saved address");
        Check(await InvokeAsync(CreateWorker(new ResponseSequence()), "ClaimAsync", CancellationToken.None) == null,
            "new address remains closed to outbound tasks");
        Console.WriteLine($"PASS address change fences old {(int)oldResponse} verification receipt");
    }

    foreach (var newestResponse in new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        SystemState? newest = null;
        var olderHandler = new ResponseSequence(newestResponse == HttpStatusCode.OK ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
        olderHandler.OnSend = async () =>
        {
            Check(Status(await CreateController(new ResponseSequence(newestResponse)).测试外部系统连接(new() { 编号 = systemId })) == 200,
                "newer connection test completes");
            newest = await ReadSystemAsync(systemId);
        };
        var older = await CreateController(olderHandler).测试外部系统连接(new() { 编号 = systemId });
        var current = await ReadSystemAsync(systemId);
        Check(Status(older) == 409 && current.State == newest!.State && current.UpdatedAt == newest.UpdatedAt && current.Error == newest.Error,
            "latest-started verification owns the result regardless of completion order");
        Console.WriteLine($"PASS overlapping connection tests preserve newest {(int)newestResponse} result");
    }

    foreach (var successFirst in new[] { true, false })
    {
        await ResetAsync();
        var systemId = await SeedAsync();
        await db.ExecuteAsync("""
            INSERT INTO "外部同步任务" ("外部系统编号","业务类型","方向","业务主键","幂等键","请求负载","状态","最大重试次数")
            SELECT "外部系统编号","业务类型","方向",'fixture-2','second-task-key',"请求负载",'待处理',"最大重试次数" FROM "外部同步任务";
            """);
        var successWorker = CreateWorker(new ResponseSequence(HttpStatusCode.OK));
        var failureWorker = CreateWorker(new ResponseSequence(HttpStatusCode.BadRequest));
        var successWork = await InvokeAsync(successWorker, "ClaimAsync", CancellationToken.None);
        var failureWork = await InvokeAsync(failureWorker, "ClaimAsync", CancellationToken.None);
        Check(successWork != null && failureWork != null, "two tasks claim one verification generation");
        if (successFirst)
        {
            await InvokeAsync(successWorker, "ExecuteOneAsync", successWork!, CancellationToken.None);
            await InvokeAsync(failureWorker, "ExecuteOneAsync", failureWork!, CancellationToken.None);
        }
        else
        {
            await InvokeAsync(failureWorker, "ExecuteOneAsync", failureWork!, CancellationToken.None);
            await InvokeAsync(successWorker, "ExecuteOneAsync", successWork!, CancellationToken.None);
        }
        Check(await ReadSystemStateAsync(systemId) == "failed", "terminal failure closes its generation regardless of concurrent task success order");
        Check(await db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"外部同步任务\" WHERE \"状态\" IN ('成功','失败');") == 2, "both task results persist independently");
        Console.WriteLine($"PASS concurrent delivery results preserve failure gate; success first: {successFirst}");
    }

    await ResetAsync();
    var precisionId = await SeedAsync();
    await db.ExecuteAsync("""
        UPDATE "外部系统" SET "更新时间"=timestamp '2100-01-01 00:00:00.123456' WHERE "编号"=@Id;
        """, new { Id = precisionId });
    var precisionController = CreateController(new ResponseSequence(HttpStatusCode.OK));
    var before = await ReadSystemAsync(precisionId);
    Check(Status(await precisionController.测试外部系统连接(new() { 编号 = precisionId }, dryRun: true)) == 200,
        "dry run checks configuration without sending a request");
    Check((await ReadSystemAsync(precisionId)).UpdatedAt == before.UpdatedAt, "dry run does not claim a verification generation");
    await precisionController.保存外部系统(SaveModel(precisionId));
    var savedPrecision = await ReadSystemAsync(precisionId);
    Check(savedPrecision.UpdatedAt == before.UpdatedAt.AddTicks(10), "save advances exactly one database microsecond despite a backward clock");
    await precisionController.测试外部系统连接(new() { 编号 = precisionId });
    Check((await ReadSystemAsync(precisionId)).UpdatedAt == savedPrecision.UpdatedAt.AddTicks(20), "test start and completion each advance generation with exact timestamp precision");
    var precisionWorker = CreateWorker(new ResponseSequence(HttpStatusCode.BadRequest));
    var precisionWork = await InvokeAsync(precisionWorker, "ClaimAsync", CancellationToken.None);
    await InvokeAsync(precisionWorker, "ExecuteOneAsync", precisionWork!, CancellationToken.None);
    Check(await ReadSystemStateAsync(precisionId) == "failed", "worker compares a timestamp with nonzero microseconds exactly");
    Console.WriteLine("PASS database timestamp precision and monotonic generation under backward clock");

    await ResetAsync();
    var manualId = await SeedAsync();
    await CreateController(new ResponseSequence()).保存外部系统(SaveModel(manualId, mode: "人工导入"));
    var manualBefore = await ReadSystemAsync(manualId);
    await CreateController(new ResponseSequence()).测试外部系统连接(new() { 编号 = manualId }, dryRun: true);
    var manualAfter = await ReadSystemAsync(manualId);
    Check(manualAfter.State == "manual_import" && manualAfter.UpdatedAt == manualBefore.UpdatedAt && manualAfter.VerifiedAt == null,
        "manual-import dry run remains read-only");
    Console.WriteLine("PASS manual-import dry run does not mutate status metadata");

    Console.WriteLine("External sync runtime regression passed: 31 scenarios; production controller and worker methods, isolated PostgreSQL schema, no supplier network calls.");
    return 0;

    ExternalSyncWorker CreateWorker(ResponseSequence handler) => new(
        new ConfigurationBuilder().Build(), new FixedConnectionFactory(connectionString),
        new HandlerClientFactory(handler), NullLogger<ExternalSyncWorker>.Instance);

    OperationsController CreateController(ResponseSequence handler)
    {
        var services = new ServiceCollection()
            .AddSingleton<IDatabaseConnectionFactory>(new FixedConnectionFactory(connectionString))
            .AddSingleton<IWebHostEnvironment>(new TestEnvironment())
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<IHttpClientFactory>(new HandlerClientFactory(handler))
            .BuildServiceProvider();
        controllerServices.Add(services);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items[KeycloakUserMiddleware.CurrentUserItemKey] = new CurrentUser { 编号 = 1, DbKey = "ScientificResearch_Model", 工号 = "external-regression" };
        return new OperationsController { ControllerContext = new ControllerContext { HttpContext = context } };
    }

    外部系统保存 SaveModel(long id, string address = "http://127.0.0.1/regression-no-network", string mode = "接口") => new()
    {
        编号 = id, 系统代码 = "HR", 系统名称 = "regression", 系统类型 = "HR", 基础地址 = address,
        接入方式 = mode, 认证方式 = "无", 是否启用 = true, 超时秒数 = 3, 最大重试次数 = 3
    };

    async Task<SystemState> ReadSystemAsync(long id) => await db.QuerySingleAsync<SystemState>("""
        SELECT "状态代码" AS "State","基础地址" AS "Address","更新时间" AS "UpdatedAt","最近连接时间" AS "VerifiedAt","最近连接错误" AS "Error"
        FROM "外部系统" WHERE "编号"=@Id;
        """, new { Id = id });

    async Task ResetAsync() => await db.ExecuteAsync("TRUNCATE \"外部同步任务\",\"外部系统\" RESTART IDENTITY;");

    async Task<long> SeedAsync(int maxAttempts = 3)
    {
        foreach (var type in new[] { "HR", "财务", "采购", "伦理", "合同", "人才库", "生物样本库", "论文采集" })
            await db.ExecuteAsync("""
                INSERT INTO "外部系统" ("系统类型","系统代码","基础地址","认证方式","认证引用","超时秒数","最大重试次数","是否启用","接入方式","状态代码","最近连接状态")
                VALUES (@Type,@Type,'http://127.0.0.1/regression-no-network','无',NULL,3,@MaxAttempts,TRUE,'接口','connected','已连接');
                """, new { Type = type, MaxAttempts = maxAttempts });
        var systemId = await db.QuerySingleAsync<long>("SELECT \"编号\" FROM \"外部系统\" WHERE \"系统类型\"='HR';");
        await db.ExecuteAsync("""
            INSERT INTO "外部同步任务" ("外部系统编号","业务类型","方向","业务主键","幂等键","请求负载","状态","最大重试次数")
            VALUES (@SystemId,'regression','出站','fixture-1','regression-idempotency-key','{}','待处理',@MaxAttempts);
            """, new { SystemId = systemId, MaxAttempts = maxAttempts });
        return systemId;
    }

    async Task<TaskState> ReadTaskAsync() => await db.QuerySingleAsync<TaskState>("""
        SELECT "状态" AS "State","尝试次数" AS "Attempts","下次重试时间" AS "NextRetry","最后错误" AS "Error","完成时间" AS "CompletedAt" FROM "外部同步任务";
        """);

    async Task<string> ReadSystemStateAsync(long id) => await db.QuerySingleAsync<string>("SELECT \"状态代码\" FROM \"外部系统\" WHERE \"编号\"=@Id;", new { Id = id });
}
finally
{
    foreach (var services in controllerServices) services.Dispose();
    await admin.ExecuteAsync($"DROP SCHEMA \"{schema}\" CASCADE;");
}

static int Status(object value) => value is ObjectResult result ? result.StatusCode ?? 200 : 200;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new InvalidOperationException("Regression failed: " + scenario);
}

static async Task<object?> InvokeAsync(ExternalSyncWorker worker, string name, params object?[] args)
{
    var method = typeof(ExternalSyncWorker).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Production worker method not found: " + name);
    var task = (Task)method.Invoke(worker, args)!;
    await task;
    return task.GetType().GetProperty("Result")?.GetValue(task);
}

sealed class FixedConnectionFactory(string connectionString) : IDatabaseConnectionFactory
{
    public string Create(string databaseKey, string environmentName) => connectionString;
}

sealed class HandlerClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

sealed class ResponseSequence(params object[] responses) : HttpMessageHandler
{
    private readonly Queue<object> responses = new(responses);
    public int Attempts { get; private set; }
    public List<string> IdempotencyKeys { get; } = [];
    public List<string> RequestIds { get; } = [];
    public List<string> Addresses { get; } = [];
    public Func<Task>? OnSend { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Attempts++;
        if (request.Headers.TryGetValues("Idempotency-Key", out var keys)) IdempotencyKeys.Add(keys.Single());
        Addresses.Add(request.RequestUri!.AbsoluteUri);
        RequestIds.Add(request.Headers.GetValues("X-Request-ID").Single());
        if (OnSend != null) await OnSend();
        var next = responses.Dequeue();
        if (next is Exception exception) throw exception;
        var response = new HttpResponseMessage((HttpStatusCode)next)
        {
            Content = new StringContent("{\"receipt\":\"regression-only\",\"access_token\":\"test-secret\"}")
        };
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
        return response;
    }
}

sealed class TaskState
{
    public string State { get; set; } = "";
    public int Attempts { get; set; }
    public DateTime? NextRetry { get; set; }
    public string? Error { get; set; }
    public DateTime? CompletedAt { get; set; }
}

sealed class SystemState
{
    public string State { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Error { get; set; }
}

sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "ExternalSyncRegression";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
