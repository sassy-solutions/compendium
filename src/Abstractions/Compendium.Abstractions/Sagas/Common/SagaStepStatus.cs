// -----------------------------------------------------------------------
// <copyright file="SagaStepStatus.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Common;

/// <summary>
/// Defines the possible states of an orchestration saga step during its lifecycle.
/// Used exclusively by <see cref="ProcessManagers.IProcessManager"/>; choreography sagas
/// have no concept of named "steps".
/// </summary>
public enum SagaStepStatus
{
    /// <summary>
    /// The step is waiting to be executed.
    /// </summary>
    Pending,

    /// <summary>
    /// The step is currently being executed.
    /// </summary>
    Executing,

    /// <summary>
    /// The step has been successfully executed.
    /// </summary>
    Completed,

    /// <summary>
    /// The step has failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// The step is currently being compensated due to a downstream failure.
    /// </summary>
    Compensating,

    /// <summary>
    /// The step has been successfully compensated (rolled back).
    /// </summary>
    Compensated,
}
