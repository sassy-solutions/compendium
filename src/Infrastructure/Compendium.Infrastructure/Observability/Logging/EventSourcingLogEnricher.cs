// -----------------------------------------------------------------------
// <copyright file="EventSourcingLogEnricher.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Serilog.Core;
using Serilog.Events;

namespace Compendium.Infrastructure.Observability.Logging;

/// <summary>
/// Serilog enricher that adds event sourcing context properties to log events.
/// Enriches logs with AggregateId, TenantId, EventType, and CorrelationId from the current context.
/// </summary>
public sealed class EventSourcingLogEnricher : ILogEventEnricher
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcingLogEnricher"/> class.
    /// </summary>
    /// <param name="correlationIdProvider">The correlation ID provider.</param>
    public EventSourcingLogEnricher(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
    }

    /// <summary>
    /// Enriches the log event with event sourcing context properties.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">The property factory.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Add CorrelationId
        var correlationId = _correlationIdProvider.GetCorrelationId();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));

        // Add event sourcing context from AsyncLocal storage
        if (EventSourcingContext.Current.AggregateId is not null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("AggregateId", EventSourcingContext.Current.AggregateId));
        }

        if (EventSourcingContext.Current.TenantId is not null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("TenantId", EventSourcingContext.Current.TenantId));
        }

        if (EventSourcingContext.Current.EventType is not null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("EventType", EventSourcingContext.Current.EventType));
        }

        if (EventSourcingContext.Current.AggregateVersion.HasValue)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("AggregateVersion", EventSourcingContext.Current.AggregateVersion.Value));
        }
    }
}

/// <summary>
/// Provides thread-safe storage for event sourcing context information using AsyncLocal.
/// This allows automatic enrichment of logs with event sourcing metadata.
/// </summary>
public sealed class EventSourcingContext
{
    private static readonly AsyncLocal<EventSourcingContext> _current = new();

    /// <summary>
    /// Gets or sets the current event sourcing context for the async flow.
    /// </summary>
    public static EventSourcingContext Current
    {
        get => _current.Value ??= new EventSourcingContext();
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets or sets the aggregate ID being processed.
    /// </summary>
    public string? AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the event type being processed.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the aggregate version being processed.
    /// </summary>
    public long? AggregateVersion { get; set; }

    /// <summary>
    /// Creates a disposable scope that automatically populates event sourcing context.
    /// When disposed, restores the previous context.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="eventType">The event type (optional).</param>
    /// <param name="aggregateVersion">The aggregate version (optional).</param>
    /// <returns>A disposable scope that manages the event sourcing context.</returns>
    public static IDisposable BeginScope(
        string aggregateId,
        string? tenantId = null,
        string? eventType = null,
        long? aggregateVersion = null)
    {
        return new EventSourcingContextScope(aggregateId, tenantId, eventType, aggregateVersion);
    }

    /// <summary>
    /// Clears the current event sourcing context.
    /// </summary>
    public static void Clear()
    {
        _current.Value = new EventSourcingContext();
    }
}

/// <summary>
/// A disposable scope for managing event sourcing context.
/// Automatically restores the previous context when disposed.
/// </summary>
internal sealed class EventSourcingContextScope : IDisposable
{
    private readonly EventSourcingContext _previousContext;

    public EventSourcingContextScope(
        string aggregateId,
        string? tenantId = null,
        string? eventType = null,
        long? aggregateVersion = null)
    {
        _previousContext = EventSourcingContext.Current;

        EventSourcingContext.Current = new EventSourcingContext
        {
            AggregateId = aggregateId,
            TenantId = tenantId,
            EventType = eventType,
            AggregateVersion = aggregateVersion
        };
    }

    public void Dispose()
    {
        EventSourcingContext.Current = _previousContext;
    }
}
