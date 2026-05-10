// -----------------------------------------------------------------------
// <copyright file="ParseOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Options controlling how a document is parsed.
/// </summary>
/// <param name="Model">The structured document model to apply.</param>
/// <param name="Language">Optional BCP-47 language hint (e.g. <c>en</c>, <c>fr-CA</c>); <c>null</c> = auto-detect.</param>
/// <param name="OcrOnly">When <c>true</c>, only raw OCR text is requested; structured extraction is skipped.</param>
public sealed record ParseOptions(DocumentModel Model, string? Language = null, bool OcrOnly = false);
