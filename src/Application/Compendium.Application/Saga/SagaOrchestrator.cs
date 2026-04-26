namespace Compendium.Application.Saga;

/// <summary>
/// Orchestrates the execution of sagas, managing their lifecycle, step execution, and compensation.
/// </summary>
[Obsolete("Use Compendium.Abstractions.Sagas.ProcessManagers.IProcessManagerOrchestrator instead. " +
    "Will be removed in v1.0.")]
public interface ISagaOrchestrator
{
    /// <summary>
    /// Starts a new saga instance with the provided data.
    /// </summary>
    /// <typeparam name="TSaga">The type of the saga to start.</typeparam>
    /// <typeparam name="TData">The type of data associated with the saga.</typeparam>
    /// <param name="data">The data to initialize the saga with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the saga ID.</returns>
    Task<Result<Guid>> StartSagaAsync<TSaga, TData>(TData data, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga<TData>
        where TData : class;

    /// <summary>
    /// Executes a specific step of a saga.
    /// </summary>
    /// <param name="sagaId">The ID of the saga.</param>
    /// <param name="stepName">The name of the step to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> ExecuteStepAsync(Guid sagaId, string stepName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates compensation for a failed saga, rolling back completed steps in reverse order.
    /// </summary>
    /// <param name="sagaId">The ID of the saga to compensate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> CompensateAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a saga by its ID.
    /// </summary>
    /// <param name="sagaId">The ID of the saga to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the saga.</returns>
    Task<Result<ISaga>> GetSagaAsync(Guid sagaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of the saga orchestrator that manages saga lifecycle and coordination.
/// </summary>
[Obsolete("Use Compendium.Application.Sagas.ProcessManagers.ProcessManagerOrchestrator instead. " +
    "Will be removed in v1.0.")]
public sealed class SagaOrchestrator : ISagaOrchestrator
{
    private readonly ISagaRepository _repository;
    private readonly ISagaStepExecutor _stepExecutor;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaOrchestrator"/> class.
    /// </summary>
    /// <param name="repository">The repository for saga persistence.</param>
    /// <param name="stepExecutor">The executor for saga steps.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SagaOrchestrator(
        ISagaRepository repository,
        ISagaStepExecutor stepExecutor,
        IServiceProvider serviceProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Starts a new saga instance with the provided data.
    /// </summary>
    /// <typeparam name="TSaga">The type of the saga to start.</typeparam>
    /// <typeparam name="TData">The type of data associated with the saga.</typeparam>
    /// <param name="data">The data to initialize the saga with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the saga ID.</returns>
    public async Task<Result<Guid>> StartSagaAsync<TSaga, TData>(TData data, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga<TData>
        where TData : class
    {
        try
        {
            var sagaFactoryType = typeof(ISagaFactory<,>).MakeGenericType(typeof(TSaga), typeof(TData));
            var sagaFactory = _serviceProvider.GetService(sagaFactoryType);
            if (sagaFactory is null)
            {
                return Result.Failure<Guid>(Error.NotFound("SagaFactory.NotFound", $"No factory found for saga {typeof(TSaga).Name}"));
            }

            var createMethod = sagaFactoryType.GetMethod("Create");
            if (createMethod is null)
            {
                return Result.Failure<Guid>(Error.Failure("SagaFactory.InvalidFactory", "Factory does not have Create method"));
            }

            var saga = (ISaga)createMethod.Invoke(sagaFactory, new object[] { data })!;
            await _repository.SaveAsync(saga, cancellationToken);

            return Result.Success(saga.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(Error.Failure("Saga.StartFailed", ex.Message));
        }
    }

    /// <summary>
    /// Executes a specific step of a saga.
    /// </summary>
    /// <param name="sagaId">The ID of the saga.</param>
    /// <param name="stepName">The name of the step to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> ExecuteStepAsync(Guid sagaId, string stepName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sagaResult = await _repository.GetByIdAsync(sagaId, cancellationToken);
            if (sagaResult.IsFailure)
            {
                return sagaResult;
            }

            var saga = sagaResult.Value;
            var step = saga.Steps.FirstOrDefault(s => s.Name == stepName);
            if (step is null)
            {
                return Result.Failure(Error.NotFound("Step.NotFound", $"Step '{stepName}' not found in saga"));
            }

            if (step.Status != SagaStepStatus.Pending)
            {
                return Result.Failure(Error.Conflict("Step.InvalidStatus", $"Step '{stepName}' is not in pending status"));
            }

            var executionResult = await _stepExecutor.ExecuteAsync(saga, step, cancellationToken);
            if (executionResult.IsFailure)
            {
                // Mark step as failed and potentially start compensation
                await _repository.UpdateStepStatusAsync(sagaId, step.Id, SagaStepStatus.Failed, executionResult.Error.Message, cancellationToken);
                return executionResult;
            }

            await _repository.UpdateStepStatusAsync(sagaId, step.Id, SagaStepStatus.Completed, null, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Saga.ExecutionFailed", ex.Message));
        }
    }

    /// <summary>
    /// Initiates compensation for a failed saga, rolling back completed steps in reverse order.
    /// </summary>
    /// <param name="sagaId">The ID of the saga to compensate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> CompensateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sagaResult = await _repository.GetByIdAsync(sagaId, cancellationToken);
            if (sagaResult.IsFailure)
            {
                return sagaResult;
            }

            var saga = sagaResult.Value;
            var completedSteps = saga.Steps
                .Where(s => s.Status == SagaStepStatus.Completed)
                .OrderByDescending(s => s.Order)
                .ToList();

            await _repository.UpdateSagaStatusAsync(sagaId, SagaStatus.Compensating, cancellationToken);

            foreach (var step in completedSteps)
            {
                var compensationResult = await _stepExecutor.CompensateAsync(saga, step, cancellationToken);
                if (compensationResult.IsFailure)
                {
                    await _repository.UpdateStepStatusAsync(sagaId, step.Id, SagaStepStatus.Failed, compensationResult.Error.Message, cancellationToken);
                    return compensationResult;
                }

                await _repository.UpdateStepStatusAsync(sagaId, step.Id, SagaStepStatus.Compensated, null, cancellationToken);
            }

            await _repository.UpdateSagaStatusAsync(sagaId, SagaStatus.Compensated, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Saga.CompensationFailed", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a saga by its ID.
    /// </summary>
    /// <param name="sagaId">The ID of the saga to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the saga.</returns>
    public async Task<Result<ISaga>> GetSagaAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(sagaId, cancellationToken);
    }
}

/// <summary>
/// Defines the contract for saga persistence operations.
/// </summary>
[Obsolete("Use Compendium.Abstractions.Sagas.ProcessManagers.IProcessManagerRepository instead. " +
    "Will be removed in v1.0.")]
public interface ISagaRepository
{
    /// <summary>
    /// Retrieves a saga by its unique identifier.
    /// </summary>
    /// <param name="sagaId">The unique identifier of the saga.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the saga.</returns>
    Task<Result<ISaga>> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a saga to the repository.
    /// </summary>
    /// <param name="saga">The saga to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(ISaga saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a saga.
    /// </summary>
    /// <param name="sagaId">The unique identifier of the saga.</param>
    /// <param name="status">The new status to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateSagaStatusAsync(Guid sagaId, SagaStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a specific saga step.
    /// </summary>
    /// <param name="sagaId">The unique identifier of the saga.</param>
    /// <param name="stepId">The unique identifier of the step.</param>
    /// <param name="status">The new status to set.</param>
    /// <param name="errorMessage">Optional error message if the step failed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateStepStatusAsync(Guid sagaId, Guid stepId, SagaStepStatus status, string? errorMessage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for executing and compensating saga steps.
/// </summary>
[Obsolete("Use Compendium.Abstractions.Sagas.ProcessManagers.IProcessManagerStepExecutor instead. " +
    "Will be removed in v1.0.")]
public interface ISagaStepExecutor
{
    /// <summary>
    /// Executes a saga step.
    /// </summary>
    /// <param name="saga">The saga containing the step.</param>
    /// <param name="step">The step to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> ExecuteAsync(ISaga saga, SagaStep step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates (rolls back) a previously executed saga step.
    /// </summary>
    /// <param name="saga">The saga containing the step.</param>
    /// <param name="step">The step to compensate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> CompensateAsync(ISaga saga, SagaStep step, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a factory for creating saga instances with associated data.
/// </summary>
/// <typeparam name="TSaga">The type of saga to create.</typeparam>
/// <typeparam name="TData">The type of data associated with the saga.</typeparam>
[Obsolete("Process Managers are now constructed directly via static factory methods on the saga class " +
    "(see Compendium.Application.Sagas.ProcessManagers.ProcessManager{TState}). Will be removed in v1.0.")]
public interface ISagaFactory<TSaga, TData>
    where TSaga : class, ISaga<TData>
    where TData : class
{
    /// <summary>
    /// Creates a new saga instance with the provided data.
    /// </summary>
    /// <param name="data">The data to initialize the saga with.</param>
    /// <returns>A new saga instance.</returns>
    TSaga Create(TData data);
}
