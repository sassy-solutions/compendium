// -----------------------------------------------------------------------
// <copyright file="WebhookEndpoint.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks.Models;

/// <summary>
/// Represents a registered consumer endpoint that subscribes to a set of webhook events
/// for a single tenant.
/// </summary>
public sealed record WebhookEndpoint
{
    /// <summary>
    /// Gets the unique endpoint identifier (typically issued by the adapter on registration).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the destination URL the adapter will <c>POST</c> webhook deliveries to.
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// Gets the tenant identifier owning this endpoint.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the list of event names this endpoint subscribes to. An empty list means
    /// "no subscriptions" — the endpoint will not receive any deliveries.
    /// </summary>
    public required IReadOnlyList<string> EventFilters { get; init; }

    /// <summary>
    /// Gets the optional shared secret used by the adapter to sign outbound deliveries.
    /// Adapters that require signatures should fail fast if this is missing.
    /// </summary>
    public string? SigningSecret { get; init; }

    /// <summary>
    /// Gets a value indicating whether this endpoint is active and should receive deliveries.
    /// </summary>
    public bool Active { get; init; } = true;
}
