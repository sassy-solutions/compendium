// -----------------------------------------------------------------------
// <copyright file="ZitadelHealthCheck.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Adapters.Zitadel.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Zitadel.Health;

/// <summary>
/// Health check for Zitadel Identity Provider connectivity.
/// Verifies the OIDC discovery endpoint is accessible and configuration is valid.
/// </summary>
public sealed class ZitadelHealthCheck : IHealthCheck
{
    private readonly ZitadelOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZitadelHealthCheck> _logger;

    public ZitadelHealthCheck(
        IOptions<ZitadelOptions> options,
        HttpClient httpClient,
        ILogger<ZitadelHealthCheck> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Step 1: Validate configuration
            var configErrors = ValidateConfiguration();
            if (configErrors.Count > 0)
            {
                data["configurationErrors"] = configErrors;
                return HealthCheckResult.Unhealthy(
                    "Zitadel configuration is invalid",
                    data: data);
            }

            // Step 2: Check OIDC discovery endpoint
            var discoveryUrl = $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";
            data["discoveryUrl"] = discoveryUrl;

            using var response = await _httpClient.GetAsync(discoveryUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                data["statusCode"] = (int)response.StatusCode;
                return HealthCheckResult.Unhealthy(
                    $"Zitadel OIDC discovery endpoint returned {response.StatusCode}",
                    data: data);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var discovery = JsonSerializer.Deserialize<JsonElement>(content);

            // Step 3: Validate issuer matches
            if (discovery.TryGetProperty("issuer", out var issuerElement))
            {
                var issuer = issuerElement.GetString();
                data["issuer"] = issuer ?? "null";

                var expectedIssuer = _options.Authority.TrimEnd('/');
                if (!string.Equals(issuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
                {
                    data["expectedIssuer"] = expectedIssuer;
                    return HealthCheckResult.Degraded(
                        "Zitadel issuer mismatch",
                        data: data);
                }
            }

            // Step 4: Verify required endpoints exist
            var requiredEndpoints = new[] { "authorization_endpoint", "token_endpoint", "jwks_uri" };
            var missingEndpoints = requiredEndpoints
                .Where(ep => !discovery.TryGetProperty(ep, out _))
                .ToList();

            if (missingEndpoints.Count > 0)
            {
                data["missingEndpoints"] = missingEndpoints;
                return HealthCheckResult.Degraded(
                    "Zitadel discovery missing required endpoints",
                    data: data);
            }

            data["authority"] = _options.Authority;
            data["projectId"] = _options.ProjectId ?? "not configured";

            return HealthCheckResult.Healthy(
                "Zitadel is healthy and properly configured",
                data: data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Zitadel health check failed: network error");
            data["error"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                "Zitadel is unreachable",
                ex,
                data);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Zitadel health check failed: timeout");
            data["error"] = "Request timed out";
            return HealthCheckResult.Unhealthy(
                "Zitadel request timed out",
                ex,
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zitadel health check failed unexpectedly");
            data["error"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                "Zitadel health check failed",
                ex,
                data);
        }
    }

    private List<string> ValidateConfiguration()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(_options.Authority))
        {
            errors.Add("Authority is not configured");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            errors.Add("ClientId is not configured");
        }

        // ClientSecret is optional for public clients, but we use confidential clients
        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            errors.Add("ClientSecret is not configured");
        }

        return errors;
    }
}
