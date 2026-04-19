// -----------------------------------------------------------------------
// <copyright file="ReaderWriterLockStrategy.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Primitives;

/// <summary>
/// Reader-writer locking strategy that allows concurrent reads but exclusive writes.
/// Optimal for scenarios with high read-to-write ratios.
/// </summary>
public sealed class ReaderWriterLockStrategy : ILockingStrategy
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private bool _disposed;

    /// <inheritdoc />
    public T ExecuteRead<T>(Func<T> operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReaderWriterLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        _lock.EnterReadLock();
        try
        {
            return operation();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public void ExecuteWrite(Action operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReaderWriterLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        _lock.EnterWriteLock();
        try
        {
            operation();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteReadAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReaderWriterLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        _lock.EnterReadLock();
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteWriteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReaderWriterLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        _lock.EnterWriteLock();
        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }
}
