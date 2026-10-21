using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ScientificResearch.Infrastucture;

/// <summary>Checks the configured Keycloak Realm discovery document before traffic is admitted.</summary>
public sealed class KeycloakReadinessCheck : IHealthCheck
{
    private readonly KeycloakOptions options;
    private readonly IHttpClientFactory clients;

    public KeycloakReadinessCheck(KeycloakOptions options, IHttpClientFactory clients)
    {
        this.options = options;
        this.clients = clients;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled) return HealthCheckResult.Healthy("Keycloak 未启用");
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority))
            return HealthCheckResult.Unhealthy("Keycloak Authority 配置无效");
        var discovery = new Uri(authority.ToString().TrimEnd('/') + "/.well-known/openid-configuration");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Keycloak can take several seconds to answer while its JVM is
            // warming up or the local Docker host is under memory pressure.
            // Keep readiness fail-closed, but avoid turning a transient slow
            // discovery response into a false 503 for the whole application.
            timeout.CancelAfter(TimeSpan.FromSeconds(6));
            var probeToken = timeout.Token;
            var client = clients.CreateClient("keycloak");
            using var response = await client.GetAsync(discovery, probeToken);
            if (!response.IsSuccessStatusCode)
                return HealthCheckResult.Unhealthy($"Keycloak Realm discovery 返回 {(int)response.StatusCode}");
            await using var stream = await response.Content.ReadAsStreamAsync(probeToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: probeToken);
            if (!document.RootElement.TryGetProperty("issuer", out var issuer) || string.IsNullOrWhiteSpace(issuer.GetString()))
                return HealthCheckResult.Unhealthy("Keycloak discovery 缺少 issuer");
            if (!document.RootElement.TryGetProperty("token_endpoint", out var tokenEndpoint)
                || !Uri.TryCreate(tokenEndpoint.GetString(), UriKind.Absolute, out _))
                return HealthCheckResult.Unhealthy("Keycloak discovery 缺少有效 token_endpoint");
            var expectedIssuer = authority.ToString().TrimEnd('/');
            var actualIssuer = issuer.GetString()!.TrimEnd('/');
            if (!string.Equals(expectedIssuer, actualIssuer, StringComparison.OrdinalIgnoreCase))
                return HealthCheckResult.Unhealthy($"Keycloak issuer 不匹配，期望 {expectedIssuer}");
            return HealthCheckResult.Healthy("Keycloak Realm discovery 可访问");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Keycloak Realm discovery 不可访问", exception);
        }
    }
}
