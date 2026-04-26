// -----------------------------------------------------------------------
// <copyright file="ProcessManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;

namespace Compendium.Application.Sagas.ProcessManagers;

/// <summary>
/// Base class for DDD-style orchestration sagas (Process Managers). Subclass this to
/// declare a saga's steps and state shape; the <see cref="IProcessManagerOrchestrator"/>
/// drives execution and compensation.
/// </summary>
/// <typeparam name="TState">Shape of the saga's persisted state.</typeparam>
/// <example>
/// <code>
/// public sealed class OrderProcessManager : ProcessManager&lt;OrderProcessState&gt;
/// {
///     private OrderProcessManager(Guid id, OrderProcessState state)
///         : base(id, state, new[] { "ReserveInventory", "ProcessPayment", "ShipOrder" })
///     {
///     }
///
///     public static OrderProcessManager Create(string orderId, decimal amount) =&gt;
///         new(Guid.NewGuid(), new OrderProcessState { OrderId = orderId, Amount = amount });
/// }
/// </code>
/// </example>
public abstract class ProcessManager<TState> : IProcessManager<TState>, IMutableProcessManager
    where TState : class, new()
{
    private readonly List<SagaStep> _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessManager{TState}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the saga instance.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="stepNames">The ordered list of step names that comprise this saga.</param>
    protected ProcessManager(Guid id, TState state, IEnumerable<string> stepNames)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stepNames);

        Id = id;
        State = state;
        Status = SagaStatus.NotStarted;
        CreatedAt = DateTime.UtcNow;

        _steps = stepNames
            .Select((name, idx) => new SagaStep
            {
                Id = Guid.NewGuid(),
                Name = name,
                Status = SagaStepStatus.Pending,
                Order = idx + 1,
            })
            .ToList();

        if (_steps.Count == 0)
        {
            throw new ArgumentException("A process manager must declare at least one step.", nameof(stepNames));
        }
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public TState State { get; protected set; }

    /// <inheritdoc />
    public SagaStatus Status { get; private set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <inheritdoc />
    public DateTime? CompletedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<SagaStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Updates the overall status. Called by the orchestrator; should not be invoked
    /// directly by user code.
    /// </summary>
    /// <param name="status">The new status.</param>
    public void TransitionTo(SagaStatus status)
    {
        Status = status;
        if (status is SagaStatus.Completed or SagaStatus.Compensated)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates a step's status. Called by the orchestrator.
    /// </summary>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="status">The new step status.</param>
    /// <param name="errorMessage">Optional error message.</param>
    public void TransitionStep(Guid stepId, SagaStepStatus status, string? errorMessage = null)
    {
        var index = _steps.FindIndex(s => s.Id == stepId);
        if (index < 0)
        {
            return;
        }

        var step = _steps[index];
        _steps[index] = new SagaStep
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
