// -----------------------------------------------------------------------
// <copyright file="ParsedTable.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Represents a tabular region extracted from a document.
/// </summary>
/// <param name="PageNumber">1-based page index where the table was found.</param>
/// <param name="Rows">Row-major cell content; outer list = rows, inner list = cells in the row.</param>
public sealed record ParsedTable(int PageNumber, IReadOnlyList<IReadOnlyList<string>> Rows);
