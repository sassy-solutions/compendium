// -----------------------------------------------------------------------
// <copyright file="CrmEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Models;

/// <summary>
/// Represents a behavioural / lifecycle event to be tracked against a CRM contact.
/// </summary>
/// <param name="Name">The event name (e.g. "checkout_completed", "trial_started").</param>
/// <param name="ContactExternalId">The external identifier of the contact the event is associated with.</param>
/// <param name="Properties">Additional provider-specific event properties.</param>
/// <param name="Timestamp">The moment the event occurred.</param>
/// <param name="TenantId">The tenant that owns this event (for multi-tenant isolation).</param>
public sealed record CrmEvent(
    string Name,
    string ContactExternalId,
    IReadOnlyDictionary<string, object>? Properties,
    DateTimeOffset Timestamp,
    string TenantId);
