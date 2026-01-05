// -----------------------------------------------------------------------
// <copyright file="SecurityHeadersOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AspNetCore.Security;

/// <summary>
/// Configuration options for security headers middleware.
/// Implements OWASP security best practices for HTTP headers.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable HTTP Strict Transport Security (HSTS).
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// HSTS forces browsers to use HTTPS instead of HTTP.
    /// Recommended max-age: 31536000 seconds (1 year).
    /// </remarks>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// Gets or sets the HSTS max-age in seconds.
    /// Default: 31536000 (1 year).
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000;

    /// <summary>
    /// Gets or sets a value indicating whether to include subdomains in HSTS.
    /// Default: true.
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable HSTS preload.
    /// Default: false (requires manual submission to HSTS preload list).
    /// </summary>
    public bool HstsPreload { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to set X-Content-Type-Options to nosniff.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Prevents browsers from MIME-sniffing responses, reducing XSS attacks.
    /// </remarks>
    public bool EnableNoSniff { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to set X-Frame-Options.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Prevents clickjacking attacks by controlling iframe embedding.
    /// </remarks>
    public bool EnableFrameOptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-Frame-Options value.
    /// Default: DENY.
    /// </summary>
    /// <remarks>
    /// Options: DENY, SAMEORIGIN, ALLOW-FROM uri.
    /// DENY is most secure but prevents all iframe embedding.
    /// </remarks>
    public string FrameOptionsValue { get; set; } = "DENY";

    /// <summary>
    /// Gets or sets a value indicating whether to set Content-Security-Policy.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// CSP helps prevent XSS, clickjacking, and other code injection attacks.
    /// </remarks>
    public bool EnableContentSecurityPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Content-Security-Policy directive.
    /// Default: Restrictive policy suitable for APIs.
    /// </summary>
    /// <remarks>
    /// For APIs: "default-src 'none'; frame-ancestors 'none'"
    /// For web apps: Customize based on your needs.
    /// See: https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP
    /// </remarks>
    public string ContentSecurityPolicy { get; set; } = "default-src 'none'; frame-ancestors 'none'";

    /// <summary>
    /// Gets or sets a value indicating whether to set X-Permitted-Cross-Domain-Policies.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Controls cross-domain policy files (e.g., Adobe Flash, PDF).
    /// Recommended: "none" to prevent cross-domain data loading.
    /// </remarks>
    public bool EnablePermittedCrossDomainPolicies { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-Permitted-Cross-Domain-Policies value.
    /// Default: none.
    /// </summary>
    public string PermittedCrossDomainPoliciesValue { get; set; } = "none";

    /// <summary>
    /// Gets or sets a value indicating whether to set Referrer-Policy.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Controls how much referrer information is included with requests.
    /// </remarks>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Referrer-Policy value.
    /// Default: strict-origin-when-cross-origin.
    /// </summary>
    /// <remarks>
    /// Options: no-referrer, no-referrer-when-downgrade, origin,
    /// origin-when-cross-origin, same-origin, strict-origin,
    /// strict-origin-when-cross-origin, unsafe-url.
    /// </remarks>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Gets or sets a value indicating whether to set Permissions-Policy.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Controls browser features and APIs (formerly Feature-Policy).
    /// </remarks>
    public bool EnablePermissionsPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Permissions-Policy directive.
    /// Default: Restrictive policy disabling all features.
    /// </summary>
    /// <remarks>
    /// Example: "geolocation=(), microphone=(), camera=()"
    /// For APIs, typically disable all: "geolocation=(), microphone=(), camera=(), payment=(), usb=()"
    /// </remarks>
    public string PermissionsPolicyValue { get; set; } = "geolocation=(), microphone=(), camera=(), payment=(), usb=()";

    /// <summary>
    /// Gets or sets a value indicating whether to remove the Server header.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Removing the Server header reduces information disclosure.
    /// Prevents attackers from knowing server technology.
    /// </remarks>
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to remove the X-Powered-By header.
    /// Default: true.
    /// </summary>
    /// <remarks>
    /// Removes technology fingerprinting headers.
    /// </remarks>
    public bool RemoveXPoweredByHeader { get; set; } = true;

    /// <summary>
    /// Gets a default configuration suitable for REST APIs.
    /// </summary>
    public static SecurityHeadersOptions ForApi()
    {
        return new SecurityHeadersOptions
        {
            ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'",
            FrameOptionsValue = "DENY",
            HstsMaxAgeSeconds = 31536000,
            HstsIncludeSubDomains = true,
            HstsPreload = false,
            PermissionsPolicyValue = "geolocation=(), microphone=(), camera=(), payment=(), usb=()"
        };
    }

    /// <summary>
    /// Gets a default configuration suitable for web applications with static content.
    /// </summary>
    public static SecurityHeadersOptions ForWebApp()
    {
        return new SecurityHeadersOptions
        {
            ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'",
            FrameOptionsValue = "SAMEORIGIN",
            HstsMaxAgeSeconds = 31536000,
            HstsIncludeSubDomains = true,
            HstsPreload = false,
            PermissionsPolicyValue = "geolocation=(), microphone=(), camera=()"
        };
    }
}
