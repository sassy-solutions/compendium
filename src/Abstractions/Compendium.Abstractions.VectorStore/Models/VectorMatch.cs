// -----------------------------------------------------------------------
// <copyright file="VectorMatch.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Models;

/// <summary>
/// Represents a single match returned by a similarity search.
/// </summary>
/// <param name="Id">The identifier of the matched record.</param>
/// <param name="Score">The similarity score; semantics depend on the configured <see cref="DistanceMetric"/>.</param>
/// <param name="Metadata">Metadata associated with the matched record.</param>
/// <param name="TenantId">Optional tenant identifier of the matched record.</param>
public sealed record VectorMatch(
    string Id,
    float Score,
    IReadOnlyDictionary<string, object> Metadata,
    string? TenantId = null);
