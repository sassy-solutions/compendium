namespace Compendium.Application.Saga;

/// <summary>
/// Represents a saga (distributed transaction) that coordinates multiple operations
/// with compensation capabilities for failure handling.
/// </summary>
public interface ISaga
{
    /// <summary>
    /// Gets the unique identifier of the saga.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the current status of the saga.
    /// </summary>
    SagaStatus Status { get; }

    /// <summary>
    /// Gets the timestamp when the saga was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when the saga was completed, if applicable.
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Gets the read-only list of steps that comprise this saga.
    /// </summary>
    IReadOnlyList<SagaStep> Steps { get; }
}

/// <summary>
/// Represents a saga with associated data of type <typeparamref name="TData"/>.
/// </summary>
/// <typeparam name="TData">The type of data associated with the saga.</typeparam>
public interface ISaga<TData> : ISaga
    where TData : class
{
    /// <summary>
    /// Gets the data associated with this saga.
    /// </summary>
    TData Data { get; }
}

/// <summary>
/// Defines the possible states of a saga during its lifecycle.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// The saga has been created but not yet started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The saga is currently executing its steps.
    /// </summary>
    InProgress,

    /// <summary>
    /// The saga has successfully completed all its steps.
    /// </summary>
    Completed,

    /// <summary>
    /// The saga has failed and cannot proceed.
    /// </summary>
    Failed,

    /// <summary>
    /// The saga is currently executing compensation actions due to a failure.
    /// </summary>
    Compensating,

    /// <summary>
    /// The saga has successfully completed all compensation actions.
    /// </summary>
    Compensated
}

/// <summary>
/// Represents a single step within a saga, including its execution state and metadata.
/// </summary>
public sealed class SagaStep
{
    /// <summary>
    /// Gets or initializes the unique identifier of the saga step.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the name of the saga step.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the current status of the saga step.
    /// </summary>
    public SagaStepStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the step was executed.
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
    /// Gets or initializes the execution order of this step within the saga.
    /// </summary>
    public int Order { get; init; }
}

/// <summary>
/// Defines the possible states of a saga step during its lifecycle.
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
    /// The step is currently being compensated due to a failure.
    /// </summary>
    Compensating,

    /// <summary>
    /// The step has been successfully compensated.
    /// </summary>
    Compensated
}
