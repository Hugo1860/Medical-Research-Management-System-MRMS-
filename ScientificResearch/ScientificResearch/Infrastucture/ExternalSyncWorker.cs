using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ScientificResearch.Infrastucture
{
    /// <summary>执行已登记的外部同步任务；供应商差异由外部系统适配器端点承担。</summary>
    public sealed class ExternalSyncWorker : BackgroundService
    {
        private static readonly string[] RequiredSystemTypes = { "HR", "财务", "采购", "伦理", "合同", "人才库", "生物样本库", "论文采集" };
        private readonly IConfiguration configuration;
        private readonly IDatabaseConnectionFactory connectionFactory;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ExternalSyncWorker> logger;

        public ExternalSyncWorker(IConfiguration configuration, IDatabaseConnectionFactory connectionFactory,
            IHttpClientFactory httpClientFactory, ILogger<ExternalSyncWorker> logger)
        {
            this.configuration = configuration;
            this.connectionFactory = connectionFactory;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = configuration.GetValue("ExternalSync:Enabled", false);
            if (!enabled)
            {
                logger.LogInformation("External sync worker disabled by configuration");
                return;
            }

            if (!await HasCompleteOutboundConfigurationAsync(stoppingToken))
            {
                logger.LogError("External sync worker stopped: eight required hospital systems are not fully configured");
                return;
            }

            var interval = Math.Clamp(configuration.GetValue("ExternalSync:PollIntervalSeconds", 30), 5, 600);
            var batchSize = Math.Clamp(configuration.GetValue("ExternalSync:BatchSize", 10), 1, 50);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var recovered = await RequeueStaleAsync(stoppingToken);
                    if (recovered > 0)
                        logger.LogWarning("External sync worker recovered {Count} stale processing tasks", recovered);
                    for (var i = 0; i < batchSize && !stoppingToken.IsCancellationRequested; i++)
                    {
                        var work = await ClaimAsync(stoppingToken);
                        if (work == null) break;
                        await ExecuteOneAsync(work, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "External sync worker cycle failed");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }

        private async Task<bool> HasCompleteOutboundConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new NpgsqlConnection(GetConnectionString());
                await connection.OpenAsync(cancellationToken);
                var command = new CommandDefinition(@"
SELECT ""系统类型"",""系统代码"",""基础地址"",""认证方式"",""认证引用"",""超时秒数"",""最大重试次数""
  FROM ""外部系统"" WHERE ""是否启用""=TRUE;", cancellationToken: cancellationToken);
                var systems = (await connection.QueryAsync<OutboundSystemConfig>(command)).ToList();
                var missingTypes = RequiredSystemTypes
                    .Where(type => !systems.Any(system => system.系统类型 == type && IsCompleteOutboundConfig(system)))
                    .ToList();
                var duplicateTypes = RequiredSystemTypes
                    .Where(type => systems.Count(system => system.系统类型 == type) > 1)
                    .ToList();
                if (duplicateTypes.Count > 0)
                {
                    logger.LogError("External sync worker requires one enabled adapter per system type; duplicates: {DuplicateTypes}", string.Join(",", duplicateTypes));
                    return false;
                }
                if (missingTypes.Count > 0)
                    logger.LogError("External sync worker configuration missing: {MissingTypes}", string.Join(",", missingTypes));
                return missingTypes.Count == 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "External sync worker configuration check failed");
                return false;
            }
        }

        /// <summary>
        /// A process can be terminated after claiming a task and before the
        /// supplier response is recorded. Requeue the lease-expired task so
        /// it cannot remain in 处理中 forever. Tasks that exhausted retries
        /// become terminal failures and are visible in the failure alert API.
        /// </summary>
        private async Task<int> RequeueStaleAsync(CancellationToken cancellationToken)
        {
            var leaseSeconds = Math.Clamp(configuration.GetValue("ExternalSync:LeaseTimeoutSeconds", 900), 60, 3600);
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);
            return await connection.ExecuteAsync(new CommandDefinition(@"
UPDATE ""外部同步任务""
   SET ""状态""=CASE WHEN ""尝试次数"">=""最大重试次数"" THEN '失败' ELSE '待处理' END,
       ""下次重试时间""=CASE WHEN ""尝试次数"">=""最大重试次数"" THEN NULL ELSE CURRENT_TIMESTAMP END,
       ""最后错误""=COALESCE(NULLIF(""最后错误"",''),'worker 租约超时，任务已自动回收'),
       ""完成时间""=CASE WHEN ""尝试次数"">=""最大重试次数"" THEN CURRENT_TIMESTAMP ELSE ""完成时间"" END,
       ""更新时间""=CURRENT_TIMESTAMP
 WHERE ""状态""='处理中'
   AND ""更新时间"" < CURRENT_TIMESTAMP - make_interval(secs => @LeaseSeconds);", new { LeaseSeconds = leaseSeconds }, cancellationToken: cancellationToken));
        }

        private async Task<SyncWorkItem> ClaimAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<SyncWorkItem>(@"
WITH candidate AS (
    SELECT q.""编号""
      FROM ""外部同步任务"" q
      JOIN ""外部系统"" s ON s.""编号""=q.""外部系统编号""
     WHERE q.""状态""='待处理' AND s.""是否启用""=TRUE
       AND NULLIF(btrim(s.""基础地址""), '') IS NOT NULL
       AND (s.""认证方式""='无' OR NULLIF(btrim(s.""认证引用""), '') IS NOT NULL)
       AND s.""超时秒数"" BETWEEN 1 AND 120
       AND s.""最大重试次数"" BETWEEN 0 AND 10
       AND NULLIF(btrim(q.""幂等键""), '') IS NOT NULL
       AND length(q.""幂等键"") <= 200
       AND q.""幂等键"" !~ '[[:space:][:cntrl:]]'
       AND (q.""下次重试时间"" IS NULL OR q.""下次重试时间""<=CURRENT_TIMESTAMP)
     ORDER BY q.""创建时间"", q.""编号""
     LIMIT 1
     FOR UPDATE SKIP LOCKED
)
UPDATE ""外部同步任务"" q
   SET ""状态""='处理中', ""尝试次数""=q.""尝试次数""+1,
       ""请求ID""=@请求ID, ""更新时间""=CURRENT_TIMESTAMP
  FROM candidate c
  JOIN ""外部系统"" s ON s.""编号""=q.""外部系统编号""
 WHERE q.""编号""=c.""编号""
RETURNING q.""编号"", q.""业务类型"", q.""方向"", q.""业务主键"", q.""幂等键"", q.""请求负载""::text AS ""请求负载"",
          q.""尝试次数"", q.""最大重试次数"", q.""请求ID"", s.""基础地址"", s.""认证方式"", s.""认证引用"", s.""超时秒数"";",
                new { 请求ID = Guid.NewGuid().ToString("N") });
        }

        private async Task ExecuteOneAsync(SyncWorkItem work, CancellationToken cancellationToken)
        {
            try
            {
                if (!IsValidIdempotencyKey(work.幂等键))
                    throw new InvalidOperationException("同步任务幂等键为空、超长或包含空白/控制字符");
                using var request = new HttpRequestMessage(HttpMethod.Post, work.基础地址.TrimEnd('/'));
                request.Headers.TryAddWithoutValidation("X-Request-ID", work.请求ID);
                request.Headers.TryAddWithoutValidation("Idempotency-Key", work.幂等键);
                ApplyAuthentication(request, work.认证方式, work.认证引用);
                var envelope = new
                {
                    businessType = work.业务类型,
                    direction = work.方向,
                    businessKey = work.业务主键,
                    idempotencyKey = work.幂等键,
                    requestId = work.请求ID,
                    payload = ParsePayload(work.请求负载)
                };
                request.Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json");
                var client = httpClientFactory.CreateClient();
                // Respect the timeout registered for this supplier. The global
                // setting remains a safe fallback for legacy rows with no value.
                var fallbackTimeout = configuration.GetValue("ExternalSync:RequestTimeoutSeconds", 30);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(work.超时秒数 > 0 ? work.超时秒数 : fallbackTimeout, 1, 120));
                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    if (!retryAfter.HasValue && response.Headers.RetryAfter?.Date is DateTimeOffset retryAt)
                        retryAfter = retryAt - DateTimeOffset.UtcNow;
                    throw new ExternalSyncResponseException((int)response.StatusCode, SanitizeResponseSummary(body, 500), retryAfter);
                }
                await MarkSuccessAsync(work, SanitizeResponseSummary(body, 2000), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (OperationCanceledException ex)
            {
                // HttpClient uses OperationCanceledException for request
                // timeouts. A timeout is retryable; only host shutdown above
                // should escape without changing the task state.
                await MarkFailureAsync(work, ex.Message.Length == 0 ? "外部系统请求超时" : ex.Message, retryable: true, cancellationToken: cancellationToken);
                logger.LogWarning("External sync task {TaskId} timed out on attempt {Attempt}", work.编号, work.尝试次数);
            }
            catch (ExternalSyncResponseException ex)
            {
                await MarkFailureAsync(work, ex.Message, ex.Retryable, cancellationToken, ex.RetryAfter);
                logger.LogWarning("External sync task {TaskId} returned non-success HTTP response on attempt {Attempt}: {Error}", work.编号, work.尝试次数, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                await MarkFailureAsync(work, ex.Message, retryable: true, cancellationToken: cancellationToken);
                logger.LogWarning("External sync task {TaskId} failed on attempt {Attempt}: {Error}", work.编号, work.尝试次数, ex.Message);
            }
            catch (Exception ex)
            {
                await MarkFailureAsync(work, ex.Message, retryable: false, cancellationToken: cancellationToken);
                logger.LogWarning("External sync task {TaskId} rejected locally on attempt {Attempt}: {Error}", work.编号, work.尝试次数, ex.Message);
            }
        }

        private void ApplyAuthentication(HttpRequestMessage request, string authMode, string authRef)
        {
            if (string.IsNullOrWhiteSpace(authMode) || authMode == "无") return;
            if (!IsValidAuthReference(authRef)) throw new InvalidOperationException("外部系统认证引用格式无效");
            var secret = configuration[$"ExternalSystems:Secrets:{authRef}"];
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("部署环境缺少外部系统认证密钥");
            switch (authMode)
            {
                case "Bearer":
                case "OIDC": request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret); break;
                case "ApiKey": request.Headers.TryAddWithoutValidation("X-API-Key", secret); break;
                case "Basic": request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(secret))); break;
                default: throw new InvalidOperationException("不支持的外部系统认证方式");
            }
        }

        private async Task MarkSuccessAsync(SyncWorkItem work, string response, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(@"UPDATE ""外部同步任务"" SET ""状态""='成功',""外部响应摘要""=@响应,""最后错误""=NULL,""完成时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号 AND ""状态""='处理中';", new { 编号 = work.编号, 响应 = response });
        }

        private async Task MarkFailureAsync(SyncWorkItem work, string error, bool retryable, CancellationToken cancellationToken, TimeSpan? retryAfter = null)
        {
            var terminal = !retryable || work.尝试次数 >= work.最大重试次数;
            var backoffSeconds = Math.Min(300, Math.Pow(2, Math.Min(work.尝试次数, 8)) * 10);
            if (retryAfter.HasValue)
                backoffSeconds = Math.Clamp(retryAfter.Value.TotalSeconds, 1, 3600);
            var next = DateTime.Now.AddSeconds(backoffSeconds);
            await using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(@"UPDATE ""外部同步任务"" SET ""状态""=@状态,""最后错误""=@错误,""下次重试时间""=CASE WHEN @状态='失败' THEN NULL ELSE @下次重试时间 END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号 AND ""状态""='处理中';", new { 编号 = work.编号, 状态 = terminal ? "失败" : "待处理", 错误 = SanitizeResponseSummary(error, 2000), 下次重试时间 = next });
        }

        private sealed class ExternalSyncResponseException : Exception
        {
            public bool Retryable { get; }
            public TimeSpan? RetryAfter { get; }

            public ExternalSyncResponseException(int statusCode, string responseBody, TimeSpan? retryAfter = null)
                : base($"外部系统返回 HTTP {statusCode}: {responseBody}")
            {
                // 5xx, request timeout, and rate limiting are transient
                // supplier conditions. Other 4xx responses are treated as
                // terminal business/configuration errors.
                Retryable = statusCode >= 500 || statusCode is 408 or 429;
                RetryAfter = retryAfter;
            }
        }

        private string GetConnectionString() => connectionFactory.Create(configuration.GetValue<string>("ExternalSync:DatabaseKey") ?? "ScientificResearch_Model", configuration.GetValue<string>("Environment") ?? "Production");
        private static object ParsePayload(string payload) { try { return JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(payload) ? "{}" : payload); } catch { return new { }; } }
        /// <summary>
        /// Supplier responses are diagnostic evidence, not a second data store.
        /// Keep a short receipt summary while removing credentials and session
        /// material before the value reaches PostgreSQL or structured logs.
        /// </summary>
        private static string SanitizeResponseSummary(string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var sanitized = value;
            sanitized = Regex.Replace(sanitized,
                "(?i)([\\\"']?(?:access[_-]?token|refresh[_-]?token|id[_-]?token|authorization|password|passwd|secret|client[_-]?secret|api[_-]?key|cookie)[\\\"']?\\s*[:=]\\s*[\\\"'])(.*?)([\\\"'])",
                "$1[REDACTED]$3");
            sanitized = Regex.Replace(sanitized, @"(?i)(Bearer\s+)[A-Za-z0-9._~+/=-]+", "$1[REDACTED]");
            sanitized = Regex.Replace(sanitized, @"(?i)(Basic\s+)[A-Za-z0-9+/=]+", "$1[REDACTED]");
            return sanitized.Length <= length ? sanitized : sanitized.Substring(0, length);
        }
        private bool IsCompleteOutboundConfig(OutboundSystemConfig system)
            => IsValidAddress(system.基础地址)
            && (system.认证方式 == "无" || (IsValidAuthReference(system.认证引用) && !string.IsNullOrWhiteSpace(configuration[$"ExternalSystems:Secrets:{system.认证引用}"])))
            && system.超时秒数 is >= 1 and <= 120
            && system.最大重试次数 is >= 0 and <= 10;
        private static bool IsValidAddress(string address)
        {
            if (!Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var endpoint)
                || endpoint.Scheme is not ("http" or "https")
                || string.IsNullOrWhiteSpace(endpoint.Host)
                || (endpoint.Scheme == "http" && !IsLocalAddress(endpoint))) return false;
            return !address.Contains("example.", StringComparison.OrdinalIgnoreCase)
                && !address.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                && !address.Contains("<host>", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsLocalAddress(Uri endpoint)
            => endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || endpoint.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || endpoint.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        private static bool IsValidAuthReference(string reference)
            => !string.IsNullOrWhiteSpace(reference) && reference.Length <= 100
               && reference.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '.' or '-');
        private static bool IsValidIdempotencyKey(string key)
            => !string.IsNullOrWhiteSpace(key) && key.Length <= 200
               && key.All(ch => !char.IsWhiteSpace(ch) && !char.IsControl(ch));

        private sealed class OutboundSystemConfig
        {
            public string 系统类型 { get; set; }
            public string 系统代码 { get; set; }
            public string 基础地址 { get; set; }
            public string 认证方式 { get; set; }
            public string 认证引用 { get; set; }
            public int 超时秒数 { get; set; }
            public int 最大重试次数 { get; set; }
        }

        private sealed class SyncWorkItem
        {
            public long 编号 { get; set; }
            public string 业务类型 { get; set; }
            public string 方向 { get; set; }
            public string 业务主键 { get; set; }
            public string 幂等键 { get; set; }
            public string 请求负载 { get; set; }
            public int 尝试次数 { get; set; }
            public int 最大重试次数 { get; set; }
            public string 请求ID { get; set; }
            public string 基础地址 { get; set; }
            public string 认证方式 { get; set; }
            public string 认证引用 { get; set; }
            public int 超时秒数 { get; set; }
        }
    }
}
