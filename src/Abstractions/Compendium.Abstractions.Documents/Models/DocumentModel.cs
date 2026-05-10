// -----------------------------------------------------------------------
// <copyright file="DocumentModel.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Identifies the structured document model the parser should apply.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentModel
{
    /// <summary>
    /// Generic OCR / layout extraction with no domain-specific schema.
    /// </summary>
    Generic,

    /// <summary>
    /// Receipt model — line items, totals, taxes, merchant.
    /// </summary>
    Receipt,

    /// <summary>
    /// Invoice model — billing/shipping addresses, line items, totals, due date.
    /// </summary>
    Invoice,

    /// <summary>
    /// Identification document model — passport, driver's licence, national ID.
    /// </summary>
    IdDocument,

    /// <summary>
    /// Caller-defined custom model identified out-of-band (provider-specific id).
    /// </summary>
    Custom,
}
