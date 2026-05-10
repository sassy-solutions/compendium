// -----------------------------------------------------------------------
// <copyright file="CrmCompany.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Models;

/// <summary>
/// Represents a CRM company / organization to be synchronized with an external CRM provider.
/// </summary>
/// <param name="ExternalId">The stable identifier of the company in the source system.</param>
/// <param name="Name">The display name of the company.</param>
/// <param name="Domain">The company's primary web domain, if known.</param>
/// <param name="Properties">Additional provider-specific properties to attach to the company.</param>
/// <param name="TenantId">The tenant that owns this company (for multi-tenant isolation).</param>
public sealed record CrmCompany(
    string ExternalId,
    string Name,
    string? Domain,
    IReadOnlyDictionary<string, object>? Properties,
    string TenantId);
