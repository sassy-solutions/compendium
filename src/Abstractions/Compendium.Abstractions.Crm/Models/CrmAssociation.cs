// -----------------------------------------------------------------------
// <copyright file="CrmAssociation.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Models;

/// <summary>
/// Represents a directional association between two CRM entities (e.g. contact -> company).
/// </summary>
/// <param name="FromType">The type of the source entity (e.g. "contact", "company").</param>
/// <param name="FromId">The external identifier of the source entity.</param>
/// <param name="ToType">The type of the target entity.</param>
/// <param name="ToId">The external identifier of the target entity.</param>
/// <param name="Role">Optional role / label describing the relationship (e.g. "primary_contact").</param>
public sealed record CrmAssociation(
    string FromType,
    string FromId,
    string ToType,
    string ToId,
    string? Role);
