// -----------------------------------------------------------------------
// <copyright file="ProjectionManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Compendium.Core.Telemetry;

namespace Compendium.Infrastructure.EventSourcing;

/// <summary>
/// Interface for managing projections and read models.
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Registers a projection with the manager.
    /// </summary>
    /// <param name="projection">The projection to register.</param>
    void RegisterProjection(IProjection projection);

    /// <summary>
    /// Processes an event through all applicable projections.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="domainEvent">The domain event to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> ProcessEventAsync(
        string aggregateId,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds a specific projection by replaying all events.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="aggregateId">The aggregate identifier to rebuild for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> RebuildProjectionAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a projection.
    /// </summary>
    /// <typeparam name="T">The type of the projection state.</typeparam>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projection state.</returns>
    Task<Result<T>> GetProjectionStateAsync<T>(
        string projectionId,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets statistics about all registered projections.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Projection statistics.</returns>
    Task<Result<ProjectionManagerStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages projections and read models for event sourcing.
/// Provides thread-safe projection processing and rebuilding capabilities.
/// </summary>
public sealed class ProjectionManager : IProjectionManager, IDisposable
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<ProjectionManager>? _logger;
    private readonly IProjectionCheckpointStore? _checkpointStore;
    private readonly ConcurrentDictionary<string, IProjection> _projections = new();
    private readonly ConcurrentDictionary<string, ProjectionState> _states = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _projectionSemaphores = new();
    private readonly int _checkpointInterval;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionManager"/> class.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="checkpointStore">Optional checkpoint store for persistence.</param>
    /// <param name="checkpointInterval">Number of events between checkpoints (default: 1000).</param>
    public ProjectionManager(
        IEventStore eventStore,
        ILogger<ProjectionManager> logger,
        IProjectionCheckpointStore? checkpointStore = null,
        int checkpointInterval = 1000)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkpointStore = checkpointStore;
        _checkpointInterval = checkpointInterval > 0 ? checkpointInterval : 1000;
    }

    /// <inheritdoc />
    public void RegisterProjection(IProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        // Allow replacing existing projections
        _projections.AddOrUpdate(projection.ProjectionId, projection, (key, oldValue) => projection);

        // Create dedicated semaphore for this projection
        _projectionSemaphores.TryAdd(projection.ProjectionId, new SemaphoreSlim(1, 1));

        _states[projection.ProjectionId] = new ProjectionState(
            projection.ProjectionId,
            0,
            DateTimeOffset.UtcNow,
            null,
            false
        );

        _logger?.LogInformation("Registered projection {ProjectionId}", projection.ProjectionId);
    }

    /// <inheritdoc />
    public async Task<Result> ProcessEventAsync(
        string aggregateId,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        // COMP-019: Start distributed trace for event processing
        using var activity = CompendiumTelemetry.ActivitySource.StartActivity(
            CompendiumTelemetry.ProjectionActivities.ProcessEvent);

        activity?.SetTag(CompendiumTelemetry.Tags.AggregateId, aggregateId);
        activity?.SetTag(CompendiumTelemetry.Tags.EventType, domainEvent?.GetType().Name ?? "null");

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Result.Failure(Error.Validation("ProjectionManager.InvalidAggregateId", "AggregateId cannot be null or empty"));
        }

        if (domainEvent == null)
        {
            return Result.Failure(Error.Validation("ProjectionManager.InvalidEvent", "DomainEvent cannot be null"));
        }

        ThrowIfDisposed();

        var sw = Stopwatch.StartNew();
        var tasks = new List<Task<Result>>();

        foreach (var projection in _projections.Values)
        {
            tasks.Add(ProcessEventForProjectionAsync(projection, domainEvent, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var failures = results.Where(r => !r.IsSuccess).ToList();
        if (failures.Any())
        {
            // COMP-019: Record failure metrics
            foreach (var projection in _projections.Values)
            {
                CompendiumTelemetry.ProjectionEventsProcessed.Add(1,
                    new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.ProjectionId, projection.ProjectionId),
                    new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, CompendiumTelemetry.StatusValues.Failure));
            }

            activity?.SetStatus(ActivityStatusCode.Error);
            var errorMessages = failures.Select(r => r.Error.Message);
            return Result.Failure(Error.Failure("Projection.Failed",
                $"Failed to process event: {string.Join(", ", errorMessages)}"));
        }

        // COMP-019: Record success metrics for each projection
        foreach (var projection in _projections.Values)
        {
            CompendiumTelemetry.ProjectionEventsProcessed.Add(1,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.ProjectionId, projection.ProjectionId),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.AggregateType, aggregateId.Split('-').FirstOrDefault() ?? "unknown"),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, CompendiumTelemetry.StatusValues.Success));
        }

        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result> RebuildProjectionAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        return RebuildProjectionAsync(projectionId, aggregateId, null, cancellationToken);
    }

    /// <summary>
    /// Rebuilds a specific projection by replaying all events with progress reporting.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="aggregateId">The aggregate identifier to rebuild for.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<Result> RebuildProjectionAsync(
        string projectionId,
        string aggregateId,
        IProgress<ProjectionRebuildProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        // COMP-019: Start distributed trace for projection rebuild
        using var activity = CompendiumTelemetry.ActivitySource.StartActivity(
            CompendiumTelemetry.ProjectionActivities.RebuildProjection);

        activity?.SetTag(CompendiumTelemetry.Tags.ProjectionId, projectionId);
        activity?.SetTag(CompendiumTelemetry.Tags.AggregateId, aggregateId);

        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Result.Failure(Error.Validation("ProjectionManager.InvalidProjectionId", "ProjectionId cannot be null or empty"));
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            return Result.Failure(Error.Validation("ProjectionManager.InvalidAggregateId", "AggregateId cannot be null or empty"));
        }

        ThrowIfDisposed();

        if (!_projections.TryGetValue(projectionId, out var projection))
        {
            return Result.Failure(Error.NotFound("Projection.NotFound", $"Projection {projectionId} not found"));
        }

        if (!_projectionSemaphores.TryGetValue(projectionId, out var semaphore))
        {
            return Result.Failure(Error.Failure("Projection.SemaphoreNotFound", $"Semaphore for projection {projectionId} not found"));
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            _logger?.LogInformation("Starting rebuild of projection {ProjectionId} for aggregate {AggregateId}",
                projectionId, aggregateId);

            // Mark as rebuilding
            var state = _states[projectionId];
            _states[projectionId] = state with { IsRebuilding = true };

            // Try to restore from checkpoint
            var lastCheckpointPosition = 0L;
            if (_checkpointStore != null)
            {
                var checkpointResult = await _checkpointStore.GetCheckpointAsync(projectionId, aggregateId, cancellationToken);
                if (checkpointResult.IsSuccess)
                {
                    lastCheckpointPosition = checkpointResult.Value;
                    _logger?.LogInformation("Resuming projection {ProjectionId} from checkpoint at position {Position}",
                        projectionId, lastCheckpointPosition);
                }
            }

            // Reset projection state if starting from beginning
            if (lastCheckpointPosition == 0)
            {
                await projection.ResetAsync(cancellationToken);
            }

            // Get all events for the aggregate (or from checkpoint position)
            var eventsResult = await _eventStore.GetEventsAsync(aggregateId, lastCheckpointPosition, cancellationToken);
            if (!eventsResult.IsSuccess)
            {
                return Result.Failure(eventsResult.Error);
            }

            var totalEvents = eventsResult.Value.Count;
            var processedEvents = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Report initial progress
            progress?.Report(new ProjectionRebuildProgress(projectionId, 0, totalEvents, 0, false));

            // Replay events with checkpointing
            foreach (var @event in eventsResult.Value)
            {
                await projection.ApplyAsync(@event, cancellationToken);
                processedEvents++;

                // Checkpoint every N events
                if (_checkpointStore != null && processedEvents % _checkpointInterval == 0)
                {
                    var checkpointPosition = lastCheckpointPosition + processedEvents;
                    await _checkpointStore.SaveCheckpointAsync(projectionId, aggregateId, checkpointPosition, cancellationToken);

                    _logger?.LogDebug("Checkpoint saved for projection {ProjectionId} at position {Position}",
                        projectionId, checkpointPosition);
                }

                // Report progress every 100 events or at completion
                if (processedEvents % 100 == 0 || processedEvents == totalEvents)
                {
                    var percentComplete = (double)processedEvents / totalEvents * 100;
                    var eventsPerSecond = processedEvents / sw.Elapsed.TotalSeconds;

                    progress?.Report(new ProjectionRebuildProgress(
                        projectionId,
                        processedEvents,
                        totalEvents,
                        percentComplete,
                        false,
                        eventsPerSecond));
                }
            }

            sw.Stop();

            // Save final checkpoint
            if (_checkpointStore != null && processedEvents > 0)
            {
                var finalPosition = lastCheckpointPosition + processedEvents;
                await _checkpointStore.SaveCheckpointAsync(projectionId, aggregateId, finalPosition, cancellationToken);
            }

            // Update state
            _states[projectionId] = new ProjectionState(
                projectionId,
                projection.Version,
                DateTimeOffset.UtcNow,
                await projection.GetStateAsync(cancellationToken),
                false
            );

            // Report completion
            var finalEventsPerSec = totalEvents / sw.Elapsed.TotalSeconds;
            progress?.Report(new ProjectionRebuildProgress(projectionId, totalEvents, totalEvents, 100, true, finalEventsPerSec));

            _logger?.LogInformation("Completed rebuild of projection {ProjectionId} - Processed {EventCount} events in {Duration}ms ({EventsPerSec:F2} events/sec)",
                projectionId, totalEvents, sw.ElapsedMilliseconds, finalEventsPerSec);

            // COMP-019: Record rebuild metrics
            CompendiumTelemetry.ProjectionRebuildsCompleted.Add(1,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.ProjectionId, projectionId),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, CompendiumTelemetry.StatusValues.Success));

            CompendiumTelemetry.ProjectionRebuildDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.ProjectionId, projectionId));

            activity?.SetTag("events_processed", totalEvents);
            activity?.SetTag("events_per_second", finalEventsPerSec);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to rebuild projection {ProjectionId}", projectionId);

            // COMP-019: Record failure metrics
            CompendiumTelemetry.ProjectionRebuildsCompleted.Add(1,
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.ProjectionId, projectionId),
                new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.Status, CompendiumTelemetry.StatusValues.Failure));

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure(Error.Failure("Projection.RebuildFailed", ex.Message));
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public Task<Result<T>> GetProjectionStateAsync<T>(
        string projectionId,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(projectionId))
        {
            return Task.FromResult(Result.Failure<T>(Error.Validation("ProjectionManager.InvalidProjectionId", "ProjectionId cannot be null or empty")));
        }

        ThrowIfDisposed();

        if (!_states.TryGetValue(projectionId, out var state))
        {
            return Task.FromResult(Result.Failure<T>(Error.NotFound("Projection.NotFound", $"Projection {projectionId} not found")));
        }

        if (state.IsRebuilding)
        {
            return Task.FromResult(Result.Failure<T>(Error.Failure("ProjectionManager.ProjectionRebuilding", $"Projection {projectionId} is currently rebuilding")));
        }

        if (state.State is T typedState)
        {
            return Task.FromResult(Result.Success<T>(typedState));
        }

        return Task.FromResult(Result.Failure<T>(Error.Failure("ProjectionManager.InvalidStateType", $"Projection state is not of type {typeof(T).Name}")));
    }

    /// <inheritdoc />
    public Task<Result<ProjectionManagerStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var stats = new ProjectionManagerStatistics
            {
                TotalProjections = _projections.Count,
                ActiveProjections = _states.Values.Count(s => !s.IsRebuilding),
                RebuildingProjections = _states.Values.Count(s => s.IsRebuilding),
                ProjectionDetails = _states.Values.ToDictionary(
                    s => s.ProjectionId,
                    s => new ProjectionDetails
                    {
                        ProjectionId = s.ProjectionId,
                        LastProcessedVersion = s.LastProcessedVersion,
                        LastUpdated = s.LastUpdated,
                        IsRebuilding = s.IsRebuilding
                    })
            };

            return Task.FromResult(Result.Success(stats));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get projection manager statistics");
            return Task.FromResult(Result.Failure<ProjectionManagerStatistics>(
                Error.Failure("ProjectionManager.GetStatisticsFailed", ex.Message)));
        }
    }

    /// <summary>
    /// Processes an event for a specific projection.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task<Result> ProcessEventForProjectionAsync(
        IProjection projection,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            await projection.ApplyAsync(domainEvent, cancellationToken);

            var state = _states[projection.ProjectionId];
            _states[projection.ProjectionId] = state with
            {
                LastProcessedVersion = projection.Version,
                LastUpdated = DateTimeOffset.UtcNow,
                State = await projection.GetStateAsync(cancellationToken)
            };

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to process event for projection {ProjectionId}", projection.ProjectionId);
            return Result.Failure(Error.Failure("Projection.Failed", $"Projection {projection.ProjectionId} failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Throws an exception if the instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ProjectionManager));
        }
    }

    /// <summary>
    /// Disposes the resources used by the projection manager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var semaphore in _projectionSemaphores.Values)
            {
                semaphore.Dispose();
            }
            _projectionSemaphores.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Represents the state of a projection.
    /// </summary>
    /// <param name="ProjectionId">The projection identifier.</param>
    /// <param name="LastProcessedVersion">The last processed version.</param>
    /// <param name="LastUpdated">The last updated timestamp.</param>
    /// <param name="State">The current state.</param>
    /// <param name="IsRebuilding">Whether the projection is rebuilding.</param>
    private sealed record ProjectionState(
        string ProjectionId,
        int LastProcessedVersion,
        DateTimeOffset LastUpdated,
        object? State,
        bool IsRebuilding
    );
}

/// <summary>
/// Interface for projections that handle domain events.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Gets the unique identifier of the projection.
    /// </summary>
    string ProjectionId { get; }

    /// <summary>
    /// Gets the current version of the projection.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Applies a domain event to the projection.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ApplyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the projection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current state.</returns>
    Task<object> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the projection to its initial state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for projections that handle specific event types.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle.</typeparam>
/// <typeparam name="TState">The type of the projection state.</typeparam>
public abstract class ProjectionBase<TEvent, TState> : IProjection
    where TEvent : class, IDomainEvent
    where TState : class, new()
{
    private TState _state = new();
    private int _version;

    /// <inheritdoc />
    public abstract string ProjectionId { get; }

    /// <inheritdoc />
    public int Version => _version;

    /// <inheritdoc />
    public async Task ApplyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is TEvent typedEvent)
        {
            await HandleEventAsync(typedEvent, cancellationToken);
            _version++;
        }
    }

    /// <inheritdoc />
    public Task<object> GetStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(_state);
    }

    /// <inheritdoc />
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _state = new TState();
        _version = 0;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a specific event type.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task HandleEventAsync(TEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the projection.
    /// </summary>
    protected TState State => _state;

    /// <summary>
    /// Updates the state of the projection.
    /// </summary>
    /// <param name="state">The new state.</param>
    protected void UpdateState(TState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }
}

/// <summary>
/// Statistics about the projection manager.
/// </summary>
#pragma warning disable IDE0051 // Remove unused private members
public sealed class ProjectionManagerStatistics
{
    /// <summary>
    /// Gets or sets the total number of projections.
    /// </summary>
    public int TotalProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of active projections.
    /// </summary>
    public int ActiveProjections { get; init; }

    /// <summary>
    /// Gets or sets the number of rebuilding projections.
    /// </summary>
    public int RebuildingProjections { get; init; }

    /// <summary>
    /// Gets or sets details for each projection.
    /// </summary>
    public Dictionary<string, ProjectionDetails> ProjectionDetails { get; init; } = new();
}
#pragma warning restore IDE0051

/// <summary>
/// Details about a specific projection.
/// </summary>
#pragma warning disable IDE0051 // Remove unused private members
public sealed class ProjectionDetails
{
    /// <summary>
    /// Gets or sets the projection identifier.
    /// </summary>
    public string ProjectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the last processed version.
    /// </summary>
    public int LastProcessedVersion { get; init; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Gets or sets whether the projection is rebuilding.
    /// </summary>
    public bool IsRebuilding { get; init; }
}
#pragma warning restore IDE0051

/// <summary>
/// Interface for persisting projection checkpoints.
/// </summary>
public interface IProjectionCheckpointStore
{
    /// <summary>
    /// Gets the last checkpoint position for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The last checkpoint position (0 if no checkpoint exists).</returns>
    Task<Result<long>> GetCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a checkpoint position for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="position">The checkpoint position.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> SaveCheckpointAsync(
        string projectionId,
        string aggregateId,
        long position,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint for a projection.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> DeleteCheckpointAsync(
        string projectionId,
        string aggregateId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents progress information during projection rebuild.
/// </summary>
/// <param name="ProjectionId">The projection identifier.</param>
/// <param name="ProcessedEvents">Number of events processed so far.</param>
/// <param name="TotalEvents">Total number of events to process.</param>
/// <param name="PercentComplete">Percentage of completion (0-100).</param>
/// <param name="IsCompleted">Whether the rebuild is completed.</param>
/// <param name="EventsPerSecond">Processing rate in events per second.</param>
public sealed record ProjectionRebuildProgress(
    string ProjectionId,
    int ProcessedEvents,
    int TotalEvents,
    double PercentComplete,
    bool IsCompleted,
    double EventsPerSecond = 0.0
);
#pragma warning restore IDE0051
