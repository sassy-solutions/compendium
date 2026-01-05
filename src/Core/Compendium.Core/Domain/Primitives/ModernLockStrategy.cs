// -----------------------------------------------------------------------
// <copyright file="ModernLockStrategy.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Primitives;

/// <summary>
/// Modern locking strategy using .NET 9's new Lock type.
/// Provides improved performance over traditional lock statements.
/// Note: For async operations, falls back to SemaphoreSlim due to Lock limitations.
/// </summary>
public sealed class ModernLockStrategy : ILockingStrategy
{
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _asyncSemaphore = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public T ExecuteRead<T>(Func<T> operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModernLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        using (_lock.EnterScope())
        {
            return operation();
        }
    }

    /// <inheritdoc />
    public void ExecuteWrite(Action operation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModernLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        using (_lock.EnterScope())
        {
            operation();
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteReadAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModernLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        // Use SemaphoreSlim for async operations since .NET 9 Lock cannot cross await boundaries
        await _asyncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            _asyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteWriteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModernLockStrategy));
        }

        ArgumentNullException.ThrowIfNull(operation);

        // Use SemaphoreSlim for async operations since .NET 9 Lock cannot cross await boundaries
        await _asyncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _asyncSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            // Lock type in .NET 9 is not disposable, it's handled by GC
            _asyncSemaphore.Dispose();
            _disposed = true;
        }
    }
}
