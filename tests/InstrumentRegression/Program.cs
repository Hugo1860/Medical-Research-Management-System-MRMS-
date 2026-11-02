using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using ScientificResearch.Controllers;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

// These tests call the production controller against a disposable migrated DB.
// The synthetic identity is for controller regression only, not a Keycloak UAT.
var connectionString = Environment.GetEnvironmentVariable("INSTRUMENT_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString) || Environment.GetEnvironmentVariable("INSTRUMENT_TEST_ALLOW_FIXTURES") != "YES")
    throw new InvalidOperationException("Set INSTRUMENT_TEST_CONNECTION and INSTRUMENT_TEST_ALLOW_FIXTURES=YES for an isolated migrated test database.");
await using var db = new NpgsqlConnection(connectionString);
await db.OpenAsync();
var marker = Guid.NewGuid().ToString("N");
var userId = Random.Shared.Next(1500000000, 1900000000);
var normalId = userId + 1;
var departmentId = userId;
long platformId = 0;
var passed = 0;
using var services = new ServiceCollection()
    .AddSingleton<IDatabaseConnectionFactory>(new FixedConnectionFactory(connectionString))
    .AddSingleton<IWebHostEnvironment>(new TestEnvironment())
    .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
    .BuildServiceProvider();
PlatformInstrumentController Controller(int personId)
{
    var context = new DefaultHttpContext { RequestServices = services };
    context.Items[KeycloakUserMiddleware.CurrentUserItemKey] = new CurrentUser { 编号 = personId, DbKey = "ScientificResearch_Model", 工号 = "instrument-regression", 部门编号 = departmentId };
    return new PlatformInstrumentController { ControllerContext = new ControllerContext { HttpContext = context } };
}
void Check(bool valid, string name) { if (!valid) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
static int Status(object value) => value is ObjectResult result ? result.StatusCode ?? 200 : 200;
static long Id(object value) => JsonSerializer.SerializeToElement(value).GetProperty("编号").GetInt64();
async Task ExpectArgument(Func<Task<object>> action, string name)
{
    try { await action(); throw new Exception("FAIL: " + name + " did not reject invalid input"); }
    catch (ArgumentException) { Check(true, name); }
}
try
{
    await db.ExecuteAsync("""
        INSERT INTO "部门" ("编号","上级部门编号","名称") VALUES (@departmentId,0,@name);
        INSERT INTO "人员" ("编号","工号","密码","姓名","部门编号") VALUES (@userId,@manager,'not-a-login','仪器测试管理员',@departmentId),(@normalId,@normal,'not-a-login','仪器测试申请人',@departmentId);
        INSERT INTO "人员角色" ("人员编号","角色编号") SELECT @userId,"编号" FROM "角色" WHERE "名称"='系统管理员' LIMIT 1;
        """, new { departmentId, userId, normalId, name = "仪器测试" + marker, manager = "im-" + marker, normal = "iu-" + marker });
    Check(await db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"人员角色\" WHERE \"人员编号\"=@userId", new { userId }) == 1, "manager fixture has a real database role mapping");
    platformId = await db.QuerySingleAsync<long>("INSERT INTO \"科研平台\" (\"平台名称\",\"创建人编号\") VALUES (@name,@userId) RETURNING \"编号\"", new { name = "仪器平台" + marker, userId });
    var manager = Controller(userId);
    var normal = Controller(normalId);
    var instrument = new PlatformInstrumentSaveModel { 平台编号 = checked((int)platformId), 资产编号 = marker, 设备名称 = "回归仪器", 管理员编号 = userId, 内部单价 = 120m, 最短预约分钟 = 30, 最长预约分钟 = 480 };
    Check(Status(await normal.保存仪器设备(instrument)) == 403, "non-manager cannot maintain instruments");
    var instrumentId = Id(await manager.保存仪器设备(instrument));
    var start = new DateTime(2030, 1, 10, 9, 0, 0, DateTimeKind.Unspecified);
    InstrumentBookingApplyModel Apply(DateTime time, int minutes = 60, int samples = 2) => new() { 仪器编号 = instrumentId, 开始时间 = time, 结束时间 = time.AddMinutes(minutes), 样品数 = samples, 用途说明 = "isolated regression" };
    var bookingId = Id(await normal.提交仪器预约(Apply(start)));
    Check(await db.QuerySingleAsync<string>("SELECT \"状态\" FROM \"仪器预约\" WHERE \"编号\"=@bookingId", new { bookingId }) == "待审核", "booking commits through one open database connection");
    Check(Status(await normal.提交仪器预约(Apply(start.AddMinutes(15)))) == 409, "overlapping pending booking is rejected");
    Check(Status(await normal.提交仪器预约(Apply(start.AddDays(1), 10))) == 409, "booking minimum duration is enforced by server");
    await ExpectArgument(() => normal.提交仪器预约(Apply(start.AddDays(1), samples: 0)), "zero sample booking is rejected by server");
    InstrumentUsageRegisterModel Usage(long id, int samples = 2) => new() { 预约编号 = id, 实际开始时间 = start, 实际结束时间 = start.AddHours(1), 实际样品数 = samples };
    Check(Status(await manager.登记仪器使用(Usage(bookingId))) == 409, "pending booking cannot be charged");
    await ExpectArgument(() => manager.登记仪器使用(Usage(bookingId, -2)), "negative sample usage is rejected before billing");
    Check(Status(await normal.审核仪器预约(new() { 编号 = bookingId, 审核结果 = "已批准" })) == 403, "non-manager cannot approve booking");
    await manager.审核仪器预约(new() { 编号 = bookingId, 审核结果 = "已批准" });
    Check(Status(await normal.登记仪器使用(Usage(bookingId))) == 403, "non-manager cannot register charges");
    var result = JsonSerializer.SerializeToElement(await manager.登记仪器使用(Usage(bookingId)));
    Check(result.GetProperty("计费金额").GetDecimal() == 120m, "hourly charge uses the real configured rate");
    Check(await db.QuerySingleAsync<string>("SELECT \"状态\" FROM \"仪器预约\" WHERE \"编号\"=@bookingId", new { bookingId }) == "已完成", "charge and completed booking commit together");
    Check(Status(await manager.登记仪器使用(Usage(bookingId, 99))) == 409, "completed booking cannot be overwritten or charged twice");
    Check(await db.QuerySingleAsync<decimal>("SELECT \"计费金额\" FROM \"仪器使用记录\" WHERE \"预约编号\"=@bookingId", new { bookingId }) == 120m, "rejected duplicate keeps original charge");
    var concurrency = await Task.WhenAll(Controller(normalId).提交仪器预约(Apply(start.AddDays(2))), Controller(normalId).提交仪器预约(Apply(start.AddDays(2))));
    Check(concurrency.Count(r => Status(r) == 200) == 1 && concurrency.Count(r => Status(r) == 409) == 1, "concurrent overlapping requests admit exactly one booking");
    var sampleBookingId = Id(concurrency.Single(r => Status(r) == 200));
    await db.ExecuteAsync("UPDATE \"平台仪器设备\" SET \"计费方式\"='按样品',\"内部单价\"=12.5 WHERE \"编号\"=@instrumentId", new { instrumentId });
    await manager.审核仪器预约(new() { 编号 = sampleBookingId, 审核结果 = "已批准" });
    result = JsonSerializer.SerializeToElement(await manager.登记仪器使用(Usage(sampleBookingId, 3)));
    Check(result.GetProperty("计费金额").GetDecimal() == 37.5m, "sample charge uses the validated sample count");
    var rejectedId = Id(await normal.提交仪器预约(Apply(start.AddDays(3))));
    await manager.审核仪器预约(new() { 编号 = rejectedId, 审核结果 = "已拒绝" });
    Check(Status(await manager.登记仪器使用(Usage(rejectedId))) == 409, "rejected booking cannot become completed through usage registration");
    await db.ExecuteAsync("UPDATE \"平台仪器设备\" SET \"需培训资质\"=TRUE WHERE \"编号\"=@instrumentId", new { instrumentId });
    Check(Status(await normal.提交仪器预约(Apply(start.AddDays(4)))) == 403, "missing training qualification still prevents booking");
    Check(Status(await normal.生成平台年报(new() { 平台编号 = (int)platformId, 年度 = 2030 })) == 403, "non-manager cannot generate annual report");
    await manager.生成平台年报(new() { 平台编号 = (int)platformId, 年度 = 2030, 年度总结 = "回归测试草稿" });
    Check(await db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"平台年报\" WHERE \"平台编号\"=@platformId AND \"年度\"=2030", new { platformId }) == 1, "annual report is persisted from actual instrument data");
    await db.ExecuteAsync("UPDATE \"平台年报\" SET \"状态\"='已审核' WHERE \"平台编号\"=@platformId", new { platformId });
    Check(Status(await manager.生成平台年报(new() { 平台编号 = (int)platformId, 年度 = 2030, 年度总结 = "不得覆盖" })) == 409, "approved annual report cannot be overwritten by regeneration");
    Check(await db.QuerySingleAsync<string>("SELECT \"年度总结\" FROM \"平台年报\" WHERE \"平台编号\"=@platformId", new { platformId }) == "回归测试草稿", "approved report retains original content");
    Console.WriteLine($"Instrument controller regression passed: {passed} checks. Synthetic identity; not a real-login UAT.");
}
finally
{
    // Delete only this run's fixtures, in foreign-key order; no source business rows are changed.
    if (platformId != 0) await db.ExecuteAsync("DELETE FROM \"科研平台\" WHERE \"编号\"=@platformId", new { platformId });
    await db.ExecuteAsync("DELETE FROM \"人员角色\" WHERE \"人员编号\" IN (@userId,@normalId); DELETE FROM \"人员\" WHERE \"编号\" IN (@userId,@normalId); DELETE FROM \"部门\" WHERE \"编号\"=@departmentId", new { userId, normalId, departmentId });
}
sealed class FixedConnectionFactory(string connectionString) : IDatabaseConnectionFactory
{
    public string Create(string databaseKey, string environmentName) => connectionString;
}
sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "InstrumentRegression";
    public string EnvironmentName { get; set; } = "Production";
    public string ContentRootPath { get; set; } = "/tmp";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "/tmp";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
