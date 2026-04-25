// -----------------------------------------------------------------------
// <copyright file="IProcessManagerRepository.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Sagas.ProcessManagers;

/// <summary>
/// Persistence port for <see cref="IProcessManager"/> instances. Adapters (e.g. PostgreSQL,
/// in-memory) implement this contract.
/// </summary>
public interface IProcessManagerRepository
{
    /// <summary>
    /// Retrieves a process manager by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the process manager.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result with the process manager, or a not-found error.</returns>
    Task<Result<IProcessManager>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the given process manager.
    /// </summary>
    /// <remarks>
    /// The contract does not currently expose a version/ETag on <see cref="IProcessManager"/>,
    /// so optimistic concurrency cannot be expressed at this layer; durable adapters are
    /// nonetheless encouraged to track an internal version column and surface conflicts as
    /// <see cref="Error.Conflict"/> when they detect concurrent writes. A future minor
    /// version may extend the abstraction with an explicit <c>expectedVersion</c> argument.
    /// </remarks>
    /// <param name="processManager">The process manager to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or an adapter-defined error (conflict, persistence failure, etc.).</returns>
    Task<Result> SaveAsync(IProcessManager processManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the overall status of a process manager.
    /// </summary>
    /// <param name="id">The process manager identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or an error if the process manager was not found.</returns>
    Task<Result> UpdateStatusAsync(Guid id, SagaStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a single step within a process manager.
    /// </summary>
    /// <param name="processManagerId">The process manager identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="status">The new step status.</param>
    /// <param name="errorMessage">Optional error message, if the step failed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result, or an error if the saga or step was not found.</returns>
    Task<Result> UpdateStepStatusAsync(
        Guid processManagerId,
        Guid stepId,
        SagaStepStatus status,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}
