// -----------------------------------------------------------------------
// <copyright file="ListOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Models;

/// <summary>
/// Specifies options for listing objects in an object store.
/// </summary>
/// <param name="Prefix">If set, only objects whose key starts with this prefix are returned.</param>
/// <param name="MaxKeys">The maximum number of keys to return in a single page. Defaults to <c>1000</c>.</param>
/// <param name="ContinuationToken">An opaque token returned by a previous list call to fetch the next page.</param>
public sealed record ListOptions(
    string? Prefix = null,
    int MaxKeys = 1000,
    string? ContinuationToken = null);

/// <summary>
/// Represents a single page of results returned by <see cref="IObjectStore.ListAsync"/>.
/// </summary>
/// <param name="Items">The objects in this page, in provider-defined order.</param>
/// <param name="NextContinuationToken">An opaque token used to fetch the next page, or <c>null</c> if no more pages remain.</param>
public sealed record ListPage(
    IReadOnlyList<ObjectInfo> Items,
    string? NextContinuationToken = null);
