// -----------------------------------------------------------------------
// <copyright file="EventSourcedRepository.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Persistence;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Domain.Specifications;
using Compendium.Infrastructure.EventSourcing;

namespace Compendium.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation for event-sourced aggregates with optional snapshot support.
/// This repository works perfectly without snapshots and adds them as a pure performance optimization.
/// </summary>
/// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class EventSourcedRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore? _snapshotStore;
    private readonly ISnapshotStrategy? _snapshotStrategy;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcedRepository{TAggregate, TId}"/> class.
    /// </summary>
    /// <param name="eventStore">The event store (required).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="snapshotStore">The snapshot store (optional for performance optimization).</param>
    /// <param name="snapshotStrategy">The snapshot strategy (optional, defaults to no snapshots).</param>
    protected EventSourcedRepository(
        IEventStore eventStore,
        ILogger logger,
        ISnapshotStore? snapshotStore = null,
        ISnapshotStrategy? snapshotStrategy = null)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _snapshotStore = snapshotStore;
        _snapshotStrategy = snapshotStrategy ?? new NoSnapshotStrategy();
    }

    /// <inheritdoc />
    public async Task<Result<TAggregate>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        try
        {
            var aggregateId = ConvertIdToString(id);

            // Try to load from snapshot first if available
            var aggregate = await TryLoadFromSnapshotAsync(aggregateId, cancellationToken);
            var fromVersion = 0L;

            if (aggregate.IsSuccess)
            {
                fromVersion = aggregate.Value.Version + 1;
                _logger.LogDebug("Loaded aggregate {AggregateId} from snapshot at version {Version}",
                    aggregateId, fromVersion - 1);
            }

            // Load events from event store (from snapshot version + 1 or from beginning)
            var eventsResult = fromVersion > 0
                ? await _eventStore.GetEventsAsync(aggregateId, fromVersion, cancellationToken)
                : await _eventStore.GetEventsAsync(aggregateId, cancellationToken);

            if (!eventsResult.IsSuccess)
            {
                // If we have a snapshot but failed to load subsequent events,
                // that's still a failure since we might be missing critical events
                return Result.Failure<TAggregate>(eventsResult.Error);
            }

            // If no snapshot and no events, aggregate doesn't exist
            if (!aggregate.IsSuccess && eventsResult.Value.Count == 0)
            {
                return Result.Failure<TAggregate>(
                    Error.NotFound("Repository.AggregateNotFound", $"Aggregate with ID {aggregateId} not found"));
            }

            // Apply events to build/update the aggregate
            var finalAggregate = aggregate.IsSuccess
                ? await ApplyEventsToAggregate(aggregate.Value, eventsResult.Value)
                : await BuildAggregateFromEvents(eventsResult.Value);

            if (!finalAggregate.IsSuccess)
            {
                return Result.Failure<TAggregate>(finalAggregate.Error);
            }

            _logger.LogDebug("Successfully loaded aggregate {AggregateId} with {EventCount} events",
                aggregateId, eventsResult.Value.Count);

            return Result.Success(finalAggregate.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load aggregate with ID {AggregateId}", id);
            return Result.Failure<TAggregate>(
                Error.Failure("Repository.LoadFailed", $"Failed to load aggregate: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<IEnumerable<TAggregate>>> FindAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        // For event sourcing, finding aggregates by specification is complex
        // and typically not efficient since it would require loading all aggregates
        // This could be implemented with a read model/projection instead
        return Task.FromResult(Result.Failure<IEnumerable<TAggregate>>(
            Error.Failure("Repository.FindNotSupported",
                "Find by specification is not supported for event-sourced aggregates. Use read models or projections instead.")));
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        // For event sourcing, Add and Update are the same operation - save uncommitted events
        return await SaveAsync(aggregate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        // For event sourcing, Add and Update are the same operation - save uncommitted events
        return await SaveAsync(aggregate, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        // For event sourcing, we typically don't delete aggregates but mark them as deleted
        // through events. This is application-specific, so we'll make it a virtual method
        return await MarkAsDeletedAsync(aggregate, cancellationToken);
    }

    /// <summary>
    /// Marks an aggregate as deleted. Override this to implement soft deletion through events.
    /// </summary>
    /// <param name="aggregate">The aggregate to mark as deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual Task<Result> MarkAsDeletedAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        // Default implementation: not supported - applications should override to implement soft deletion
        return Task.FromResult(Result.Failure(
            Error.Failure("Repository.DeleteNotSupported",
                "Delete is not supported by default for event-sourced aggregates. Override MarkAsDeletedAsync to implement soft deletion.")));
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        try
        {
            var aggregateId = aggregate.Id;
            var aggregateIdString = ConvertIdToString(aggregateId);
            var uncommittedEvents = aggregate.DomainEvents;

            if (!uncommittedEvents.Any())
            {
                _logger.LogDebug("No uncommitted events to save for aggregate {AggregateId}", aggregateIdString);
                return Result.Success();
            }

            var expectedVersion = aggregate.Version - uncommittedEvents.Count();

            // Save events to event store
            var saveResult = await _eventStore.AppendEventsAsync(
                aggregateIdString,
                uncommittedEvents,
                expectedVersion,
                cancellationToken);

            if (!saveResult.IsSuccess)
            {
                _logger.LogError("Failed to save events for aggregate {AggregateId}: {Error}",
                    aggregateIdString, saveResult.Error.Message);
                return saveResult;
            }

            // Mark events as committed
            aggregate.ClearDomainEvents();

            // Take snapshot if strategy suggests it and snapshot store is available
            await TryTakeSnapshotAsync(aggregate, cancellationToken);

            _logger.LogDebug("Successfully saved {EventCount} events for aggregate {AggregateId}",
                uncommittedEvents.Count(), aggregateIdString);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save aggregate");
            return Result.Failure(
                Error.Failure("Repository.SaveFailed", $"Failed to save aggregate: {ex.Message}"));
        }
    }

    /// <summary>
    /// Attempts to load an aggregate from a snapshot.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate from snapshot or failure if not found/not available.</returns>
    private async Task<Result<TAggregate>> TryLoadFromSnapshotAsync(
        string aggregateId,
        CancellationToken cancellationToken)
    {
        if (_snapshotStore == null)
        {
            return Result.Failure<TAggregate>(
                Error.NotFound("Repository.NoSnapshotStore", "No snapshot store configured"));
        }

        try
        {
            var snapshotResult = await _snapshotStore.GetLatestSnapshotAsync<TAggregate>(
                aggregateId,
                cancellationToken);

            if (!snapshotResult.IsSuccess)
            {
                return Result.Failure<TAggregate>(snapshotResult.Error);
            }

            return Result.Success(snapshotResult.Value.State);
        }
        catch (Exception ex)
        {
            // Log but don't fail - snapshots are optional performance optimization
            _logger.LogWarning(ex, "Failed to load snapshot for aggregate {AggregateId}, will load from events",
                aggregateId);

            return Result.Failure<TAggregate>(
                Error.Failure("Repository.SnapshotLoadFailed", $"Snapshot load failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Attempts to take a snapshot if the strategy suggests it.
    /// </summary>
    /// <param name="aggregate">The aggregate to snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task TryTakeSnapshotAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        if (_snapshotStore == null || _snapshotStrategy == null)
        {
            return;
        }

        try
        {
            var aggregateId = aggregate.Id;
            var aggregateIdString = ConvertIdToString(aggregateId);
            var version = aggregate.Version;
            var eventCount = await GetTotalEventCountAsync(aggregateIdString, cancellationToken);

            if (!_snapshotStrategy.ShouldTakeSnapshot(aggregateIdString, version, eventCount))
            {
                return;
            }

            var snapshotResult = await _snapshotStore.SaveSnapshotAsync(
                aggregateIdString,
                aggregate,
                version,
                cancellationToken);

            if (snapshotResult.IsSuccess)
            {
                _logger.LogDebug("Successfully created snapshot for aggregate {AggregateId} at version {Version}",
                    aggregateIdString, version);
            }
            else
            {
                _logger.LogWarning("Failed to create snapshot for aggregate {AggregateId}: {Error}",
                    aggregateIdString, snapshotResult.Error.Message);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the save operation if snapshot fails - it's just a performance optimization
            _logger.LogWarning(ex, "Failed to create snapshot, continuing with save operation");
        }
    }

    /// <summary>
    /// Gets the total event count for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total event count.</returns>
    private async Task<int> GetTotalEventCountAsync(string aggregateId, CancellationToken cancellationToken)
    {
        try
        {
            var eventsResult = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            return eventsResult.IsSuccess ? eventsResult.Value.Count : 0;
        }
        catch
        {
            return 0; // Default to 0 if we can't determine count
        }
    }

    /// <summary>
    /// Converts the strongly-typed ID to a string.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    /// <returns>The string representation of the ID.</returns>
    protected virtual string ConvertIdToString(TId id) => id.ToString()!;


    /// <summary>
    /// Builds an aggregate from a collection of events.
    /// </summary>
    /// <param name="events">The events to apply.</param>
    /// <returns>The built aggregate.</returns>
    protected abstract Task<Result<TAggregate>> BuildAggregateFromEvents(IReadOnlyList<IDomainEvent> events);

    /// <summary>
    /// Applies events to an existing aggregate instance.
    /// </summary>
    /// <param name="aggregate">The existing aggregate.</param>
    /// <param name="events">The events to apply.</param>
    /// <returns>The updated aggregate.</returns>
    protected abstract Task<Result<TAggregate>> ApplyEventsToAggregate(
        TAggregate aggregate,
        IReadOnlyList<IDomainEvent> events);
}
