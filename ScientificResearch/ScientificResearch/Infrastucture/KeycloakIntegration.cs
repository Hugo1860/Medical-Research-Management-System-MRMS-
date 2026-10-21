using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Npgsql;
using ScientificResearch.Models;

namespace ScientificResearch.Infrastucture;

/// <summary>Keycloak 配置。Keycloak 用户名默认必须与本地人员表的工号一致。</summary>
public sealed class KeycloakOptions
{
    public bool Enabled { get; set; }
    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool RequireMfa { get; set; }
    public string MfaAmr { get; set; } = "otp,mfa";
    public string UserIdentifierClaim { get; set; } = "preferred_username";
    public string DbKey { get; set; } = "ScientificResearch_Model";

    public static KeycloakOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Keycloak");
        return new KeycloakOptions
        {
            Enabled = section.GetValue<bool>("Enabled"),
            Authority = section.GetValue<string>("Authority") ?? "",
            Audience = section.GetValue<string>("Audience") ?? "",
            ClientId = section.GetValue<string>("ClientId") ?? "",
            RedirectUri = section.GetValue<string>("RedirectUri") ?? "",
            RequireHttpsMetadata = section.GetValue("RequireHttpsMetadata", true),
            RequireMfa = section.GetValue("RequireMfa", false),
            MfaAmr = section.GetValue<string>("MfaAmr") ?? "otp,mfa",
            UserIdentifierClaim = section.GetValue<string>("UserIdentifierClaim") ?? "preferred_username",
            DbKey = section.GetValue<string>("DbKey") ?? "ScientificResearch_Model"
        };
    }
}

public interface IKeycloakUserResolver
{
    Task<CurrentUser> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

/// <summary>将 Keycloak 身份映射到现有人员记录，不自动创建人员。</summary>
public sealed class KeycloakUserResolver : IKeycloakUserResolver
{
    private readonly KeycloakOptions options;
    private readonly IDatabaseConnectionFactory connectionFactory;
    private readonly IWebHostEnvironment environment;

    public KeycloakUserResolver(KeycloakOptions options, IDatabaseConnectionFactory connectionFactory, IWebHostEnvironment environment)
    {
        this.options = options;
        this.connectionFactory = connectionFactory;
        this.environment = environment;
    }

    public async Task<CurrentUser> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var identifier = principal.FindFirst(options.UserIdentifierClaim)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(identifier)) return null;

        var connectionString = connectionFactory.Create(options.DbKey, environment.EnvironmentName);
        await using var connection = new NpgsqlConnection(connectionString);
        var user = await connection.QuerySingleOrDefaultAsync<CurrentUser>(new CommandDefinition(@"
SELECT p.编号, p.工号, p.姓名, p.部门编号, d.名称 AS 部门名称, string_agg(DISTINCT r.名称, '、' ORDER BY r.名称) AS 角色名称
FROM 人员 p
LEFT JOIN 部门 d ON d.编号 = p.部门编号
LEFT JOIN 人员角色 pr ON pr.人员编号 = p.编号
LEFT JOIN 角色 r ON r.编号 = pr.角色编号
WHERE p.工号 = @工号 AND p.是否启用 = TRUE
GROUP BY p.编号, p.工号, p.姓名, p.部门编号, d.名称;",
            new { 工号 = identifier }, cancellationToken: cancellationToken));
        if (user != null) user.DbKey = options.DbKey;
        return user;
    }
}

/// <summary>在 MVC 过滤器之前，将 Keycloak 用户放入当前请求上下文。</summary>
public sealed class KeycloakUserMiddleware
{
    public const string CurrentUserItemKey = "ScientificResearch.CurrentUser";
    private readonly RequestDelegate next;
    private readonly KeycloakOptions options;

    public KeycloakUserMiddleware(RequestDelegate next, KeycloakOptions options)
    {
        this.next = next;
        this.options = options;
    }

    public async Task InvokeAsync(HttpContext context, IKeycloakUserResolver resolver)
    {
        if (options.Enabled && context.User.Identity?.IsAuthenticated == true)
        {
            var user = await resolver.ResolveAsync(context.User, context.RequestAborted);
            if (user != null) context.Items[CurrentUserItemKey] = user;
        }
        await next(context);
    }
}
