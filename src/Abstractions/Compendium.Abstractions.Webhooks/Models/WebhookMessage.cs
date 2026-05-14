// -----------------------------------------------------------------------
// <copyright file="WebhookMessage.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks.Models;

/// <summary>
/// Represents a webhook message to be fanned out to subscribed consumer endpoints.
/// </summary>
public sealed record WebhookMessage
{
    /// <summary>
    /// Gets the idempotency key for this message. Adapters MUST treat repeated sends
    /// with the same <see cref="Id"/> as a single delivery.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the logical event name (e.g. <c>order.created</c>) used by consumers
    /// to filter subscriptions.
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// Gets the event payload to be serialized and delivered to consumers.
    /// </summary>
    public required object Payload { get; init; }

    /// <summary>
    /// Gets the tenant identifier owning this message.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the optional source domain event identifier (e.g. the event-store event id)
    /// for traceability between integration events and outbound webhooks.
    /// </summary>
    public string? EventId { get; init; }

    /// <summary>
    /// Gets the timestamp at which the message was produced.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
