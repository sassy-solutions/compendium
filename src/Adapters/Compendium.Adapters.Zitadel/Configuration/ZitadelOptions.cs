// -----------------------------------------------------------------------
// <copyright file="ZitadelOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Zitadel.Configuration;

/// <summary>
/// Configuration options for the Zitadel adapter.
/// </summary>
public sealed class ZitadelOptions
{
    /// <summary>
    /// Required placeholder in OIDC URI templates; substituted with the provisioned
    /// organization name at provision time.
    /// </summary>
    public const string OrganizationPlaceholder = "{organization}";

    /// <summary>
    /// Gets or sets the base URL of the Zitadel instance (e.g., "https://zitadel.example.com").
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service account JSON key for machine-to-machine authentication.
    /// </summary>
    public string? ServiceAccountKeyJson { get; set; }

    /// <summary>
    /// Gets or sets the path to the service account JSON key file.
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the client ID for OAuth2 client credentials flow.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for OAuth2 client credentials flow.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the Personal Access Token for machine-to-machine authentication.
    /// When set, used directly as Bearer token instead of client_credentials flow.
    /// </summary>
    public string? PersonalAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the project ID (required for some operations).
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the default organization ID for operations.
    /// </summary>
    public string? DefaultOrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds. Defaults to 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed requests. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the internal (cluster-local) base URL for API calls.
    /// When set, HTTP requests use this URL instead of Authority to avoid hairpin NAT issues.
    /// The Host header is set to the Authority hostname for correct routing.
    /// </summary>
    public string? InternalBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets whether to skip SSL certificate validation. Only for development.
    /// </summary>
    public bool SkipSslValidation { get; set; }

    /// <summary>
    /// Gets or sets the OIDC redirect URI template used when creating an OIDC application
    /// for a newly provisioned organization. Must contain the literal placeholder
    /// <see cref="OrganizationPlaceholder"/> which is substituted with the organization name.
    /// Example: <c>https://{organization}.admin.example.com/api/auth/callback/zitadel</c>.
    /// </summary>
    public string? RedirectUriTemplate { get; set; }

    /// <summary>
    /// Gets or sets the OIDC post-logout redirect URI template. Same semantics as
    /// <see cref="RedirectUriTemplate"/>.
    /// Example: <c>https://{organization}.admin.example.com</c>.
    /// </summary>
    public string? PostLogoutUriTemplate { get; set; }

    /// <summary>
    /// Gets the API base URL for Zitadel management API.
    /// </summary>
    public string ManagementApiUrl => $"{Authority.TrimEnd('/')}/management/v1";

    /// <summary>
    /// Gets the API base URL for Zitadel auth API.
    /// </summary>
    public string AuthApiUrl => $"{Authority.TrimEnd('/')}/auth/v1";

    /// <summary>
    /// Gets the API base URL for Zitadel user API v2.
    /// </summary>
    public string UserApiV2Url => $"{Authority.TrimEnd('/')}/v2";

    /// <summary>
    /// Gets the OAuth2 token endpoint.
    /// </summary>
    public string TokenEndpoint => $"{Authority.TrimEnd('/')}/oauth/v2/token";

    /// <summary>
    /// Gets the OAuth2 introspection endpoint.
    /// </summary>
    public string IntrospectionEndpoint => $"{Authority.TrimEnd('/')}/oauth/v2/introspect";

    /// <summary>
    /// Gets the JWKS endpoint for token validation.
    /// </summary>
    public string JwksEndpoint => $"{Authority.TrimEnd('/')}/.well-known/jwks.json";
}
