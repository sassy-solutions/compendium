// -----------------------------------------------------------------------
// <copyright file="ICrmSync.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Crm.Models;

namespace Compendium.Abstractions.Crm;

/// <summary>
/// Provides synchronization of contacts, companies, events and associations with an external CRM.
/// This interface is provider-agnostic and can be implemented by adapters for HubSpot, Attio,
/// Salesforce, or similar platforms.
/// </summary>
public interface ICrmSync
{
    /// <summary>
    /// Inserts or updates a contact in the target CRM.
    /// </summary>
    /// <param name="contact">The contact to upsert.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the provider's identifier for the contact or an error.</returns>
    Task<Result<string>> UpsertContactAsync(CrmContact contact, CancellationToken ct);

    /// <summary>
    /// Inserts or updates a company in the target CRM.
    /// </summary>
    /// <param name="company">The company to upsert.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the provider's identifier for the company or an error.</returns>
    Task<Result<string>> UpsertCompanyAsync(CrmCompany company, CancellationToken ct);

    /// <summary>
    /// Tracks a behavioural / lifecycle event against a CRM contact.
    /// </summary>
    /// <param name="evt">The event to record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A successful result or an error.</returns>
    Task<Result> TrackEventAsync(CrmEvent evt, CancellationToken ct);

    /// <summary>
    /// Creates an association between two CRM entities.
    /// </summary>
    /// <param name="association">The association to record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A successful result or an error.</returns>
    Task<Result> AssociateAsync(CrmAssociation association, CancellationToken ct);
}
