// -----------------------------------------------------------------------
// <copyright file="ParsedDocument.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Represents the full result of parsing a document.
/// </summary>
/// <param name="RawText">Concatenated raw text across all pages.</param>
/// <param name="Pages">Per-page parsed content.</param>
/// <param name="Tables">Tables detected across the document.</param>
/// <param name="KeyValues">Structured key/value fields extracted by the model.</param>
/// <param name="Confidence">Aggregate confidence in the range [0.0, 1.0].</param>
public sealed record ParsedDocument(
    string RawText,
    IReadOnlyList<ParsedPage> Pages,
    IReadOnlyList<ParsedTable> Tables,
    IReadOnlyDictionary<string, ParsedField> KeyValues,
    double Confidence);
