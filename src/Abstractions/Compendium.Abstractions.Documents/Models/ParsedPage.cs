// -----------------------------------------------------------------------
// <copyright file="ParsedPage.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Represents a single parsed page of a document.
/// </summary>
/// <param name="PageNumber">1-based page index.</param>
/// <param name="Text">Raw text extracted from the page.</param>
/// <param name="Confidence">Provider-reported confidence in the range [0.0, 1.0].</param>
public sealed record ParsedPage(int PageNumber, string Text, double Confidence);
