// -----------------------------------------------------------------------
// <copyright file="VectorRecord.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Models;

/// <summary>
/// Represents a vector record to be stored in or retrieved from a vector store.
/// </summary>
/// <param name="Id">The stable identifier of the record within its collection.</param>
/// <param name="Embedding">The dense embedding vector.</param>
/// <param name="Metadata">Arbitrary metadata indexed alongside the vector and usable in filters.</param>
/// <param name="TenantId">Optional tenant identifier used to enforce per-tenant isolation.</param>
public sealed record VectorRecord(
    string Id,
    ReadOnlyMemory<float> Embedding,
    IReadOnlyDictionary<string, object> Metadata,
    string? TenantId = null);
