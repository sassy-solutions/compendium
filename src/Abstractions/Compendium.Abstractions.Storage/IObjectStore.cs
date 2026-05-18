// -----------------------------------------------------------------------
// <copyright file="IObjectStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Storage.Models;

namespace Compendium.Abstractions.Storage;

/// <summary>
/// Provides provider-agnostic object/blob storage operations.
/// Implementations target backends such as AWS S3, Azure Blob Storage, Google Cloud Storage,
/// MinIO, or local filesystems.
/// </summary>
public interface IObjectStore
{
    /// <summary>
    /// Uploads an object to the store, replacing any existing object at the same key.
    /// </summary>
    /// <param name="key">The object key (path) inside the bucket / container.</param>
    /// <param name="content">The readable stream containing the payload to upload.</param>
    /// <param name="metadata">Optional metadata (content type, cache control, custom headers).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the stored <see cref="ObjectInfo"/> on success, or an error.</returns>
    Task<Result<ObjectInfo>> PutAsync(
        string key,
        Stream content,
        ObjectMetadata? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an object from the store. The caller is responsible for disposing the returned <see cref="ObjectStream"/>.
    /// </summary>
    /// <param name="key">The object key to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the <see cref="ObjectStream"/> on success, or an error.</returns>
    Task<Result<ObjectStream>> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object from the store. Deleting a non-existent key returns success.
    /// </summary>
    /// <param name="key">The object key to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an object exists at the given key.
    /// </summary>
    /// <param name="key">The object key to test.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing <c>true</c> if the object exists, otherwise <c>false</c>; or an error.</returns>
    Task<Result<bool>> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a single page of objects matching the supplied options.
    /// </summary>
    /// <param name="options">Listing options (prefix, page size, continuation token). When <c>null</c>, defaults are used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the next <see cref="ListPage"/>, or an error.</returns>
    Task<Result<ListPage>> ListAsync(
        ListOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited presigned URL that allows performing the given action without further authentication.
    /// </summary>
    /// <param name="key">The object key to presign.</param>
    /// <param name="action">The action allowed by the presigned URL (<see cref="PresignedAction.Get"/> or <see cref="PresignedAction.Put"/>).</param>
    /// <param name="expiresIn">The lifetime of the presigned URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the presigned URL, or an error.</returns>
    Task<Result<Uri>> GetPresignedUrlAsync(
        string key,
        PresignedAction action,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
