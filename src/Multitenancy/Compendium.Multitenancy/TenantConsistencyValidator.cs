// -----------------------------------------------------------------------
// <copyright file="TenantConsistencyValidator.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Results;
using Microsoft.Extensions.Logging;

namespace Compendium.Multitenancy;

/// <summary>
/// Validates that tenant identifiers from multiple sources are consistent.
/// Ensures header, subdomain, and JWT claim all reference the same tenant.
/// </summary>
public interface ITenantConsistencyValidator
{
    /// <summary>
    /// Validates that all provided tenant identifiers are consistent (match).
    /// </summary>
    /// <param name="sources">The tenant identifiers from different sources.</param>
    /// <returns>A result indicating success or validation failure with details.</returns>
    Result<string> Validate(TenantSourceIdentifiers sources);
}

/// <summary>
/// Contains tenant identifiers from various sources for consistency validation.
/// </summary>
public sealed record TenantSourceIdentifiers
{
    /// <summary>
    /// Gets the tenant ID from the HTTP header (X-Tenant-ID).
    /// </summary>
    public string? HeaderTenantId { get; init; }

    /// <summary>
    /// Gets the tenant ID extracted from the subdomain.
    /// </summary>
    public string? SubdomainTenantId { get; init; }

    /// <summary>
    /// Gets the tenant ID from the JWT claim.
    /// </summary>
    public string? JwtTenantId { get; init; }

    /// <summary>
    /// Gets all non-null tenant IDs.
    /// </summary>
    public IEnumerable<string> GetAllIds() =>
        new[] { HeaderTenantId, SubdomainTenantId, JwtTenantId }
            .Where(id => !string.IsNullOrWhiteSpace(id))!;

    /// <summary>
    /// Gets the count of non-null tenant sources.
    /// </summary>
    public int SourceCount => GetAllIds().Count();

    /// <summary>
    /// Gets whether all non-null sources have the same tenant ID.
    /// </summary>
    public bool AreConsistent
    {
        get
        {
            var ids = GetAllIds().ToList();
            return ids.Count <= 1 || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
        }
    }

    /// <summary>
    /// Gets the resolved tenant ID (if consistent).
    /// </summary>
    public string? ResolvedTenantId => AreConsistent ? GetAllIds().FirstOrDefault() : null;
}

/// <summary>
/// Default implementation of <see cref="ITenantConsistencyValidator"/>.
/// </summary>
public sealed class TenantConsistencyValidator : ITenantConsistencyValidator
{
    private readonly TenantConsistencyOptions _options;
    private readonly ILogger<TenantConsistencyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConsistencyValidator"/> class.
    /// </summary>
    public TenantConsistencyValidator(
        TenantConsistencyOptions options,
        ILogger<TenantConsistencyValidator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Result<string> Validate(TenantSourceIdentifiers sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        // Check if we have at least one source
        if (sources.SourceCount == 0)
        {
            if (_options.RequireAtLeastOneSource)
            {
                _logger.LogWarning("No tenant identifier found in any source");
                return Result.Failure<string>(TenantErrors.NoTenantIdentifier());
            }

            _logger.LogDebug("No tenant identifier provided, allowing anonymous access");
            return Result.Success(string.Empty);
        }

        // Check minimum required sources
        if (_options.MinimumRequiredSources > 0 && sources.SourceCount < _options.MinimumRequiredSources)
        {
            _logger.LogWarning(
                "Insufficient tenant sources. Required: {Required}, Found: {Found}",
                _options.MinimumRequiredSources,
                sources.SourceCount);

            return Result.Failure<string>(TenantErrors.InsufficientSources(
                _options.MinimumRequiredSources,
                sources.SourceCount));
        }

        // Check consistency
        if (!sources.AreConsistent)
        {
            var ids = sources.GetAllIds().ToList();
            _logger.LogWarning(
                "Tenant ID mismatch detected. Header: {Header}, Subdomain: {Subdomain}, JWT: {Jwt}",
                sources.HeaderTenantId ?? "(none)",
                sources.SubdomainTenantId ?? "(none)",
                sources.JwtTenantId ?? "(none)");

            return Result.Failure<string>(TenantErrors.TenantMismatch(
                sources.HeaderTenantId,
                sources.SubdomainTenantId,
                sources.JwtTenantId));
        }

        var tenantId = sources.ResolvedTenantId!;
        _logger.LogDebug("Tenant consistency validated. TenantId: {TenantId}", tenantId);

        return Result.Success(tenantId);
    }
}

/// <summary>
/// Configuration options for tenant consistency validation.
/// </summary>
public sealed class TenantConsistencyOptions
{
    /// <summary>
    /// Gets or sets whether at least one tenant source is required.
    /// Default is true.
    /// </summary>
    public bool RequireAtLeastOneSource { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of sources required.
    /// Set to 0 to allow any number of sources.
    /// Set to 2 or 3 to require multiple consistent sources.
    /// Default is 1.
    /// </summary>
    public int MinimumRequiredSources { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to allow anonymous (no tenant) requests.
    /// Only applies when RequireAtLeastOneSource is false.
    /// Default is false.
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// Gets or sets paths that should be excluded from tenant validation.
    /// Example: ["/health", "/metrics", "/.well-known"].
    /// Default includes common health check paths.
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

/// <summary>
/// Standard tenant-related errors.
/// </summary>
public static class TenantErrors
{
    /// <summary>
    /// Error code prefix for tenant errors.
    /// </summary>
    public const string Prefix = "Tenant";

    /// <summary>
    /// No tenant identifier was found in any source.
    /// </summary>
    public static Error NoTenantIdentifier() =>
        Error.Validation($"{Prefix}.NoIdentifier",
            "No tenant identifier found. Please provide a tenant ID via header, subdomain, or authentication token.");

    /// <summary>
    /// Insufficient tenant sources provided.
    /// </summary>
    public static Error InsufficientSources(int required, int found) =>
        Error.Validation($"{Prefix}.InsufficientSources",
            $"Insufficient tenant sources. Required {required} but found {found}.");

    /// <summary>
    /// Tenant identifiers from different sources don't match.
    /// </summary>
    public static Error TenantMismatch(string? header, string? subdomain, string? jwt) =>
        Error.Validation($"{Prefix}.Mismatch",
            $"Tenant identifier mismatch. Header: '{header ?? "none"}', Subdomain: '{subdomain ?? "none"}', JWT: '{jwt ?? "none"}'.");

    /// <summary>
    /// Tenant was not found.
    /// </summary>
    public static Error TenantNotFound(string tenantId) =>
        Error.NotFound($"{Prefix}.NotFound",
            $"Tenant '{tenantId}' was not found or is not active.");

    /// <summary>
    /// Tenant access is not authorized.
    /// </summary>
    public static Error TenantAccessDenied(string tenantId) =>
        Error.Forbidden($"{Prefix}.AccessDenied",
            $"Access to tenant '{tenantId}' is not authorized.");
}
