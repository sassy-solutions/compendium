// -----------------------------------------------------------------------
// <copyright file="SecurityHeadersMiddleware.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.AspNetCore.Security;

/// <summary>
/// Middleware that adds security headers to HTTP responses.
/// Implements OWASP security best practices.
/// </summary>
/// <remarks>
/// This middleware should be registered early in the pipeline to ensure
/// headers are added to all responses, including error responses.
/// </remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The security headers configuration options.</param>
    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Invokes the middleware to add security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Add security headers before calling next middleware
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Remove information disclosure headers
        if (_options.RemoveServerHeader)
        {
            headers.Remove("Server");
        }

        if (_options.RemoveXPoweredByHeader)
        {
            headers.Remove("X-Powered-By");
        }

        // X-Content-Type-Options: Prevents MIME-sniffing
        if (_options.EnableNoSniff)
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options: Prevents clickjacking
        if (_options.EnableFrameOptions)
        {
            headers["X-Frame-Options"] = _options.FrameOptionsValue;
        }

        // Content-Security-Policy: Prevents XSS and other injection attacks
        if (_options.EnableContentSecurityPolicy && !string.IsNullOrWhiteSpace(_options.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // X-Permitted-Cross-Domain-Policies: Controls cross-domain policy files
        if (_options.EnablePermittedCrossDomainPolicies)
        {
            headers["X-Permitted-Cross-Domain-Policies"] = _options.PermittedCrossDomainPoliciesValue;
        }

        // Referrer-Policy: Controls referrer information
        if (_options.EnableReferrerPolicy)
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicyValue;
        }

        // Permissions-Policy: Controls browser features
        if (_options.EnablePermissionsPolicy && !string.IsNullOrWhiteSpace(_options.PermissionsPolicyValue))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicyValue;
        }

        // Strict-Transport-Security: Forces HTTPS
        // Note: HSTS should only be set for HTTPS responses
        if (_options.EnableHsts && context.Request.IsHttps)
        {
            var hstsHeader = $"max-age={_options.HstsMaxAgeSeconds}";

            if (_options.HstsIncludeSubDomains)
            {
                hstsHeader += "; includeSubDomains";
            }

            if (_options.HstsPreload)
            {
                hstsHeader += "; preload";
            }

            headers["Strict-Transport-Security"] = hstsHeader;
        }
    }
}
