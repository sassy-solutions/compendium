// -----------------------------------------------------------------------
// <copyright file="IMutableProcessManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Application.Sagas.ProcessManagers;

/// <summary>
/// Internal seam used by repositories to push status updates onto a process manager
/// instance held in memory. Implemented by <see cref="ProcessManager{TState}"/>; not
/// intended for user code.
/// </summary>
public interface IMutableProcessManager
{
    /// <summary>
    /// Transitions the saga's overall status.
    /// </summary>
    /// <param name="status">The new status.</param>
    void TransitionTo(SagaStatus status);

    /// <summary>
    /// Transitions a single step's status.
    /// </summary>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="status">The new step status.</param>
    /// <param name="errorMessage">Optional error message if the step failed.</param>
    void TransitionStep(Guid stepId, SagaStepStatus status, string? errorMessage = null);
}
