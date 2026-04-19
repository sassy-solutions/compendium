// -----------------------------------------------------------------------
// <copyright file="IStreamingEventStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Extended event store interface that supports streaming operations for projections.
/// </summary>
public interface IStreamingEventStore : IEventStore
{
    /// <summary>
    /// Streams events from all streams starting from a global position.
    /// Used for projection rebuilds and live processing.
    /// </summary>
    /// <param name="streamId">Optional stream identifier to filter by specific stream.</param>
    /// <param name="fromPosition">The global position to start from (inclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of event data with metadata.</returns>
    IAsyncEnumerable<EventData> StreamEventsAsync(
        string? streamId,
        long fromPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of events for estimating rebuild progress.
    /// </summary>
    /// <param name="streamId">Optional stream identifier to count specific stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of events.</returns>
    Task<long> GetEventCountAsync(string? streamId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the highest global position in the event store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The highest global position.</returns>
    Task<long> GetMaxGlobalPositionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event data with metadata for projection processing.
/// </summary>
public class EventData
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets or sets the stream identifier.
    /// </summary>
    public string StreamId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the position within the stream.
    /// </summary>
    public long StreamPosition { get; init; }

    /// <summary>
    /// Gets or sets the global position across all streams.
    /// </summary>
    public long GlobalPosition { get; init; }

    /// <summary>
    /// Gets or sets the event type name.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event data.
    /// </summary>
    public string EventDataJson { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deserialized domain event.
    /// </summary>
    public IDomainEvent Event { get; init; } = default!;

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the user identifier associated with the event.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets additional headers and metadata.
    /// </summary>
    public Dictionary<string, object>? Headers { get; init; }
}
