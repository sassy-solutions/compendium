// -----------------------------------------------------------------------
// <copyright file="SagaStatus.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Common;

/// <summary>
/// Defines the possible states of a saga during its lifecycle.
/// Shared by both <see cref="ProcessManagers.IProcessManager"/> (orchestration) and
/// <see cref="Choreography.IEventChoreography"/> (choreography) flavors.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// The saga has been created but not yet started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The saga is currently executing.
    /// </summary>
    InProgress,

    /// <summary>
    /// The saga has successfully completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The saga has failed and cannot proceed without compensation.
    /// </summary>
    Failed,

    /// <summary>
    /// The saga is currently executing compensation actions due to a failure.
    /// </summary>
    Compensating,

    /// <summary>
    /// The saga has successfully completed all compensation actions.
    /// </summary>
    Compensated,
}
