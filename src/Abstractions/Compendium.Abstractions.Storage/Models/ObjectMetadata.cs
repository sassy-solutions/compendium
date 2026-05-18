// -----------------------------------------------------------------------
// <copyright file="ObjectMetadata.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Models;

/// <summary>
/// Describes metadata applied to an object on upload.
/// </summary>
/// <param name="ContentType">The MIME type of the object (for example <c>image/png</c>).</param>
/// <param name="CacheControl">The HTTP <c>Cache-Control</c> header to apply when serving the object.</param>
/// <param name="ContentDisposition">The HTTP <c>Content-Disposition</c> header to apply when serving the object.</param>
/// <param name="Custom">Arbitrary user-defined metadata stored alongside the object.</param>
public sealed record ObjectMetadata(
    string? ContentType = null,
    string? CacheControl = null,
    string? ContentDisposition = null,
    IReadOnlyDictionary<string, string>? Custom = null);
