// -----------------------------------------------------------------------
// <copyright file="IEventStore.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.EventSourcing;

/// <summary>
/// Interface for event store implementations.
/// Provides methods for storing and retrieving domain events with comprehensive functionality.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to the event store for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Result> AppendEventsAsync(
        string aggregateId,
        IEnumerable<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the events.</returns>
    Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for a specific aggregate starting from a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="fromVersion">The version to start from (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the events.</returns>
    Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsAsync(
        string aggregateId,
        long fromVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for a specific aggregate within a version range.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="fromVersion">The version to start from (inclusive).</param>
    /// <param name="toVersion">The version to end at (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the events.</returns>
    Task<Result<IReadOnlyList<IDomainEvent>>> GetEventsInRangeAsync(
        string aggregateId,
        long fromVersion,
        long toVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last event for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the last event.</returns>
    Task<Result<IDomainEvent>> GetLastEventAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current version of the aggregate.</returns>
    Task<Result<long>> GetVersionAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate exists in the event store.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the aggregate exists; otherwise, false.</returns>
    Task<Result<bool>> ExistsAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the event store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Event store statistics.</returns>
    Task<Result<EventStoreStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about the event store.
/// </summary>
public sealed class EventStoreStatistics
{
    /// <summary>
    /// Gets or sets the total number of aggregates.
    /// </summary>
    public int TotalAggregates { get; set; }

    /// <summary>
    /// Gets or sets the total number of events.
    /// </summary>
    public long TotalEvents { get; set; }

    /// <summary>
    /// Gets or sets statistics per aggregate.
    /// </summary>
    public Dictionary<string, AggregateStatistics> AggregateStatistics { get; set; } = new();
}

/// <summary>
/// Statistics for a specific aggregate.
/// </summary>
public sealed class AggregateStatistics
{
    /// <summary>
    /// Gets or sets the number of events for this aggregate.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Gets or sets the date of the first event.
    /// </summary>
    public DateTimeOffset? FirstEventDate { get; set; }

    /// <summary>
    /// Gets or sets the date of the last event.
    /// </summary>
    public DateTimeOffset? LastEventDate { get; set; }

    /// <summary>
    /// Gets or sets the current version of the aggregate.
    /// </summary>
    public long CurrentVersion { get; set; }
}
