// -----------------------------------------------------------------------
// <copyright file="IProcessManagerStepExecutor.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Sagas.ProcessManagers;

/// <summary>
/// Defines the contract for executing and compensating individual process manager steps.
/// Implementations issue commands to aggregates / external systems and report success or
/// failure back to the orchestrator.
/// </summary>
public interface IProcessManagerStepExecutor
{
    /// <summary>
    /// Executes a single step.
    /// </summary>
    /// <param name="processManager">The owning process manager.</param>
    /// <param name="step">The step to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result if the step succeeded; a failure result triggers compensation.</returns>
    Task<Result> ExecuteAsync(IProcessManager processManager, SagaStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates (undoes) a previously executed step. Compensations should be idempotent:
    /// the orchestrator may retry on transient failures.
    /// </summary>
    /// <param name="processManager">The owning process manager.</param>
    /// <param name="step">The step to compensate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A successful result on completion; a failure result halts compensation and leaves the saga in <see cref="SagaStatus.Compensating"/>.</returns>
    Task<Result> CompensateAsync(IProcessManager processManager, SagaStep step, CancellationToken cancellationToken = default);
}
