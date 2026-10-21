using System.Diagnostics;
using Dapper;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyLib;
using Npgsql;
using ScientificResearch.Models;

namespace ScientificResearch.Infrastucture;

/// <summary>
/// Records mutating API requests after the response has completed. Request bodies,
/// cookies and authorization headers are deliberately excluded.
/// </summary>
public sealed class AuditMiddleware
{
    private readonly RequestDelegate next;
    private readonly IDatabaseConnectionFactory connectionFactory;
    private readonly IWebHostEnvironment environment;
    private readonly KeycloakOptions keycloak;
    private readonly IConfiguration configuration;
    private readonly ILog log;

    public AuditMiddleware(
        RequestDelegate next,
        IDatabaseConnectionFactory connectionFactory,
        IWebHostEnvironment environment,
        KeycloakOptions keycloak,
        IConfiguration configuration,
        ILog log)
    {
        this.next = next;
        this.connectionFactory = connectionFactory;
        this.environment = environment;
        this.keycloak = keycloak;
        this.configuration = configuration;
        this.log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception failure = null;
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await WriteAuditAsync(context, stopwatch.ElapsedMilliseconds, failure);
        }
    }

    private async Task WriteAuditAsync(HttpContext context, long elapsedMs, Exception failure)
    {
        var user = keycloak.Enabled
            ? context.Items.TryGetValue(KeycloakUserMiddleware.CurrentUserItemKey, out var itemUser)
                ? itemUser as CurrentUser
                : null
            : context.Session.Get<CurrentUser>("user");
        if (user == null) return;

        try
        {
            var timeoutSeconds = Math.Clamp(
                configuration.GetValue<int?>("Audit:TimeoutSeconds") ?? 3,
                1,
                15);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var cancellationToken = timeout.Token;
            await using var connection = new NpgsqlConnection(
                connectionFactory.Create(user.DbKey, environment.EnvironmentName));
            await connection.OpenAsync(cancellationToken);
            var responseStatus = context.Response.StatusCode;
            if (failure is UnauthorizedAccessException && responseStatus < 400) responseStatus = StatusCodes.Status401Unauthorized;
            else if (failure != null && responseStatus < 400) responseStatus = StatusCodes.Status500InternalServerError;
            var command = new CommandDefinition(
                "SELECT \"sp_操作审计_增\"(@人员编号,@请求编号,@请求方法,@请求路径,@响应状态,@客户端IP,@耗时毫秒,@是否成功,@代理授权人编号);",
                new
                {
                    人员编号 = user.编号,
                    代理授权人编号 = context.Items.TryGetValue("代理授权人编号", out var proxyGrantor)
                        && proxyGrantor is int proxyGrantorId
                        ? proxyGrantorId
                        : (int?)null,
                    请求编号 = context.TraceIdentifier,
                    请求方法 = context.Request.Method,
                    请求路径 = context.Request.Path.Value ?? "/",
                    响应状态 = responseStatus,
                    客户端IP = context.Connection.RemoteIpAddress?.ToString(),
                    耗时毫秒 = (int)Math.Min(int.MaxValue, elapsedMs),
                    是否成功 = failure == null && responseStatus < 400
                },
                commandTimeout: timeoutSeconds,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (Exception exception)
        {
            // Auditing must never turn a completed business request into a failure.
            log.Warn($"操作审计写入失败: {context.Request.Method} {context.Request.Path}", exception);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (context.Request.Method is not ("POST" or "PUT" or "PATCH" or "DELETE")) return false;
        var path = context.Request.Path.Value ?? string.Empty;
        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            && !path.Equals("/Access/Login", StringComparison.OrdinalIgnoreCase);
    }
}
