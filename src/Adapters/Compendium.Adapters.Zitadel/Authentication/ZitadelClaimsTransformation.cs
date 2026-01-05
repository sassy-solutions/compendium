// -----------------------------------------------------------------------
// <copyright file="ZitadelClaimsTransformation.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Compendium.Adapters.Zitadel.Configuration;

namespace Compendium.Adapters.Zitadel.Authentication;

/// <summary>
/// Transforms Zitadel-specific claims into standard claims for the application.
/// This enables mapping of Zitadel organization IDs to tenant contexts.
/// </summary>
public sealed class ZitadelClaimsTransformation
{
    /// <summary>
    /// The claim type for Zitadel organization ID.
    /// </summary>
    public const string ZitadelOrgIdClaimType = "urn:zitadel:iam:org:id";

    /// <summary>
    /// The claim type for Zitadel resource owner ID.
    /// </summary>
    public const string ZitadelResourceOwnerClaimType = "urn:zitadel:iam:user:resourceowner:id";

    /// <summary>
    /// The claim type for Zitadel project roles.
    /// </summary>
    public const string ZitadelRolesClaimType = "urn:zitadel:iam:org:project:roles";

    /// <summary>
    /// Standard claim type for tenant ID.
    /// </summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>
    /// Standard claim type for organization ID.
    /// </summary>
    public const string OrganizationIdClaimType = "org_id";

    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelClaimsTransformation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZitadelClaimsTransformation"/> class.
    /// </summary>
    public ZitadelClaimsTransformation(
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelClaimsTransformation> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transforms Zitadel claims in the principal to standard application claims.
    /// </summary>
    /// <param name="principal">The claims principal to transform.</param>
    /// <returns>The transformed claims principal.</returns>
    public ClaimsPrincipal TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return principal;
        }

        var orgId = GetClaimValue(identity, ZitadelOrgIdClaimType)
                    ?? GetClaimValue(identity, ZitadelResourceOwnerClaimType);

        if (!string.IsNullOrEmpty(orgId))
        {
            // Add standard tenant_id claim if not present
            if (!identity.HasClaim(c => c.Type == TenantIdClaimType))
            {
                identity.AddClaim(new Claim(TenantIdClaimType, orgId));
                _logger.LogDebug("Added tenant_id claim with value {OrgId}", orgId);
            }

            // Add standard org_id claim if not present
            if (!identity.HasClaim(c => c.Type == OrganizationIdClaimType))
            {
                identity.AddClaim(new Claim(OrganizationIdClaimType, orgId));
            }
        }

        // Transform Zitadel roles to standard role claims
        var rolesClaim = identity.FindFirst(ZitadelRolesClaimType);
        if (rolesClaim is not null)
        {
            try
            {
                var rolesJson = System.Text.Json.JsonDocument.Parse(rolesClaim.Value);
                foreach (var role in rolesJson.RootElement.EnumerateObject())
                {
                    if (!identity.HasClaim(ClaimTypes.Role, role.Name))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
                        _logger.LogDebug("Added role claim {Role}", role.Name);
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Zitadel roles claim");
            }
        }

        return principal;
    }

    /// <summary>
    /// Extracts the tenant ID from a claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The tenant ID, or null if not found.</returns>
    public static string? GetTenantId(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        return principal.FindFirst(TenantIdClaimType)?.Value
               ?? principal.FindFirst(ZitadelOrgIdClaimType)?.Value
               ?? principal.FindFirst(ZitadelResourceOwnerClaimType)?.Value;
    }

    /// <summary>
    /// Extracts the user ID from a claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID, or null if not found.</returns>
    public static string? GetUserId(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? principal.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Extracts the roles from a claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The list of roles.</returns>
    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return Array.Empty<string>();
        }

        return principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly();
    }

    private static string? GetClaimValue(ClaimsIdentity identity, string claimType)
    {
        return identity.FindFirst(claimType)?.Value;
    }
}
