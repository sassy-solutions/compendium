// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyWebhookHandler.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http.Models;

namespace Compendium.Adapters.LemonSqueezy.Webhooks;

/// <summary>
/// Handles webhook events from LemonSqueezy with HMAC-SHA256 signature validation.
/// </summary>
internal sealed class LemonSqueezyWebhookHandler : IPaymentWebhookHandler
{
    private readonly LemonSqueezyOptions _options;
    private readonly ILogger<LemonSqueezyWebhookHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LemonSqueezyWebhookHandler"/> class.
    /// </summary>
    public LemonSqueezyWebhookHandler(
        IOptions<LemonSqueezyOptions> options,
        ILogger<LemonSqueezyWebhookHandler> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<Result<WebhookProcessingResult>> ProcessWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signature);

        _logger.LogDebug("Processing LemonSqueezy webhook");

        // Validate signature using HMAC-SHA256
        if (!ValidateSignature(payload, signature))
        {
            _logger.LogWarning("Invalid webhook signature");
            return BillingErrors.InvalidWebhookSignature;
        }

        // Parse the webhook payload
        LsWebhookPayload? webhookPayload;
        try
        {
            webhookPayload = JsonSerializer.Deserialize<LsWebhookPayload>(payload, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse webhook payload");
            return BillingErrors.WebhookProcessingFailed("Invalid JSON payload");
        }

        if (webhookPayload?.Meta is null)
        {
            _logger.LogWarning("Webhook payload missing meta information");
            return BillingErrors.WebhookProcessingFailed("Missing meta information");
        }

        var eventName = webhookPayload.Meta.EventName ?? "unknown";
        var customData = webhookPayload.Meta.CustomData;

        _logger.LogInformation("Processing webhook event: {EventName}", eventName);

        // Extract resource information from the data
        string? resourceType = null;
        string? resourceId = null;
        string? tenantId = null;

        if (webhookPayload.Data.HasValue)
        {
            try
            {
                var data = webhookPayload.Data.Value;

                if (data.TryGetProperty("type", out var typeElement))
                {
                    resourceType = typeElement.GetString();
                }

                if (data.TryGetProperty("id", out var idElement))
                {
                    resourceId = idElement.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract resource information from webhook data");
            }
        }

        // Extract tenant ID from custom data if present
        if (customData?.TryGetValue("tenant_id", out var tenantIdValue) == true)
        {
            tenantId = tenantIdValue?.ToString();
        }

        // Process specific event types
        var processingResult = await ProcessEventAsync(eventName, webhookPayload, cancellationToken);

        if (processingResult.IsFailure)
        {
            return processingResult.Error;
        }

        var result = new WebhookProcessingResult
        {
            Processed = true,
            EventType = eventName,
            EventId = Guid.NewGuid().ToString(), // LemonSqueezy doesn't provide event IDs
            ResourceType = resourceType,
            ResourceId = resourceId,
            TenantId = tenantId,
            WasDuplicate = false,
            ExtractedData = ExtractEventData(eventName, webhookPayload)
        };

        _logger.LogInformation("Successfully processed webhook event: {EventName}, Resource: {ResourceType}/{ResourceId}",
            eventName, resourceType, resourceId);

        return result;
    }

    /// <summary>
    /// Validates the webhook signature using HMAC-SHA256.
    /// </summary>
    private bool ValidateSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_options.WebhookSigningSecret))
        {
            _logger.LogWarning("Webhook signing secret not configured, skipping signature validation");
            return true; // Allow in development
        }

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSigningSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

            // LemonSqueezy signature format varies, try both with and without prefix
            var providedSignature = signature
                .Replace("sha256=", "", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();

            var isValid = computedSignature.Equals(providedSignature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogDebug("Signature mismatch. Expected: {Expected}, Got: {Got}",
                    computedSignature[..8] + "...",
                    providedSignature.Length > 8 ? providedSignature[..8] + "..." : providedSignature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Processes specific event types.
    /// </summary>
    private Task<Result> ProcessEventAsync(
        string eventName,
        LsWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        // Handle different event types
        // This is where you would emit domain events or call services
        return eventName switch
        {
            "subscription_created" => HandleSubscriptionCreatedAsync(payload, cancellationToken),
            "subscription_updated" => HandleSubscriptionUpdatedAsync(payload, cancellationToken),
            "subscription_cancelled" => HandleSubscriptionCancelledAsync(payload, cancellationToken),
            "subscription_resumed" => HandleSubscriptionResumedAsync(payload, cancellationToken),
            "subscription_expired" => HandleSubscriptionExpiredAsync(payload, cancellationToken),
            "subscription_paused" => HandleSubscriptionPausedAsync(payload, cancellationToken),
            "subscription_unpaused" => HandleSubscriptionUnpausedAsync(payload, cancellationToken),
            "subscription_payment_success" => HandlePaymentSuccessAsync(payload, cancellationToken),
            "subscription_payment_failed" => HandlePaymentFailedAsync(payload, cancellationToken),
            "subscription_payment_recovered" => HandlePaymentRecoveredAsync(payload, cancellationToken),
            "order_created" => HandleOrderCreatedAsync(payload, cancellationToken),
            "order_refunded" => HandleOrderRefundedAsync(payload, cancellationToken),
            "license_key_created" => HandleLicenseKeyCreatedAsync(payload, cancellationToken),
            "license_key_updated" => HandleLicenseKeyUpdatedAsync(payload, cancellationToken),
            _ => Task.FromResult(Result.Success()) // Acknowledge unknown events
        };
    }

    // ============================================================================
    // Event Handlers
    // ============================================================================

    private Task<Result> HandleSubscriptionCreatedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_created event");
        // Domain event emission would happen here
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionUpdatedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_updated event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionCancelledAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_cancelled event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionResumedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_resumed event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionExpiredAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_expired event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionPausedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_paused event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleSubscriptionUnpausedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_unpaused event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandlePaymentSuccessAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_payment_success event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandlePaymentFailedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_payment_failed event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandlePaymentRecoveredAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling subscription_payment_recovered event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleOrderCreatedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling order_created event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleOrderRefundedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling order_refunded event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleLicenseKeyCreatedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling license_key_created event");
        return Task.FromResult(Result.Success());
    }

    private Task<Result> HandleLicenseKeyUpdatedAsync(LsWebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Handling license_key_updated event");
        return Task.FromResult(Result.Success());
    }

    // ============================================================================
    // Data Extraction
    // ============================================================================

    private IReadOnlyDictionary<string, object>? ExtractEventData(string eventName, LsWebhookPayload payload)
    {
        if (!payload.Data.HasValue)
        {
            return null;
        }

        var extractedData = new Dictionary<string, object>();

        try
        {
            var data = payload.Data.Value;

            // Extract common fields
            if (data.TryGetProperty("id", out var id))
            {
                extractedData["resource_id"] = id.GetString() ?? string.Empty;
            }

            if (data.TryGetProperty("type", out var type))
            {
                extractedData["resource_type"] = type.GetString() ?? string.Empty;
            }

            // Extract attributes
            if (data.TryGetProperty("attributes", out var attributes))
            {
                if (attributes.TryGetProperty("status", out var status))
                {
                    extractedData["status"] = status.GetString() ?? string.Empty;
                }

                if (attributes.TryGetProperty("customer_id", out var customerId))
                {
                    extractedData["customer_id"] = customerId.GetInt32().ToString();
                }

                if (attributes.TryGetProperty("product_id", out var productId))
                {
                    extractedData["product_id"] = productId.GetInt32().ToString();
                }

                if (attributes.TryGetProperty("variant_id", out var variantId))
                {
                    extractedData["variant_id"] = variantId.GetInt32().ToString();
                }

                if (attributes.TryGetProperty("user_email", out var userEmail))
                {
                    extractedData["user_email"] = userEmail.GetString() ?? string.Empty;
                }
            }

            // Add custom data from meta
            if (payload.Meta?.CustomData is not null)
            {
                foreach (var kvp in payload.Meta.CustomData)
                {
                    extractedData[$"custom_{kvp.Key}"] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting event data");
        }

        return extractedData.Count > 0 ? extractedData : null;
    }
}
