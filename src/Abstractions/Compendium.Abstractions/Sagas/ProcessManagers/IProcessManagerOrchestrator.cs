// -----------------------------------------------------------------------
// <copyright file="IProcessManagerOrchestrator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.ProcessManagers;

/// <summary>
/// Drives the lifecycle of process managers: starts new instances, dispatches steps,
/// and triggers compensation on failure.
/// </summary>
public interface IProcessManagerOrchestrator
{
    /// <summary>
    /// Persists a freshly-constructed process manager and returns its identifier.
    /// The caller is responsible for instantiating the process manager (typically via a
    /// factory or static <c>Create</c> method) and supplying its initial state.
    /// </summary>
    /// <param name="processManager">The process manager to start.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifier of the started process manager.</returns>
    Task<Result<Guid>> StartAsync(IProcessManager processManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a single named step of a process manager. On failure, the step is marked
    /// <c>Failed</c>; the caller is expected to invoke <see cref="CompensateAsync"/> to
    /// roll back. The orchestrator does <i>not</i> auto-compensate, so callers can choose
    /// to retry transient failures before triggering compensation.
    /// </summary>
    /// <param name="processManagerId">The process manager identifier.</param>
    /// <param name="stepName">The name of the step to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result if the step succeeded; otherwise the underlying step error.</returns>
    Task<Result> ExecuteStepAsync(Guid processManagerId, string stepName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs compensation for a process manager: each completed step is undone in reverse
    /// order via <see cref="IProcessManagerStepExecutor.CompensateAsync"/>. On full success
    /// the saga reaches <c>Compensated</c>; on partial failure it is left in <c>Compensating</c>.
    /// </summary>
    /// <param name="processManagerId">The process manager identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result if all completed steps were compensated.</returns>
    Task<Result> CompensateAsync(Guid processManagerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a process manager by id.
    /// </summary>
    /// <param name="processManagerId">The process manager identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process manager, or a not-found error.</returns>
    Task<Result<IProcessManager>> GetAsync(Guid processManagerId, CancellationToken cancellationToken = default);
}
