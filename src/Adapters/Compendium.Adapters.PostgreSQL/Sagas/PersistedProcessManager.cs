// -----------------------------------------------------------------------
// <copyright file="PersistedProcessManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Application.Sagas.ProcessManagers;

namespace Compendium.Adapters.PostgreSQL.Sagas;

/// <summary>
/// Lightweight projection of an <see cref="IProcessManager"/> rehydrated from PostgreSQL.
/// Exposes only the lifecycle metadata needed by <see cref="IProcessManagerOrchestrator"/>;
/// concrete state must be reloaded by the user with the saga's typed deserializer.
/// </summary>
internal sealed class PersistedProcessManager : IProcessManager, IMutableProcessManager
{
    private readonly List<SagaStep> _steps;

    public PersistedProcessManager(
        Guid id,
        SagaStatus status,
        DateTime createdAt,
        DateTime? completedAt,
        IEnumerable<SagaStep> steps)
    {
        Id = id;
        Status = status;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
        _steps = steps.OrderBy(s => s.Order).ToList();
    }

    public Guid Id { get; }

    public SagaStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? CompletedAt { get; private set; }

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
