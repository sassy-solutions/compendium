// -----------------------------------------------------------------------
// <copyright file="WebhookProcessingResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Models;

/// <summary>
/// Represents the result of processing a payment webhook.
/// </summary>
public sealed record WebhookProcessingResult
{
    /// <summary>
    /// Gets or initializes whether the webhook was processed successfully.
    /// </summary>
    public required bool Processed { get; init; }

    /// <summary>
    /// Gets or initializes the type of webhook event that was processed.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Gets or initializes the unique identifier of the webhook event.
    /// </summary>
    public string? EventId { get; init; }

    /// <summary>
    /// Gets or initializes the resource type affected by the webhook (e.g., "subscription", "order").
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Gets or initializes the resource ID affected by the webhook.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets or initializes the tenant ID extracted from the webhook payload.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or initializes whether the webhook was a duplicate (already processed).
    /// </summary>
    public bool WasDuplicate { get; init; }

    /// <summary>
    /// Gets or initializes additional data extracted from the webhook.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ExtractedData { get; init; }

    /// <summary>
    /// Gets or initializes the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful processing result.
    /// </summary>
    public static WebhookProcessingResult Success(string eventType, string? eventId = null) =>
        new()
        {
            Processed = true,
            EventType = eventType,
            EventId = eventId
        };

    /// <summary>
    /// Creates a duplicate (already processed) result.
    /// </summary>
    public static WebhookProcessingResult Duplicate(string eventType, string? eventId = null) =>
        new()
        {
            Processed = true,
            WasDuplicate = true,
            EventType = eventType,
            EventId = eventId
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static WebhookProcessingResult Failure(string errorMessage) =>
        new()
        {
            Processed = false,
            ErrorMessage = errorMessage
        };
}
