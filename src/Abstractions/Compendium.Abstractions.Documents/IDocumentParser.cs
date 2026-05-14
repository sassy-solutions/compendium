// -----------------------------------------------------------------------
// <copyright file="IDocumentParser.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Documents.Models;

namespace Compendium.Abstractions.Documents;

/// <summary>
/// Provides document parsing operations including OCR and structured extraction.
/// This interface is provider-agnostic and can be implemented by various providers
/// such as Azure Document Intelligence, AWS Textract, Google Document AI, or local engines.
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Parses the supplied document and returns extracted text, pages, tables and key/value fields.
    /// </summary>
    /// <param name="doc">The document input (binary stream + MIME type).</param>
    /// <param name="opts">The parsing options (target model, language, OCR-only mode).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ParsedDocument"/> on success or an <see cref="Error"/> on failure.</returns>
    Task<Result<ParsedDocument>> ParseAsync(DocumentInput doc, ParseOptions opts, CancellationToken ct);
}
