// -----------------------------------------------------------------------
// <copyright file="Metrics.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Compendium.Infrastructure.Observability;

/// <summary>
/// Interface for collecting application metrics.
/// </summary>
public interface IMetrics
{
    /// <summary>
    /// Increments a counter metric.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The value to increment by.</param>
    /// <param name="tags">Optional tags.</param>
    void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a gauge value.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The value to record.</param>
    /// <param name="tags">Optional tags.</param>
    void RecordValue(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a duration measurement.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="duration">The duration to record.</param>
    /// <param name="tags">Optional tags.</param>
    void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Starts a timer for measuring duration.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>A disposable timer.</returns>
    IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records an event occurrence.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="processingTimeMs">The processing time in milliseconds.</param>
    void RecordEvent(string eventName, string aggregateId, string eventType, double processingTimeMs);

    /// <summary>
    /// Records a projection rebuild.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    void RecordProjectionRebuild(string projectionId, double durationMs);

    /// <summary>
    /// Records a circuit breaker trip.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    void RecordCircuitBreakerTrip(string serviceName);

    /// <summary>
    /// Records encryption operation timing.
    /// </summary>
    /// <param name="operation">The encryption operation.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    void RecordEncryptionOperation(string operation, double durationMs);

    /// <summary>
    /// Records connection pool semaphore wait time.
    /// </summary>
    /// <param name="durationMs">The wait duration in milliseconds.</param>
    /// <param name="operation">The database operation type.</param>
    void RecordConnectionSemaphoreWait(double durationMs, string operation);

    /// <summary>
    /// Records connection acquisition time from the pool.
    /// </summary>
    /// <param name="durationMs">The acquisition duration in milliseconds.</param>
    /// <param name="operation">The database operation type.</param>
    void RecordConnectionAcquisition(double durationMs, string operation);

    /// <summary>
    /// Records database query execution time.
    /// </summary>
    /// <param name="queryType">The type of query (select, insert, update, delete).</param>
    /// <param name="durationMs">The execution duration in milliseconds.</param>
    /// <param name="operation">The database operation name.</param>
    void RecordQueryExecution(string queryType, double durationMs, string operation);

    /// <summary>
    /// Records the current number of active connections.
    /// </summary>
    /// <param name="count">The number of active connections.</param>
    void RecordActiveConnections(int count);

    /// <summary>
    /// Records the current semaphore queue length.
    /// </summary>
    /// <param name="queueLength">The number of tasks waiting for semaphore.</param>
    void RecordSemaphoreQueueLength(int queueLength);

    /// <summary>
    /// Records a connection pool error.
    /// </summary>
    /// <param name="errorType">The type of error (timeout, exhaustion, etc.).</param>
    /// <param name="operation">The operation that caused the error.</param>
    void RecordConnectionError(string errorType, string operation);
}

/// <summary>
/// Production-ready metrics collector using System.Diagnostics.Metrics.
/// </summary>
public sealed class MetricsCollector : IMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _eventCounter;
    private readonly Histogram<double> _eventProcessingTime;
    private readonly Counter<long> _projectionRebuildCounter;
    private readonly UpDownCounter<long> _activeProjections;
    private readonly Histogram<double> _encryptionTime;
    private readonly Counter<long> _circuitBreakerTrips;
    private readonly Histogram<double> _operationDuration;
    private readonly Counter<long> _operationCounter;
    private readonly UpDownCounter<double> _gaugeValues;
    private readonly Histogram<double> _connectionSemaphoreWait;
    private readonly Histogram<double> _connectionAcquisition;
    private readonly Histogram<double> _queryExecution;
    private readonly UpDownCounter<long> _activeConnections;
    private readonly UpDownCounter<long> _semaphoreQueueLength;
    private readonly Counter<long> _connectionErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsCollector"/> class.
    /// </summary>
    /// <param name="meterName">The meter name.</param>
    public MetricsCollector(string meterName = "Compendium.Infrastructure")
    {
        _meter = new Meter(meterName, "1.0.0");

        _eventCounter = _meter.CreateCounter<long>(
            "compendium.events.total",
            description: "Total number of events processed");

        _eventProcessingTime = _meter.CreateHistogram<double>(
            "compendium.events.processing.duration",
            "ms",
            "Time taken to process events");

        _projectionRebuildCounter = _meter.CreateCounter<long>(
            "compendium.projections.rebuilds.total",
            description: "Total number of projection rebuilds");

        _activeProjections = _meter.CreateUpDownCounter<long>(
            "compendium.projections.active",
            description: "Number of active projections");

        _encryptionTime = _meter.CreateHistogram<double>(
            "compendium.encryption.duration",
            "ms",
            "Time taken for encryption operations");

        _circuitBreakerTrips = _meter.CreateCounter<long>(
            "compendium.circuitbreaker.trips.total",
            description: "Total circuit breaker trips");

        _operationDuration = _meter.CreateHistogram<double>(
            "compendium.operation.duration",
            "ms",
            "Duration of operations");

        _operationCounter = _meter.CreateCounter<long>(
            "compendium.operation.total",
            description: "Total number of operations");

        _gaugeValues = _meter.CreateUpDownCounter<double>(
            "compendium.gauge.value",
            description: "Gauge values");

        _connectionSemaphoreWait = _meter.CreateHistogram<double>(
            "compendium.connection.semaphore.wait",
            "ms",
            "Time waiting for connection semaphore");

        _connectionAcquisition = _meter.CreateHistogram<double>(
            "compendium.connection.acquisition.duration",
            "ms",
            "Time to acquire connection from pool");

        _queryExecution = _meter.CreateHistogram<double>(
            "compendium.database.query.duration",
            "ms",
            "Time to execute database queries");

        _activeConnections = _meter.CreateUpDownCounter<long>(
            "compendium.connection.active",
            description: "Number of active database connections");

        _semaphoreQueueLength = _meter.CreateUpDownCounter<long>(
            "compendium.connection.semaphore.queue",
            description: "Number of tasks waiting for semaphore");

        _connectionErrors = _meter.CreateCounter<long>(
            "compendium.connection.errors.total",
            description: "Total connection pool errors");
    }

    /// <inheritdoc />
    public void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object?>[] tags)
    {
        _operationCounter.Add((long)value, tags);
    }

    /// <inheritdoc />
    public void RecordValue(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        _gaugeValues.Add(value, tags);
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags)
    {
        _operationDuration.Record(duration.TotalMilliseconds, tags);
    }

    /// <inheritdoc />
    public IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags)
    {
        return new MetricTimer(this, name, tags);
    }

    /// <inheritdoc />
    public void RecordEvent(string eventName, string aggregateId, string eventType, double processingTimeMs)
    {
        _eventCounter.Add(1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("aggregate.id", aggregateId));

        _eventProcessingTime.Record(processingTimeMs,
            new KeyValuePair<string, object?>("event.type", eventType));
    }

    /// <inheritdoc />
    public void RecordProjectionRebuild(string projectionId, double durationMs)
    {
        _projectionRebuildCounter.Add(1,
            new KeyValuePair<string, object?>("projection.id", projectionId));
    }

    /// <inheritdoc />
    public void RecordCircuitBreakerTrip(string serviceName)
    {
        _circuitBreakerTrips.Add(1,
            new KeyValuePair<string, object?>("service.name", serviceName));
    }

    /// <inheritdoc />
    public void RecordEncryptionOperation(string operation, double durationMs)
    {
        _encryptionTime.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordConnectionSemaphoreWait(double durationMs, string operation)
    {
        _connectionSemaphoreWait.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordConnectionAcquisition(double durationMs, string operation)
    {
        _connectionAcquisition.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordQueryExecution(string queryType, double durationMs, string operation)
    {
        _queryExecution.Record(durationMs,
            new KeyValuePair<string, object?>("query.type", queryType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordActiveConnections(int count)
    {
        // Store the absolute value as a gauge metric
        // Note: For true gauge behavior with System.Diagnostics.Metrics,
        // consider using ObservableGauge with a callback
        _gaugeValues.Add(count, new KeyValuePair<string, object?>("metric.name", "connection.active"));
    }

    /// <inheritdoc />
    public void RecordSemaphoreQueueLength(int queueLength)
    {
        // Store the absolute value as a gauge metric
        _gaugeValues.Add(queueLength, new KeyValuePair<string, object?>("metric.name", "connection.semaphore.queue"));
    }

    /// <inheritdoc />
    public void RecordConnectionError(string errorType, string operation)
    {
        _connectionErrors.Add(1,
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Disposes the metrics collector.
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}

/// <summary>
/// In-memory implementation of metrics collection for development and testing purposes.
/// Stores metric data in memory without external dependencies.
/// </summary>
public sealed class InMemoryMetrics : IMetrics
{
    private readonly ConcurrentDictionary<string, MetricEntry> _metrics = new();
    private readonly ILogger<InMemoryMetrics> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryMetrics"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public InMemoryMetrics(ILogger<InMemoryMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object?>[] tags)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
        }

        var key = CreateKey(name, tags);
        _metrics.AddOrUpdate(key,
            new MetricEntry { Name = name, Type = MetricType.Counter, Value = value, Tags = tags, LastUpdated = DateTime.UtcNow },
            (_, existing) => existing with { Value = existing.Value + value, LastUpdated = DateTime.UtcNow });

        _logger.LogDebug("Counter {MetricName} incremented by {Value}", name, value);
    }

    /// <inheritdoc />
    public void RecordValue(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
        }

        var key = CreateKey(name, tags);
        _metrics.AddOrUpdate(key,
            new MetricEntry { Name = name, Type = MetricType.Gauge, Value = value, Tags = tags, LastUpdated = DateTime.UtcNow },
            (_, _) => new MetricEntry { Name = name, Type = MetricType.Gauge, Value = value, Tags = tags, LastUpdated = DateTime.UtcNow });

        _logger.LogDebug("Gauge {MetricName} set to {Value}", name, value);
    }

    /// <inheritdoc />
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object?>[] tags)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
        }

        var key = CreateKey(name, tags);
        var value = duration.TotalMilliseconds;

        _metrics.AddOrUpdate(key,
            new MetricEntry { Name = name, Type = MetricType.Histogram, Value = value, Tags = tags, LastUpdated = DateTime.UtcNow },
            (_, existing) => existing with { Value = (existing.Value + value) / 2, LastUpdated = DateTime.UtcNow }); // Simple average

        _logger.LogDebug("Duration {MetricName} recorded: {Duration}ms", name, value);
    }

    /// <inheritdoc />
    public IDisposable StartTimer(string name, params KeyValuePair<string, object?>[] tags)
    {
        return new MetricTimer(this, name, tags);
    }

    /// <inheritdoc />
    public void RecordEvent(string eventName, string aggregateId, string eventType, double processingTimeMs)
    {
        IncrementCounter("events.total", 1,
            new KeyValuePair<string, object?>("event.name", eventName),
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("aggregate.id", aggregateId));

        RecordDuration("events.processing.duration", TimeSpan.FromMilliseconds(processingTimeMs),
            new KeyValuePair<string, object?>("event.type", eventType));
    }

    /// <inheritdoc />
    public void RecordProjectionRebuild(string projectionId, double durationMs)
    {
        IncrementCounter("projections.rebuilds.total", 1,
            new KeyValuePair<string, object?>("projection.id", projectionId));

        RecordDuration("projections.rebuild.duration", TimeSpan.FromMilliseconds(durationMs),
            new KeyValuePair<string, object?>("projection.id", projectionId));
    }

    /// <inheritdoc />
    public void RecordCircuitBreakerTrip(string serviceName)
    {
        IncrementCounter("circuitbreaker.trips.total", 1,
            new KeyValuePair<string, object?>("service.name", serviceName));
    }

    /// <inheritdoc />
    public void RecordEncryptionOperation(string operation, double durationMs)
    {
        RecordDuration("encryption.operation.duration", TimeSpan.FromMilliseconds(durationMs),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordConnectionSemaphoreWait(double durationMs, string operation)
    {
        RecordDuration("connection.semaphore.wait", TimeSpan.FromMilliseconds(durationMs),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordConnectionAcquisition(double durationMs, string operation)
    {
        RecordDuration("connection.acquisition.duration", TimeSpan.FromMilliseconds(durationMs),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordQueryExecution(string queryType, double durationMs, string operation)
    {
        RecordDuration("database.query.duration", TimeSpan.FromMilliseconds(durationMs),
            new KeyValuePair<string, object?>("query.type", queryType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <inheritdoc />
    public void RecordActiveConnections(int count)
    {
        RecordValue("connection.active", count);
    }

    /// <inheritdoc />
    public void RecordSemaphoreQueueLength(int queueLength)
    {
        RecordValue("connection.semaphore.queue", queueLength);
    }

    /// <inheritdoc />
    public void RecordConnectionError(string errorType, string operation)
    {
        IncrementCounter("connection.errors.total", 1,
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Gets all collected metrics.
    /// </summary>
    /// <returns>A read-only dictionary of all collected metrics.</returns>
    public IReadOnlyDictionary<string, MetricEntry> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static string CreateKey(string name, KeyValuePair<string, object?>[] tags)
    {
        if (tags.Length == 0)
        {
            return name;
        }

        var tagString = string.Join(",", tags.Select(t => $"{t.Key}={t.Value}"));
        return $"{name}[{tagString}]";
    }
}

/// <summary>
/// Represents a metric entry with its type, value, tags, and metadata.
/// </summary>
public sealed record MetricEntry
{
    /// <summary>
    /// Gets or initializes the name of the metric.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or initializes the type of the metric.
    /// </summary>
    public MetricType Type { get; init; }
    /// <summary>
    /// Gets or initializes the value of the metric.
    /// </summary>
    public double Value { get; init; }
    /// <summary>
    /// Gets or initializes the tags associated with the metric.
    /// </summary>
    public KeyValuePair<string, object?>[] Tags { get; init; } = Array.Empty<KeyValuePair<string, object?>>();
    /// <summary>
    /// Gets or initializes the timestamp when the metric was last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Defines the types of metrics that can be collected.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// A counter metric that tracks cumulative values.
    /// </summary>
    Counter,
    /// <summary>
    /// A gauge metric that tracks point-in-time values.
    /// </summary>
    Gauge,
    /// <summary>
    /// A histogram metric that tracks distributions of values.
    /// </summary>
    Histogram
}

/// <summary>
/// Internal timer class for measuring operation duration.
/// Automatically records the elapsed time when disposed.
/// </summary>
internal sealed class MetricTimer : IDisposable
{
    private readonly IMetrics _metrics;
    private readonly string _name;
    private readonly KeyValuePair<string, object?>[] _tags;
    private readonly Stopwatch _stopwatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricTimer"/> class.
    /// </summary>
    /// <param name="metrics">The metrics collector.</param>
    /// <param name="name">The name of the metric.</param>
    /// <param name="tags">The tags for the metric.</param>
    public MetricTimer(IMetrics metrics, string name, KeyValuePair<string, object?>[] tags)
    {
        _metrics = metrics;
        _name = name;
        _tags = tags;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Disposes the timer and records the elapsed duration.
    /// </summary>
    public void Dispose()
    {
        _stopwatch.Stop();
        _metrics.RecordDuration(_name, _stopwatch.Elapsed, _tags);
    }
}
