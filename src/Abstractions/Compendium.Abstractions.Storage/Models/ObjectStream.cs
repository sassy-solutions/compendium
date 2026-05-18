// -----------------------------------------------------------------------
// <copyright file="ObjectStream.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Models;

/// <summary>
/// Represents the content of an object together with its metadata, returned by
/// <see cref="IObjectStore.GetAsync"/>.
/// </summary>
/// <remarks>
/// Disposing this instance disposes the underlying <see cref="System.IO.Stream"/>.
/// Callers should always dispose the returned object (typically with <c>using</c>).
/// </remarks>
public sealed class ObjectStream : IDisposable, IAsyncDisposable
{
    private readonly Stream _content;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectStream"/> class.
    /// </summary>
    /// <param name="content">The readable content stream. Ownership is transferred to this instance.</param>
    /// <param name="info">The metadata describing the object.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> or <paramref name="info"/> is <c>null</c>.</exception>
    public ObjectStream(Stream content, ObjectInfo info)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(info);

        _content = content;
        Info = info;
    }

    /// <summary>
    /// Gets the readable stream containing the object payload.
    /// </summary>
    public Stream Content
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _content;
        }
    }

    /// <summary>
    /// Gets the metadata describing the object.
    /// </summary>
    public ObjectInfo Info { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _content.Dispose();
        _disposed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _content.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
