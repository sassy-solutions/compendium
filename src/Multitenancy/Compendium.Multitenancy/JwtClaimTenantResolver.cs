// -----------------------------------------------------------------------
// <copyright file="JwtClaimTenantResolver.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Results;
using Microsoft.Extensions.Logging;

namespace Compendium.Multitenancy;

/// <summary>
/// A tenant resolver that identifies tenants based on JWT claims.
/// Extracts tenant identifier from a specified claim in the authenticated user's token.
/// </summary>
public sealed class JwtClaimTenantResolver : ITenantResolver
{
    private readonly ITenantStore _tenantStore;
    private readonly JwtClaimTenantResolverOptions _options;
    private readonly ILogger<JwtClaimTenantResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtClaimTenantResolver"/> class.
    /// </summary>
    /// <param name="tenantStore">The tenant store for retrieving tenant information.</param>
    /// <param name="options">The configuration options for JWT-based resolution.</param>
    /// <param name="logger">The logger instance.</param>
    public JwtClaimTenantResolver(
        ITenantStore tenantStore,
        JwtClaimTenantResolverOptions options,
        ILogger<JwtClaimTenantResolver> logger)
    {
        _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves tenant information from JWT claims.
    /// </summary>
    /// <param name="context">The tenant resolution context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the resolved tenant or null if not found.</returns>
    public async Task<Result<TenantInfo?>> ResolveTenantAsync(
        TenantResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        // JWT claims should be passed in the Properties dictionary
        if (!context.Properties.TryGetValue("Claims", out var claimsObj) ||
            claimsObj is not IDictionary<string, string> claims)
        {
            _logger.LogDebug("No claims found in resolution context");
            return Result.Success<TenantInfo?>(null);
        }

        // Try each configured claim name
        foreach (var claimName in _options.ClaimNames)
        {
            if (claims.TryGetValue(claimName, out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogDebug("Found tenant ID {TenantId} in claim {ClaimName}", tenantId, claimName);
                return await _tenantStore.GetByIdAsync(tenantId, cancellationToken);
            }
        }

        _logger.LogDebug("No tenant ID found in any configured JWT claims: {ClaimNames}",
            string.Join(", ", _options.ClaimNames));
        return Result.Success<TenantInfo?>(null);
    }
}

/// <summary>
/// Configuration options for JWT claim-based tenant resolution.
/// </summary>
public sealed class JwtClaimTenantResolverOptions
{
    /// <summary>
    /// Gets or initializes the claim names to search for tenant identifier.
    /// The resolver will try each claim in order and use the first non-empty value found.
    /// Default is ["tenant_id", "tid", "org_id", "organization_id"].
    /// </summary>
    public string[] ClaimNames { get; init; } = new[]
    {
        "tenant_id",
        "tid",
        "org_id",
        "organization_id",
        "urn:zitadel:iam:org:id"  // Zitadel-specific claim
    };
}
