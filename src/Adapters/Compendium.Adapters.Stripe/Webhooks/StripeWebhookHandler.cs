// -----------------------------------------------------------------------
// <copyright file="StripeWebhookHandler.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Stripe.Configuration;

namespace Compendium.Adapters.Stripe.Webhooks;

/// <summary>
/// Handles webhook events from Stripe with HMAC-SHA256 signature validation
/// via <see cref="EventUtility.ConstructEvent(string, string, string, long, bool)"/>.
/// </summary>
internal sealed class StripeWebhookHandler : IPaymentWebhookHandler
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeWebhookHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookHandler"/> class.
    /// </summary>
    public StripeWebhookHandler(
        IOptions<StripeOptions> options,
        ILogger<StripeWebhookHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<WebhookProcessingResult>> ProcessWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signature);

        _logger.LogDebug("Processing Stripe webhook");

        Event stripeEvent;

        if (string.IsNullOrEmpty(_options.WebhookSigningSecret))
        {
            _logger.LogWarning(
                "Stripe webhook signing secret not configured — skipping signature validation (dev mode)");

            try
            {
                stripeEvent = EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
            }
            catch (Exception ex) when (ex is StripeException or JsonException)
            {
                _logger.LogError(ex, "Failed to parse Stripe webhook payload");
                return Task.FromResult(
                    Result.Failure<WebhookProcessingResult>(
                        BillingErrors.WebhookProcessingFailed("Invalid JSON payload")));
            }
        }
        else
        {
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    _options.WebhookSigningSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook signature");
                return Task.FromResult(
                    Result.Failure<WebhookProcessingResult>(BillingErrors.InvalidWebhookSignature));
            }
        }

        var (resourceType, resourceId, tenantId, extractedData) = ExtractResourceMetadata(stripeEvent);

        _logger.LogInformation(
            "Processed Stripe webhook {EventId} ({EventType}) → {ResourceType}/{ResourceId}",
            stripeEvent.Id,
            stripeEvent.Type,
            resourceType,
            resourceId);

        var result = new WebhookProcessingResult
        {
            Processed = true,
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ResourceType = resourceType,
            ResourceId = resourceId,
            TenantId = tenantId,
            WasDuplicate = false,
            ExtractedData = extractedData
        };

        return Task.FromResult(Result.Success(result));
    }

    private static (string? resourceType, string? resourceId, string? tenantId, IReadOnlyDictionary<string, object>? data)
        ExtractResourceMetadata(Event stripeEvent)
    {
        var obj = stripeEvent.Data?.Object;
        string? resourceType = null;
        string? resourceId = null;
        string? tenantId = null;
        IDictionary<string, string>? metadata = null;

        switch (obj)
        {
            case global::Stripe.Subscription sub:
                resourceType = "subscription";
                resourceId = sub.Id;
                metadata = sub.Metadata;
                break;
            case global::Stripe.Customer cus:
                resourceType = "customer";
                resourceId = cus.Id;
                metadata = cus.Metadata;
                break;
            case global::Stripe.Checkout.Session sess:
                resourceType = "checkout_session";
                resourceId = sess.Id;
                metadata = sess.Metadata;
                break;
            case global::Stripe.Invoice inv:
                resourceType = "invoice";
                resourceId = inv.Id;
                metadata = inv.Metadata;
                break;
            case IHasId hasId when obj is IHasObject hasObj:
                resourceType = hasObj.Object;
                resourceId = hasId.Id;
                break;
            case IHasObject hasObj:
                resourceType = hasObj.Object;
                break;
        }

        if (metadata is not null && metadata.TryGetValue("tenant_id", out var tid))
        {
            tenantId = tid;
        }

        IReadOnlyDictionary<string, object>? extracted = null;
        if (metadata is { Count: > 0 })
        {
            var dict = new Dictionary<string, object>(metadata.Count, StringComparer.Ordinal);
            foreach (var kvp in metadata)
            {
                dict[$"metadata_{kvp.Key}"] = kvp.Value;
            }

            extracted = dict;
        }

        return (resourceType, resourceId, tenantId, extracted);
    }
}
