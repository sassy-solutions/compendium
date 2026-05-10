// -----------------------------------------------------------------------
// <copyright file="CrmContact.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Models;

/// <summary>
/// Represents a CRM contact to be synchronized with an external CRM provider.
/// </summary>
/// <param name="ExternalId">The stable identifier of the contact in the source system.</param>
/// <param name="Email">The contact's primary email address.</param>
/// <param name="FirstName">The contact's first name, if known.</param>
/// <param name="LastName">The contact's last name, if known.</param>
/// <param name="Properties">Additional provider-specific properties to attach to the contact.</param>
/// <param name="TenantId">The tenant that owns this contact (for multi-tenant isolation).</param>
public sealed record CrmContact(
    string ExternalId,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyDictionary<string, object>? Properties,
    string TenantId);
