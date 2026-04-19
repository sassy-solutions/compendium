// -----------------------------------------------------------------------
// <copyright file="LoggingExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Observability.Logging;

/// <summary>
/// Extension methods for ILogger that provide convenient logging methods
/// with event sourcing context enrichment.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Creates a logging scope with event sourcing context that enriches all logs within the scope.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="eventType">The event type (optional).</param>
    /// <param name="aggregateVersion">The aggregate version (optional).</param>
    /// <returns>A disposable scope.</returns>
    /// <example>
    /// <code>
    /// using (_logger.BeginEventSourcingScope("user-123", "tenant-456", "UserCreated", 1))
    /// {
    ///     _logger.LogInformation("Processing user creation event");
    ///     // All logs in this scope will include AggregateId, TenantId, EventType, AggregateVersion
    /// }
    /// </code>
    /// </example>
    public static IDisposable BeginEventSourcingScope(
        this ILogger logger,
        string aggregateId,
        string? tenantId = null,
        string? eventType = null,
        long? aggregateVersion = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        return EventSourcingContext.BeginScope(aggregateId, tenantId, eventType, aggregateVersion);
    }

    /// <summary>
    /// Logs an event being appended to the event store.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="aggregateVersion">The aggregate version.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void LogEventAppended(
        this ILogger logger,
        string aggregateId,
        string eventType,
        long aggregateVersion,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        using (EventSourcingContext.BeginScope(aggregateId, tenantId, eventType, aggregateVersion))
        {
            logger.LogInformation(
                "Event appended: {EventType} for aggregate {AggregateId} at version {AggregateVersion}",
                eventType,
                aggregateId,
                aggregateVersion);
        }
    }

    /// <summary>
    /// Logs an event being loaded from the event store.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="fromVersion">The starting version.</param>
    /// <param name="toVersion">The ending version (optional).</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void LogEventsLoaded(
        this ILogger logger,
        string aggregateId,
        long fromVersion,
        long? toVersion = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        using (EventSourcingContext.BeginScope(aggregateId, tenantId))
        {
            if (toVersion.HasValue)
            {
                logger.LogInformation(
                    "Events loaded for aggregate {AggregateId} from version {FromVersion} to {ToVersion}",
                    aggregateId,
                    fromVersion,
                    toVersion.Value);
            }
            else
            {
                logger.LogInformation(
                    "Events loaded for aggregate {AggregateId} from version {FromVersion}",
                    aggregateId,
                    fromVersion);
            }
        }
    }

    /// <summary>
    /// Logs a projection rebuild starting.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="fromVersion">The starting version.</param>
    public static void LogProjectionRebuildStarted(
        this ILogger logger,
        string projectionName,
        long fromVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionName);
        logger.LogInformation(
            "Projection rebuild started: {ProjectionName} from version {FromVersion}",
            projectionName,
            fromVersion);
    }

    /// <summary>
    /// Logs a projection rebuild completed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="eventsProcessed">The number of events processed.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public static void LogProjectionRebuildCompleted(
        this ILogger logger,
        string projectionName,
        int eventsProcessed,
        long durationMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionName);
        logger.LogInformation(
            "Projection rebuild completed: {ProjectionName}, processed {EventsProcessed} events in {DurationMs}ms",
            projectionName,
            eventsProcessed,
            durationMs);
    }

    /// <summary>
    /// Logs a concurrency conflict during event append.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void LogConcurrencyConflict(
        this ILogger logger,
        string aggregateId,
        long expectedVersion,
        long actualVersion,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        using (EventSourcingContext.BeginScope(aggregateId, tenantId))
        {
            logger.LogWarning(
                "Concurrency conflict: Expected version {ExpectedVersion}, actual version {ActualVersion} for aggregate {AggregateId}",
                expectedVersion,
                actualVersion,
                aggregateId);
        }
    }

    /// <summary>
    /// Logs a snapshot being saved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="version">The snapshot version.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void LogSnapshotSaved(
        this ILogger logger,
        string aggregateId,
        long version,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        using (EventSourcingContext.BeginScope(aggregateId, tenantId, aggregateVersion: version))
        {
            logger.LogInformation(
                "Snapshot saved for aggregate {AggregateId} at version {Version}",
                aggregateId,
                version);
        }
    }

    /// <summary>
    /// Logs a snapshot being loaded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="version">The snapshot version.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void LogSnapshotLoaded(
        this ILogger logger,
        string aggregateId,
        long version,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        using (EventSourcingContext.BeginScope(aggregateId, tenantId, aggregateVersion: version))
        {
            logger.LogInformation(
                "Snapshot loaded for aggregate {AggregateId} at version {Version}",
                aggregateId,
                version);
        }
    }
}
