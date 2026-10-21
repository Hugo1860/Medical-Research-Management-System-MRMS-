using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace ScientificResearch.Infrastucture;

/// <summary>检查应用是否可以连接到当前配置的 PostgreSQL 数据库。</summary>
public sealed class PostgresReadinessCheck : IHealthCheck
{
    private readonly IConfiguration configuration;
    private readonly IDatabaseConnectionFactory connectionFactory;
    private readonly IWebHostEnvironment environment;

    public PostgresReadinessCheck(
        IConfiguration configuration,
        IDatabaseConnectionFactory connectionFactory,
        IWebHostEnvironment environment)
    {
        this.configuration = configuration;
        this.connectionFactory = connectionFactory;
        this.environment = environment;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var databaseKey = configuration.GetSection("allowedDatabaseKeys")
            .GetChildren()
            .Select(item => item.Value)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
            ?? "ScientificResearch_Model";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // PostgreSQL may briefly stall behind a checkpoint or container
            // resource contention; readiness remains bounded and fail-closed.
            timeout.CancelAfter(TimeSpan.FromSeconds(6));
            var probeToken = timeout.Token;
            await using var connection = new NpgsqlConnection(
                connectionFactory.Create(databaseKey, environment.EnvironmentName));
            await connection.OpenAsync(probeToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(probeToken);

            var expectedVersion = configuration.GetValue<int?>("Postgres:ExpectedSchemaVersion");
            if (expectedVersion is > 0)
            {
                await using var versionCommand = new NpgsqlCommand(
                    "SELECT \"版本\" FROM \"系统迁移版本\" WHERE \"编号\" = 1", connection);
                versionCommand.CommandTimeout = 2;
                var actualValue = await versionCommand.ExecuteScalarAsync(probeToken);
                if (actualValue is null || actualValue is DBNull)
                    return HealthCheckResult.Unhealthy("PostgreSQL 未登记迁移版本");
                var actualVersion = Convert.ToInt32(actualValue, System.Globalization.CultureInfo.InvariantCulture);
                if (actualVersion != expectedVersion.Value)
                    return HealthCheckResult.Unhealthy($"PostgreSQL 迁移版本不匹配，期望 {expectedVersion.Value}，实际 {actualVersion}");
            }

            return HealthCheckResult.Healthy("PostgreSQL 可连接");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL 不可用", exception);
        }
    }
}

/// <summary>检查启用 Redis 时分布式缓存的读写能力。</summary>
public sealed class RedisReadinessCheck : IHealthCheck
{
    private readonly IDistributedCache cache;

    public RedisReadinessCheck(IDistributedCache cache)
    {
        this.cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var key = $"health:redis:{Guid.NewGuid():N}";
        var probe = ProbeAsync(key, cancellationToken);
        var completed = await Task.WhenAny(
            probe,
            Task.Delay(TimeSpan.FromSeconds(2), cancellationToken));
        if (completed != probe)
        {
            return HealthCheckResult.Unhealthy("Redis 分布式缓存探针超时");
        }

        return await probe;
    }

    private async Task<HealthCheckResult> ProbeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);
            var value = await cache.GetStringAsync(key, cancellationToken);
            await cache.RemoveAsync(key, cancellationToken);
            return value == "ok"
                ? HealthCheckResult.Healthy("Redis 分布式缓存可读写")
                : HealthCheckResult.Unhealthy("Redis 分布式缓存回读值不一致");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis 分布式缓存不可用", exception);
        }
    }
}

/// <summary>为每个请求生成可在日志和响应中关联的请求编号。</summary>
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-ID";
    private readonly RequestDelegate next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!IsSafeRequestId(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static bool IsSafeRequestId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
        return value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
    }
}
