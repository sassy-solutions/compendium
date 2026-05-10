// -----------------------------------------------------------------------
// <copyright file="DocumentInput.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Represents the binary content of a document submitted for parsing.
/// </summary>
/// <param name="Stream">The document binary stream. Caller retains ownership and is responsible for disposal.</param>
/// <param name="MimeType">The MIME type of the document (e.g. <c>application/pdf</c>, <c>image/png</c>).</param>
public sealed record DocumentInput(Stream Stream, string MimeType);
