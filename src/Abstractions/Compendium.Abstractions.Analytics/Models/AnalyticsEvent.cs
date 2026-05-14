// -----------------------------------------------------------------------
// <copyright file="AnalyticsEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Analytics.Models;

/// <summary>
/// Represents a product analytics event captured for a specific user and tenant.
/// </summary>
/// <param name="Name">The event name (e.g. "order_placed", "user_signed_up").</param>
/// <param name="DistinctId">The stable identifier for the user or entity triggering the event.</param>
/// <param name="TenantId">The tenant identifier this event belongs to, enabling multi-tenant isolation.</param>
/// <param name="Timestamp">The instant the event occurred.</param>
/// <param name="Properties">Additional structured properties describing the event.</param>
public sealed record AnalyticsEvent(
    string Name,
    string DistinctId,
    string TenantId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> Properties);
