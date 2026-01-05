// -----------------------------------------------------------------------
// <copyright file="Tracing.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Compendium.Infrastructure.Observability;

/// <summary>
/// Interface for distributed tracing operations.
/// </summary>
public interface ITracing
{
    /// <summary>
    /// Starts a new trace span.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="parent">The parent span.</param>
    /// <returns>A new trace span.</returns>
    ITraceSpan StartSpan(string operationName, ITraceSpan? parent = null);

    /// <summary>
    /// Gets the current active span.
    /// </summary>
    /// <returns>The current span or null if none is active.</returns>
    ITraceSpan? GetCurrentSpan();

    /// <summary>
    /// Sets the current active span.
    /// </summary>
    /// <param name="span">The span to set as current.</param>
    void SetCurrentSpan(ITraceSpan? span);

    /// <summary>
    /// Starts an activity for the given operation.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>The started activity or null if not sampled.</returns>
    Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Adds an event to the current activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="name">The event name.</param>
    /// <param name="attributes">Optional attributes.</param>
    void AddEvent(Activity? activity, string name, Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Sets the status of an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="description">Optional description.</param>
    void SetStatus(Activity? activity, bool success, string? description = null);
}

/// <summary>
/// Production-ready tracing service using System.Diagnostics.Activity.
/// </summary>
public sealed class TracingService : ITracing
{
    private static readonly ActivitySource _activitySource = new("Compendium.Infrastructure");
    private readonly AsyncLocal<ITraceSpan?> _currentSpan = new();
    private readonly ILogger<TracingService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TracingService"/> class.
    /// </summary>
    /// <param name="logger">The logger (optional for testing).</param>
    public TracingService(ILogger<TracingService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal) => _activitySource.StartActivity(name, kind);

    /// <inheritdoc />
    public void AddEvent(Activity? activity, string name, Dictionary<string, object>? attributes = null)
    {
        if (activity == null)
        {
            return;
        }

        var activityEvent = new ActivityEvent(
            name,
            DateTimeOffset.UtcNow,
            attributes != null ? new ActivityTagsCollection(attributes!) : null);

        activity.AddEvent(activityEvent);
    }

    /// <inheritdoc />
    public void SetStatus(Activity? activity, bool success, string? description = null)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error, description);
    }

    /// <inheritdoc />
    public ITraceSpan StartSpan(string operationName, ITraceSpan? parent = null)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        }

        var parentSpan = parent ?? _currentSpan.Value;
        var span = new InMemoryTraceSpan(operationName, parentSpan, _logger!);

        _currentSpan.Value = span;
        _logger?.LogDebug("Started span {OperationName} with ID {SpanId}", operationName, span.SpanId);

        return span;
    }

    /// <inheritdoc />
    public ITraceSpan? GetCurrentSpan()
    {
        return _currentSpan.Value;
    }

    /// <inheritdoc />
    public void SetCurrentSpan(ITraceSpan? span)
    {
        _currentSpan.Value = span;
    }
}

/// <summary>
/// Represents a trace span that tracks the execution of an operation within a distributed trace.
/// </summary>
public interface ITraceSpan : IDisposable
{
    /// <summary>
    /// Gets the trace ID that groups related spans together.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// Gets the unique identifier for this span.
    /// </summary>
    string SpanId { get; }

    /// <summary>
    /// Gets the name of the operation this span represents.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the timestamp when the span started.
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Gets the timestamp when the span ended, if completed.
    /// </summary>
    DateTime? EndTime { get; }

    /// <summary>
    /// Gets the duration of the span, if completed.
    /// </summary>
    TimeSpan? Duration { get; }

    /// <summary>
    /// Gets the current status of the span.
    /// </summary>
    TraceSpanStatus Status { get; }

    /// <summary>
    /// Gets the tags (metadata) associated with this span.
    /// </summary>
    IReadOnlyDictionary<string, object?> Tags { get; }

    /// <summary>
    /// Gets the events recorded on this span.
    /// </summary>
    IReadOnlyList<TraceEvent> Events { get; }

    /// <summary>
    /// Sets a tag (metadata) on the span.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    void SetTag(string key, object? value);

    /// <summary>
    /// Sets the status of the span.
    /// </summary>
    /// <param name="status">The status to set.</param>
    /// <param name="description">Optional description of the status.</param>
    void SetStatus(TraceSpanStatus status, string? description = null);

    /// <summary>
    /// Adds an event to the span with optional attributes.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="timestamp">Optional timestamp, defaults to current time.</param>
    /// <param name="attributes">Optional event attributes.</param>
    void AddEvent(string name, DateTime? timestamp = null, params KeyValuePair<string, object?>[] attributes);

    /// <summary>
    /// Records an exception on the span, setting error status and adding exception details.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    void RecordException(Exception exception);
}

/// <summary>
/// Defines the possible status values for a trace span.
/// </summary>
public enum TraceSpanStatus
{
    /// <summary>
    /// The span status has not been set.
    /// </summary>
    Unset,

    /// <summary>
    /// The span completed successfully.
    /// </summary>
    Ok,

    /// <summary>
    /// The span completed with an error.
    /// </summary>
    Error
}

/// <summary>
/// Represents an event that occurred during the execution of a trace span.
/// </summary>
public sealed record TraceEvent
{
    /// <summary>
    /// Gets or initializes the name of the event.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets or initializes the attributes associated with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// In-memory implementation of tracing for testing and development scenarios.
/// Stores trace spans in memory without external dependencies.
/// </summary>
public sealed class InMemoryTracing : ITracing
{
    private readonly AsyncLocal<ITraceSpan?> _currentSpan = new();
    private readonly ILogger<InMemoryTracing> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTracing"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public InMemoryTracing(ILogger<InMemoryTracing> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts a new trace span.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="parent">The parent span.</param>
    /// <returns>A new trace span.</returns>
    /// <exception cref="ArgumentException">Thrown when operationName is null or empty.</exception>
    public ITraceSpan StartSpan(string operationName, ITraceSpan? parent = null)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        }

        var parentSpan = parent ?? _currentSpan.Value;
        var span = new InMemoryTraceSpan(operationName, parentSpan, _logger);

        _currentSpan.Value = span;
        _logger.LogDebug("Started span {OperationName} with ID {SpanId}", operationName, span.SpanId);

        return span;
    }

    /// <summary>
    /// Gets the current active span.
    /// </summary>
    /// <returns>The current span or null if none is active.</returns>
    public ITraceSpan? GetCurrentSpan()
    {
        return _currentSpan.Value;
    }

    /// <summary>
    /// Sets the current active span.
    /// </summary>
    /// <param name="span">The span to set as current.</param>
    public void SetCurrentSpan(ITraceSpan? span)
    {
        _currentSpan.Value = span;
    }

    /// <summary>
    /// Starts an activity for the given operation. This is a placeholder implementation.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>Always returns null in this implementation.</returns>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        // This is a placeholder implementation for the interface
        // In a real implementation, this would use ActivitySource
        return null;
    }

    /// <summary>
    /// Adds an event to the activity. This is a placeholder implementation.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="name">The event name.</param>
    /// <param name="attributes">Optional attributes.</param>
    public void AddEvent(Activity? activity, string name, Dictionary<string, object>? attributes = null)
    {
        // This is a placeholder implementation for the interface
        // In a real implementation, this would add events to the activity
    }

    /// <summary>
    /// Sets the status of an activity. This is a placeholder implementation.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="description">Optional description.</param>
    public void SetStatus(Activity? activity, bool success, string? description = null)
    {
        // This is a placeholder implementation for the interface
        // In a real implementation, this would set the activity status
    }
}

internal sealed class InMemoryTraceSpan : ITraceSpan
{
    private readonly Dictionary<string, object?> _tags = new();
    private readonly List<TraceEvent> _events = new();
    private readonly ILogger _logger;
    private readonly ITraceSpan? _parent;
    private bool _disposed;

    public InMemoryTraceSpan(string operationName, ITraceSpan? parent, ILogger logger)
    {
        OperationName = operationName;
        _parent = parent;
        _logger = logger;

        TraceId = parent?.TraceId ?? GenerateId();
        SpanId = GenerateId();
        StartTime = DateTime.UtcNow;
        Status = TraceSpanStatus.Unset;
    }

    public string TraceId { get; }
    public string SpanId { get; }
    public string OperationName { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    public TraceSpanStatus Status { get; private set; }
    public IReadOnlyDictionary<string, object?> Tags => _tags.AsReadOnly();
    public IReadOnlyList<TraceEvent> Events => _events.AsReadOnly();

    public void SetTag(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Tag key cannot be null or empty", nameof(key));
        }

        _tags[key] = value;
        _logger.LogDebug("Set tag {Key}={Value} on span {SpanId}", key, value, SpanId);
    }

    public void SetStatus(TraceSpanStatus status, string? description = null)
    {
        Status = status;
        if (!string.IsNullOrWhiteSpace(description))
        {
            SetTag("status.description", description);
        }

        _logger.LogDebug("Set status {Status} on span {SpanId}", status, SpanId);
    }

    public void AddEvent(string name, DateTime? timestamp = null, params KeyValuePair<string, object?>[] attributes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name cannot be null or empty", nameof(name));
        }

        var traceEvent = new TraceEvent
        {
            Name = name,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Attributes = attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        _events.Add(traceEvent);
        _logger.LogDebug("Added event {EventName} to span {SpanId}", name, SpanId);
    }

    public void RecordException(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        SetStatus(TraceSpanStatus.Error);
        AddEvent("exception", DateTime.UtcNow,
            new KeyValuePair<string, object?>("exception.type", exception.GetType().Name),
            new KeyValuePair<string, object?>("exception.message", exception.Message),
            new KeyValuePair<string, object?>("exception.stacktrace", exception.StackTrace));

        _logger.LogDebug("Recorded exception {ExceptionType} on span {SpanId}", exception.GetType().Name, SpanId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        EndTime = DateTime.UtcNow;

        if (Status == TraceSpanStatus.Unset)
        {
            Status = TraceSpanStatus.Ok;
        }

        _logger.LogDebug("Completed span {OperationName} with ID {SpanId} in {Duration}ms",
            OperationName, SpanId, Duration?.TotalMilliseconds);

        _disposed = true;
    }

    private static string GenerateId()
    {
        return Guid.NewGuid().ToString("N")[..16];
    }
}
