// -----------------------------------------------------------------------
// <copyright file="ProcessManagerOrchestrator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;

namespace Compendium.Application.Sagas.ProcessManagers;

/// <summary>
/// Default <see cref="IProcessManagerOrchestrator"/> implementation. Persists the saga
/// via <see cref="IProcessManagerRepository"/> and delegates step execution / compensation
/// to <see cref="IProcessManagerStepExecutor"/>.
/// </summary>
public sealed class ProcessManagerOrchestrator : IProcessManagerOrchestrator
{
    private readonly IProcessManagerRepository _repository;
    private readonly IProcessManagerStepExecutor _stepExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessManagerOrchestrator"/> class.
    /// </summary>
    /// <param name="repository">Persistence port.</param>
    /// <param name="stepExecutor">Step execution port.</param>
    public ProcessManagerOrchestrator(
        IProcessManagerRepository repository,
        IProcessManagerStepExecutor stepExecutor)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> StartAsync(IProcessManager processManager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processManager);

        var saveResult = await _repository.SaveAsync(processManager, cancellationToken).ConfigureAwait(false);
        return saveResult.IsSuccess
            ? Result.Success(processManager.Id)
            : Result.Failure<Guid>(saveResult.Error);
    }

    /// <inheritdoc />
    public async Task<Result> ExecuteStepAsync(Guid processManagerId, string stepName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            return Result.Failure(Error.Validation("ProcessManager.StepNameMissing", "Step name must be provided."));
        }

        var lookup = await _repository.GetByIdAsync(processManagerId, cancellationToken).ConfigureAwait(false);
        if (lookup.IsFailure)
        {
            return Result.Failure(lookup.Error);
        }

        var processManager = lookup.Value;
        var step = processManager.Steps.FirstOrDefault(s => s.Name == stepName);
        if (step is null)
        {
            return Result.Failure(Error.NotFound(
                "ProcessManager.StepNotFound",
                $"Step '{stepName}' not found in process manager {processManagerId}."));
        }

        if (step.Status != SagaStepStatus.Pending)
        {
            return Result.Failure(Error.Conflict(
                "ProcessManager.StepInvalidStatus",
                $"Step '{stepName}' is in status '{step.Status}'; expected 'Pending'."));
        }

        // Mark in-progress before delegating, so a crash mid-execution is observable.
        var markInProgress = await _repository.UpdateStatusAsync(processManagerId, SagaStatus.InProgress, cancellationToken).ConfigureAwait(false);
        if (markInProgress.IsFailure)
        {
            return markInProgress;
        }

        var execution = await _stepExecutor.ExecuteAsync(processManager, step, cancellationToken).ConfigureAwait(false);
        if (execution.IsFailure)
        {
            // Persist the failure status; if persistence itself fails we still want the
            // execution error to surface (it's the original cause), but log via the result type.
            var markFailed = await _repository.UpdateStepStatusAsync(
                processManagerId,
                step.Id,
                SagaStepStatus.Failed,
                execution.Error.Message,
                cancellationToken).ConfigureAwait(false);
            return markFailed.IsFailure ? markFailed : execution;
        }

        var markCompleted = await _repository.UpdateStepStatusAsync(
            processManagerId,
            step.Id,
            SagaStepStatus.Completed,
            errorMessage: null,
            cancellationToken).ConfigureAwait(false);
        if (markCompleted.IsFailure)
        {
            return markCompleted;
        }

        // Auto-complete if every step is done. Persistence failures here surface as errors.
        var refreshed = await _repository.GetByIdAsync(processManagerId, cancellationToken).ConfigureAwait(false);
        if (refreshed.IsFailure)
        {
            return Result.Failure(refreshed.Error);
        }

        if (refreshed.Value.Steps.All(s => s.Status == SagaStepStatus.Completed))
        {
            var markFinished = await _repository.UpdateStatusAsync(processManagerId, SagaStatus.Completed, cancellationToken).ConfigureAwait(false);
            if (markFinished.IsFailure)
            {
                return markFinished;
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CompensateAsync(Guid processManagerId, CancellationToken cancellationToken = default)
    {
        var lookup = await _repository.GetByIdAsync(processManagerId, cancellationToken).ConfigureAwait(false);
        if (lookup.IsFailure)
        {
            return Result.Failure(lookup.Error);
        }

        var processManager = lookup.Value;
        var completedSteps = processManager.Steps
            .Where(s => s.Status == SagaStepStatus.Completed)
            .OrderByDescending(s => s.Order)
            .ToList();

        var markCompensating = await _repository.UpdateStatusAsync(processManagerId, SagaStatus.Compensating, cancellationToken).ConfigureAwait(false);
        if (markCompensating.IsFailure)
        {
            return markCompensating;
        }

        foreach (var step in completedSteps)
        {
            var compensation = await _stepExecutor.CompensateAsync(processManager, step, cancellationToken).ConfigureAwait(false);
            if (compensation.IsFailure)
            {
                var markStepFailed = await _repository.UpdateStepStatusAsync(
                    processManagerId,
                    step.Id,
                    SagaStepStatus.Failed,
                    compensation.Error.Message,
                    cancellationToken).ConfigureAwait(false);
                return markStepFailed.IsFailure ? markStepFailed : compensation;
            }

            var markStepCompensated = await _repository.UpdateStepStatusAsync(
                processManagerId,
                step.Id,
                SagaStepStatus.Compensated,
                errorMessage: null,
                cancellationToken).ConfigureAwait(false);
            if (markStepCompensated.IsFailure)
            {
                return markStepCompensated;
            }
        }

        return await _repository.UpdateStatusAsync(processManagerId, SagaStatus.Compensated, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<IProcessManager>> GetAsync(Guid processManagerId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(processManagerId, cancellationToken);
}
