// -----------------------------------------------------------------------
// <copyright file="PersistedProcessManager{TState}.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Application.Sagas.ProcessManagers;

namespace Compendium.Adapters.PostgreSQL.Sagas;

/// <summary>
/// Lightweight projection of an <see cref="IProcessManager{TState}"/> rehydrated from
/// PostgreSQL with its typed state snapshot. Used by callers resuming a saga from an
/// arbitrary step that need access to the full domain state (e.g. to skip external work
/// already completed in a previous attempt).
/// </summary>
/// <typeparam name="TState">The state type captured at save time.</typeparam>
internal sealed class PersistedProcessManager<TState> : IProcessManager<TState>, IMutableProcessManager
    where TState : class, new()
{
    private readonly List<SagaStep> _steps;

    public PersistedProcessManager(
        Guid id,
        SagaStatus status,
        DateTime createdAt,
        DateTime? completedAt,
        IEnumerable<SagaStep> steps,
        TState state)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(state);

        Id = id;
        Status = status;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
        State = state;
        _steps = steps.OrderBy(s => s.Order).ToList();
    }

    public Guid Id { get; }

    public SagaStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? CompletedAt { get; private set; }

    public TState State { get; }

    public IReadOnlyList<SagaStep> Steps => _steps.AsReadOnly();

    public void TransitionTo(SagaStatus status)
    {
        Status = status;
        if (status is SagaStatus.Completed or SagaStatus.Compensated)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    public void TransitionStep(Guid stepId, SagaStepStatus status, string? errorMessage = null)
    {
        var idx = _steps.FindIndex(s => s.Id == stepId);
        if (idx < 0)
        {
            return;
        }

        var step = _steps[idx];
        _steps[idx] = new SagaStep
        {
            Id = step.Id,
            Name = step.Name,
            Order = step.Order,
            Status = status,
            ErrorMessage = errorMessage,
            ExecutedAt = status == SagaStepStatus.Completed ? DateTime.UtcNow : step.ExecutedAt,
            CompensatedAt = status == SagaStepStatus.Compensated ? DateTime.UtcNow : step.CompensatedAt,
        };
    }
}
