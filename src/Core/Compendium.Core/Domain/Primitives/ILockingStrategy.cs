// -----------------------------------------------------------------------
// <copyright file="ILockingStrategy.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Primitives;

/// <summary>
/// Defines a strategy for locking operations in aggregate roots.
/// Enables different locking approaches based on performance requirements.
/// </summary>
public interface ILockingStrategy : IDisposable
{
    /// <summary>
    /// Executes a read operation with appropriate locking.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    T ExecuteRead<T>(Func<T> operation);

    /// <summary>
    /// Executes a write operation with appropriate locking.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    void ExecuteWrite(Action operation);

    /// <summary>
    /// Executes an async read operation with appropriate locking.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteReadAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an async write operation with appropriate locking.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteWriteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
