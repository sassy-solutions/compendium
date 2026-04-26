// -----------------------------------------------------------------------
// <copyright file="SagaStep.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Common;

/// <summary>
/// Represents a single step within an orchestration saga, including its execution state
/// and metadata. Steps are owned by an <see cref="ProcessManagers.IProcessManager"/> and
/// executed in declared order; compensation runs in reverse order.
/// </summary>
public sealed class SagaStep
{
    /// <summary>
    /// Gets or initializes the unique identifier of the saga step.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the name of the saga step. Used to look up the step
    /// from the parent saga and for logging/telemetry.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the current status of the saga step.
    /// </summary>
    public SagaStepStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the step finished executing successfully.
    /// </summary>
    public DateTime? ExecutedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the step was compensated.
    /// </summary>
    public DateTime? CompensatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the error message if the step failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or initializes the execution order of this step within the saga (1-based).
    /// </summary>
    public int Order { get; init; }
}
