// -----------------------------------------------------------------------
// <copyright file="TenantPropagatingDelegatingHandler.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Compendium.Multitenancy.Http;

/// <summary>
/// An HTTP message handler that propagates tenant context to outgoing requests.
/// Adds tenant-related headers to all outgoing HTTP requests.
/// </summary>
public sealed class TenantPropagatingDelegatingHandler : DelegatingHandler
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly TenantPropagationOptions _options;
    private readonly ILogger<TenantPropagatingDelegatingHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPropagatingDelegatingHandler"/> class.
    /// </summary>
    /// <param name="tenantContextAccessor">The tenant context accessor.</param>
    /// <param name="options">The propagation options.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantPropagatingDelegatingHandler(
        ITenantContextAccessor tenantContextAccessor,
        TenantPropagationOptions options,
        ILogger<TenantPropagatingDelegatingHandler> logger)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends an HTTP request with tenant context headers.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext.HasTenant)
        {
            var tenantId = tenantContext.TenantId!;

            // Add tenant ID header
            if (!request.Headers.Contains(_options.TenantIdHeaderName))
            {
                request.Headers.Add(_options.TenantIdHeaderName, tenantId);
                _logger.LogDebug(
                    "Added tenant header {HeaderName}={TenantId} to request {RequestUri}",
                    _options.TenantIdHeaderName,
                    tenantId,
                    request.RequestUri);
            }

            // Add tenant name header if available and enabled
            if (_options.IncludeTenantName && tenantContext.TenantName is not null)
            {
                if (!request.Headers.Contains(_options.TenantNameHeaderName))
                {
                    request.Headers.Add(_options.TenantNameHeaderName, tenantContext.TenantName);
                }
            }

            // Add any custom headers from tenant properties if configured
            if (_options.PropagateCustomProperties && tenantContext.CurrentTenant?.Properties is not null)
            {
                foreach (var property in tenantContext.CurrentTenant.Properties)
                {
                    if (property.Value is string stringValue &&
                        _options.AllowedPropertyHeaders.Contains(property.Key))
                    {
                        var headerName = $"X-Tenant-{property.Key}";
                        if (!request.Headers.Contains(headerName))
                        {
                            request.Headers.Add(headerName, stringValue);
                        }
                    }
                }
            }
        }
        else
        {
            _logger.LogDebug(
                "No tenant context available for request {RequestUri}",
                request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Configuration options for tenant context propagation.
/// </summary>
public sealed class TenantPropagationOptions
{
    /// <summary>
    /// Gets or sets the header name for the tenant ID.
    /// Default is "X-Tenant-ID".
    /// </summary>
    public string TenantIdHeaderName { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets the header name for the tenant name.
    /// Default is "X-Tenant-Name".
    /// </summary>
    public string TenantNameHeaderName { get; set; } = "X-Tenant-Name";

    /// <summary>
    /// Gets or sets a value indicating whether to include the tenant name header.
    /// Default is false.
    /// </summary>
    public bool IncludeTenantName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to propagate custom tenant properties as headers.
    /// Default is false.
    /// </summary>
    public bool PropagateCustomProperties { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed tenant property names to propagate as headers.
    /// Only properties in this list will be propagated when PropagateCustomProperties is true.
    /// </summary>
    public HashSet<string> AllowedPropertyHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
