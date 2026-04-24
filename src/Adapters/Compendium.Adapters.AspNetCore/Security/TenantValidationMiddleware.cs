// -----------------------------------------------------------------------
// <copyright file="TenantValidationMiddleware.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Json;
using Compendium.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.AspNetCore.Security;

/// <summary>
/// Middleware that validates tenant identity from multiple sources (header, subdomain, JWT)
/// and ensures consistency before allowing the request to proceed.
/// </summary>
public sealed class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantValidationMiddlewareOptions _options;
    private readonly ILogger<TenantValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantValidationMiddleware"/> class.
    /// </summary>
    public TenantValidationMiddleware(
        RequestDelegate next,
        IOptions<TenantValidationMiddlewareOptions> options,
        ILogger<TenantValidationMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Processes the request through the tenant validation pipeline.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantConsistencyValidator validator,
        ITenantStore tenantStore,
        TenantContext tenantContext)
    {
        // Check if path is excluded
        if (IsExcludedPath(context.Request.Path))
        {
            _logger.LogDebug("Path {Path} is excluded from tenant validation", SanitizeForLog(context.Request.Path.Value));
            await _next(context);
            return;
        }

        // Extract tenant identifiers from all sources
        var sources = ExtractTenantSources(context);

        // Validate consistency
        var validationResult = validator.Validate(sources);

        if (validationResult.IsFailure)
        {
            _logger.LogWarning(
                "Tenant validation failed: {Error}. Path: {Path}",
                validationResult.Error.Message,
                SanitizeForLog(context.Request.Path.Value));

            await WriteErrorResponse(context, validationResult.Error.Message, StatusCodes.Status403Forbidden);
            return;
        }

        var tenantId = validationResult.Value;

        // If we have a tenant ID, resolve and set the tenant context
        if (!string.IsNullOrEmpty(tenantId))
        {
            var tenantResult = await tenantStore.GetByIdAsync(tenantId, context.RequestAborted);

            if (tenantResult.IsFailure || tenantResult.Value is null)
            {
                _logger.LogWarning("Tenant {TenantId} not found or inactive", SanitizeForLog(tenantId));
                await WriteErrorResponse(
                    context,
                    TenantErrors.TenantNotFound(tenantId).Message,
                    StatusCodes.Status404NotFound);
                return;
            }

            if (!tenantResult.Value.IsActive)
            {
                _logger.LogWarning("Tenant {TenantId} is not active", SanitizeForLog(tenantId));
                await WriteErrorResponse(
                    context,
                    TenantErrors.TenantAccessDenied(tenantId).Message,
                    StatusCodes.Status403Forbidden);
                return;
            }

            // Set the tenant context for the duration of the request
            tenantContext.SetTenant(tenantResult.Value);

            _logger.LogDebug(
                "Tenant context set for {TenantId} ({TenantName})",
                tenantResult.Value.Id,
                tenantResult.Value.Name);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            // Clear tenant context after request
            tenantContext.SetTenant(null);
        }
    }

    private TenantSourceIdentifiers ExtractTenantSources(HttpContext context)
    {
        // Extract from header
        string? headerTenantId = null;
        if (context.Request.Headers.TryGetValue(_options.TenantHeaderName, out var headerValues))
        {
            headerTenantId = headerValues.FirstOrDefault();
        }

        // Extract from subdomain
        string? subdomainTenantId = null;
        if (_options.EnableSubdomainResolution)
        {
            subdomainTenantId = ExtractSubdomain(context.Request.Host.Host);
        }

        // Extract from JWT claims
        string? jwtTenantId = null;
        if (context.User.Identity?.IsAuthenticated == true)
        {
            jwtTenantId = ExtractTenantFromClaims(context.User.Claims);
        }

        return new TenantSourceIdentifiers
        {
            HeaderTenantId = headerTenantId,
            SubdomainTenantId = subdomainTenantId,
            JwtTenantId = jwtTenantId
        };
    }

    private string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrEmpty(host)) return null;

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        // Skip if it's an IP address
        if (System.Net.IPAddress.TryParse(hostWithoutPort, out _))
            return null;

        // Skip if it's localhost
        if (hostWithoutPort.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = hostWithoutPort.Split('.');

        // Need at least 3 parts for subdomain (subdomain.domain.tld)
        if (parts.Length >= 3)
        {
            var subdomain = parts[0];

            // Skip common non-tenant subdomains
            if (_options.IgnoredSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
                return null;

            return subdomain;
        }

        return null;
    }

    private string? ExtractTenantFromClaims(IEnumerable<Claim> claims)
    {
        foreach (var claimType in _options.TenantClaimTypes)
        {
            var claim = claims.FirstOrDefault(c =>
                c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase));

            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value;
            }
        }

        return null;
    }

    private bool IsExcludedPath(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;

        return _options.ExcludedPaths.Any(excluded =>
            pathValue.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Replace("\r", "_").Replace("\n", "_").Replace("\t", "_");
    }

    private static async Task WriteErrorResponse(HttpContext context, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var error = new
        {
            error = new
            {
                code = statusCode == StatusCodes.Status403Forbidden ? "TENANT_ACCESS_DENIED" : "TENANT_NOT_FOUND",
                message
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
}

/// <summary>
/// Configuration options for tenant validation middleware.
/// </summary>
public sealed class TenantValidationMiddlewareOptions
{
    /// <summary>
    /// Gets or sets the header name for tenant ID.
    /// Default is "X-Tenant-ID".
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets whether to enable subdomain-based tenant resolution.
    /// Default is true.
    /// </summary>
    public bool EnableSubdomainResolution { get; set; } = true;

    /// <summary>
    /// Gets or sets subdomains to ignore (not treated as tenant IDs).
    /// Default includes common subdomains like www, api, admin.
    /// </summary>
    public string[] IgnoredSubdomains { get; set; } = new[]
    {
        "www",
        "api",
        "admin",
        "app",
        "dashboard",
        "console",
        "portal",
        "staging",
        "dev",
        "test"
    };

    /// <summary>
    /// Gets or sets the JWT claim types to search for tenant ID.
    /// </summary>
    public string[] TenantClaimTypes { get; set; } = new[]
    {
        "tenant_id",
        "tid",
        "org_id",
        "organization_id",
        "urn:zitadel:iam:org:id"
    };

    /// <summary>
    /// Gets or sets paths excluded from tenant validation.
    /// </summary>
    public string[] ExcludedPaths { get; set; } = new[]
    {
        "/health",
        "/healthz",
        "/ready",
        "/live",
        "/metrics",
        "/.well-known",
        "/swagger",
        "/api-docs"
    };
}
