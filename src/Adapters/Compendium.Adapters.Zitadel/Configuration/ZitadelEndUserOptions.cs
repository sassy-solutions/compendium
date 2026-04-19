// -----------------------------------------------------------------------
// <copyright file="ZitadelEndUserOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Zitadel.Configuration;

/// <summary>
/// Configuration options for the Zitadel End-User adapter.
/// </summary>
/// <remarks>
/// <para>
/// This configuration is separate from <see cref="ZitadelOptions"/> because
/// End-Users use a different Zitadel Project and Application than PlatformUsers.
/// </para>
/// <para>
/// End-Users:
/// <list type="bullet">
///   <item>Self-register through Zitadel</item>
///   <item>Are scoped to specific tenants and applications</item>
///   <item>Have different permission structures (app-based vs platform-based)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ZitadelEndUserOptions
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "ZitadelEndUser";

    /// <summary>
    /// Gets or sets the base URL of the Zitadel instance (e.g., "https://zitadel.example.com").
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID for the End-User OIDC application.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for the End-User OIDC application.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the project ID for the End-User project in Zitadel.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the expected audience claim in End-User tokens.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets whether self-registration is enabled for End-Users.
    /// </summary>
    public bool SelfRegistrationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the secret used to validate Zitadel webhook signatures.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Gets or sets the webhook signature header name.
    /// </summary>
    public string WebhookSignatureHeader { get; set; } = "X-Zitadel-Signature";

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to skip SSL certificate validation. Only for development.
    /// </summary>
    public bool SkipSslValidation { get; set; }

    /// <summary>
    /// Gets or sets the default subscription tier for new End-Users.
    /// </summary>
    public string DefaultSubscriptionTier { get; set; } = "Free";

    /// <summary>
    /// Gets or sets the mapping from Zitadel organization IDs to application tenant IDs.
    /// Key: Zitadel Org ID, Value: Tenant ID (Guid string).
    /// </summary>
    /// <remarks>
    /// If empty, the system will attempt to look up the mapping dynamically
    /// from the tenant's stored Zitadel organization reference.
    /// </remarks>
    public Dictionary<string, string> OrgToTenantMapping { get; set; } = new();

    /// <summary>
    /// Gets the OAuth2 authorization endpoint.
    /// </summary>
    public string AuthorizationEndpoint => $"{Authority.TrimEnd('/')}/oauth/v2/authorize";

    /// <summary>
    /// Gets the OAuth2 token endpoint.
    /// </summary>
    public string TokenEndpoint => $"{Authority.TrimEnd('/')}/oauth/v2/token";

    /// <summary>
    /// Gets the OAuth2 userinfo endpoint.
    /// </summary>
    public string UserInfoEndpoint => $"{Authority.TrimEnd('/')}/oidc/v1/userinfo";

    /// <summary>
    /// Gets the JWKS endpoint for token validation.
    /// </summary>
    public string JwksEndpoint => $"{Authority.TrimEnd('/')}/.well-known/jwks.json";

    /// <summary>
    /// Gets the OpenID Connect discovery endpoint.
    /// </summary>
    public string DiscoveryEndpoint => $"{Authority.TrimEnd('/')}/.well-known/openid-configuration";
}
