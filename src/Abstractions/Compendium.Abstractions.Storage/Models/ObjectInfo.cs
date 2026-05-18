// -----------------------------------------------------------------------
// <copyright file="ObjectInfo.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Models;

/// <summary>
/// Describes an object stored in an object store.
/// </summary>
/// <param name="Key">The object key (path) inside the bucket / container.</param>
/// <param name="Size">The size of the object in bytes.</param>
/// <param name="ETag">The provider-supplied entity tag (typically a content hash).</param>
/// <param name="ContentType">The MIME type of the object, when known.</param>
/// <param name="LastModified">The UTC instant at which the object was last modified.</param>
/// <param name="Metadata">Arbitrary user-defined metadata associated with the object.</param>
public sealed record ObjectInfo(
    string Key,
    long Size,
    string ETag,
    string? ContentType,
    DateTimeOffset LastModified,
    IReadOnlyDictionary<string, string>? Metadata = null);
