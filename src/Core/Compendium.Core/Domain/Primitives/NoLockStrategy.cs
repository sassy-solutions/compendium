// -----------------------------------------------------------------------
// <copyright file="NoLockStrategy.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Primitives;

/// <summary>
/// No-locking strategy that provides maximum performance for single-threaded scenarios.
/// WARNING: Should only be used when thread safety is guaranteed by external means.
/// </summary>
public sealed class NoLockStrategy : ILockingStrategy
{
    private bool _disposed;

    /// <inheritdoc />
    public T ExecuteRead<T>(Func<T> operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NoLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);
        return operation();
    }

    /// <inheritdoc />
    public void ExecuteWrite(Action operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NoLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);
        operation();
    }

    /// <inheritdoc />
    public async Task<T> ExecuteReadAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NoLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);
        return await operation().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExecuteWriteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NoLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);
        await operation().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
