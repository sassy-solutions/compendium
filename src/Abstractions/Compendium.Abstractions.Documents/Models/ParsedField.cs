// -----------------------------------------------------------------------
// <copyright file="ParsedField.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Models;

/// <summary>
/// Represents a single key/value field extracted from a document.
/// </summary>
/// <param name="Value">The extracted field value (raw string — caller may parse to typed value).</param>
/// <param name="Confidence">Provider-reported confidence in the range [0.0, 1.0].</param>
public sealed record ParsedField(string Value, double Confidence);
