// -----------------------------------------------------------------------
// <copyright file="CompendiumTelemetry.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Compendium.Core.Telemetry;

/// <summary>
/// Provides centralized OpenTelemetry instrumentation for Compendium framework.
/// Defines ActivitySource for distributed tracing and Meter for metrics collection.
/// Target overhead: &lt; 5% performance impact.
/// </summary>
public static class CompendiumTelemetry
{
    /// <summary>
    /// The name of the Compendium telemetry source.
    /// </summary>
    public const string SourceName = "Compendium";

    /// <summary>
    /// The version of the Compendium telemetry instrumentation.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// ActivitySource for distributed tracing across Compendium operations.
    /// Use this to create spans for EventStore, CQRS, and Projection operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, Version);

    /// <summary>
    /// Meter for metrics collection (counters, histograms, gauges).
    /// Use this to record performance metrics, throughput, and resource usage.
    /// </summary>
    public static readonly Meter Meter = new(SourceName, Version);

    // ===== EventStore Metrics =====

    /// <summary>
    /// Counter: Total number of events appended to the event store.
    /// Tags: aggregate_type, tenant_id, status (success/failure)
    /// </summary>
    public static readonly Counter<long> EventsAppended = Meter.CreateCounter<long>(
        "compendium.eventstore.events_appended",
        unit: "events",
        description: "Total number of events appended to the event store");

    /// <summary>
    /// Histogram: Duration of event append operations in milliseconds.
    /// Tags: aggregate_type, batch_size, tenant_id
    /// </summary>
    public static readonly Histogram<double> AppendDuration = Meter.CreateHistogram<double>(
        "compendium.eventstore.append_duration",
        unit: "ms",
        description: "Duration of event append operations");

    /// <summary>
    /// Counter: Total number of events read from the event store.
    /// Tags: aggregate_type, tenant_id, paginated (true/false)
    /// </summary>
    public static readonly Counter<long> EventsRead = Meter.CreateCounter<long>(
        "compendium.eventstore.events_read",
        unit: "events",
        description: "Total number of events read from the event store");

    /// <summary>
    /// Histogram: Duration of event read operations in milliseconds.
    /// Tags: aggregate_type, event_count, tenant_id
    /// </summary>
    public static readonly Histogram<double> ReadDuration = Meter.CreateHistogram<double>(
        "compendium.eventstore.read_duration",
        unit: "ms",
        description: "Duration of event read operations");

    // ===== CQRS Metrics =====

    /// <summary>
    /// Counter: Total number of commands dispatched.
    /// Tags: command_type, handler_type, status (success/failure)
    /// </summary>
    public static readonly Counter<long> CommandsDispatched = Meter.CreateCounter<long>(
        "compendium.cqrs.commands_dispatched",
        unit: "commands",
        description: "Total number of commands dispatched");

    /// <summary>
    /// Histogram: Duration of command execution in milliseconds.
    /// Tags: command_type, handler_type, behavior_count
    /// </summary>
    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "compendium.cqrs.command_duration",
        unit: "ms",
        description: "Duration of command execution including pipeline behaviors");

    /// <summary>
    /// Counter: Total number of queries dispatched.
    /// Tags: query_type, handler_type, status (success/failure)
    /// </summary>
    public static readonly Counter<long> QueriesDispatched = Meter.CreateCounter<long>(
        "compendium.cqrs.queries_dispatched",
        unit: "queries",
        description: "Total number of queries dispatched");

    /// <summary>
    /// Histogram: Duration of query execution in milliseconds.
    /// Tags: query_type, handler_type
    /// </summary>
    public static readonly Histogram<double> QueryDuration = Meter.CreateHistogram<double>(
        "compendium.cqrs.query_duration",
        unit: "ms",
        description: "Duration of query execution");

    // ===== Projection Metrics =====

    /// <summary>
    /// Counter: Total number of events processed by projections.
    /// Tags: projection_id, event_type, status (success/failure)
    /// </summary>
    public static readonly Counter<long> ProjectionEventsProcessed = Meter.CreateCounter<long>(
        "compendium.projection.events_processed",
        unit: "events",
        description: "Total number of events processed by projections");

    /// <summary>
    /// Histogram: Duration of projection event processing in milliseconds.
    /// Tags: projection_id, event_type
    /// </summary>
    public static readonly Histogram<double> ProjectionProcessDuration = Meter.CreateHistogram<double>(
        "compendium.projection.process_duration",
        unit: "ms",
        description: "Duration of projection event processing");

    /// <summary>
    /// Counter: Total number of projection rebuilds.
    /// Tags: projection_id, status (success/failure)
    /// </summary>
    public static readonly Counter<long> ProjectionRebuilds = Meter.CreateCounter<long>(
        "compendium.projection.rebuilds",
        unit: "rebuilds",
        description: "Total number of projection rebuilds");

    /// <summary>
    /// Counter: Total number of projection rebuilds completed (deprecated - use ProjectionRebuilds).
    /// Tags: projection_id, status (success/failure)
    /// </summary>
    public static readonly Counter<long> ProjectionRebuildsCompleted = Meter.CreateCounter<long>(
        "compendium.projection.rebuilds_completed",
        unit: "rebuilds",
        description: "Total number of projection rebuilds completed");

    /// <summary>
    /// Histogram: Duration of projection rebuild operations in milliseconds.
    /// Tags: projection_id, event_count
    /// </summary>
    public static readonly Histogram<double> ProjectionRebuildDuration = Meter.CreateHistogram<double>(
        "compendium.projection.rebuild_duration",
        unit: "ms",
        description: "Duration of complete projection rebuild operations");

    /// <summary>
    /// Histogram: Projection lag in seconds - time between event creation and projection processing.
    /// Tags: projection_id, aggregate_type
    /// </summary>
    public static readonly Histogram<double> ProjectionLag = Meter.CreateHistogram<double>(
        "compendium.projection.lag_seconds",
        unit: "s",
        description: "Time lag between event creation and projection processing in seconds");

    // Note: Observable gauges removed for simplicity in COMP-016.
    // Can be added later with proper callback implementation when projection state tracking is available.

    // ===== Idempotency Metrics =====

    /// <summary>
    /// Counter: Total number of idempotency cache hits.
    /// Tags: operation_type, tenant_id
    /// </summary>
    public static readonly Counter<long> IdempotencyCacheHits = Meter.CreateCounter<long>(
        "compendium.idempotency.cache_hits",
        unit: "hits",
        description: "Total number of idempotency cache hits (duplicate operations detected)");

    /// <summary>
    /// Counter: Total number of idempotency cache misses.
    /// Tags: operation_type, tenant_id
    /// </summary>
    public static readonly Counter<long> IdempotencyCacheMisses = Meter.CreateCounter<long>(
        "compendium.idempotency.cache_misses",
        unit: "misses",
        description: "Total number of idempotency cache misses (new operations)");

    /// <summary>
    /// Counter: Total number of idempotency key generation operations.
    /// Tags: operation_type, status (success/failure)
    /// </summary>
    public static readonly Counter<long> IdempotencyKeysGenerated = Meter.CreateCounter<long>(
        "compendium.idempotency.keys_generated",
        unit: "keys",
        description: "Total number of idempotency keys generated");

    /// <summary>
    /// Histogram: Duration of idempotency check operations in milliseconds.
    /// Tags: operation_type, result (hit/miss)
    /// </summary>
    public static readonly Histogram<double> IdempotencyCheckDuration = Meter.CreateHistogram<double>(
        "compendium.idempotency.check_duration",
        unit: "ms",
        description: "Duration of idempotency check operations");

    // ===== Activity Names (for consistent span naming) =====

    /// <summary>
    /// Activity names for EventStore operations.
    /// </summary>
    public static class EventStoreActivities
    {
        /// <summary>Append events to stream</summary>
        public const string AppendEvents = "eventstore.append";

        /// <summary>Get events from stream</summary>
        public const string GetEvents = "eventstore.get";

        /// <summary>Get stream statistics</summary>
        public const string GetStatistics = "eventstore.statistics";

        /// <summary>Initialize schema</summary>
        public const string InitializeSchema = "eventstore.init_schema";
    }

    /// <summary>
    /// Activity names for CQRS operations.
    /// </summary>
    public static class CqrsActivities
    {
        /// <summary>Dispatch command</summary>
        public const string DispatchCommand = "cqrs.command.dispatch";

        /// <summary>Execute command handler</summary>
        public const string ExecuteHandler = "cqrs.command.handler";

        /// <summary>Execute pipeline behavior</summary>
        public const string ExecuteBehavior = "cqrs.command.behavior";

        /// <summary>Dispatch query</summary>
        public const string DispatchQuery = "cqrs.query.dispatch";

        /// <summary>Execute query handler</summary>
        public const string ExecuteQueryHandler = "cqrs.query.handler";
    }

    /// <summary>
    /// Activity names for Projection operations.
    /// </summary>
    public static class ProjectionActivities
    {
        /// <summary>Process single event</summary>
        public const string ProcessEvent = "projection.process_event";

        /// <summary>Rebuild projection</summary>
        public const string RebuildProjection = "projection.rebuild";

        /// <summary>Apply event to projection</summary>
        public const string ApplyEvent = "projection.apply";

        /// <summary>Get projection state</summary>
        public const string GetState = "projection.get_state";
    }

    /// <summary>
    /// Activity names for Idempotency operations.
    /// </summary>
    public static class IdempotencyActivities
    {
        /// <summary>Check idempotency key</summary>
        public const string CheckKey = "idempotency.check";

        /// <summary>Store idempotency key</summary>
        public const string StoreKey = "idempotency.store";

        /// <summary>Generate idempotency key</summary>
        public const string GenerateKey = "idempotency.generate";
    }

    /// <summary>
    /// Common tag keys for consistent tagging across all metrics and traces.
    /// </summary>
    public static class Tags
    {
        /// <summary>Aggregate type (e.g., "Order", "Configuration")</summary>
        public const string AggregateType = "aggregate_type";

        /// <summary>Aggregate ID</summary>
        public const string AggregateId = "aggregate_id";

        /// <summary>Tenant ID (for multi-tenant scenarios)</summary>
        public const string TenantId = "tenant_id";

        /// <summary>Event type (e.g., "OrderPlaced", "ConfigurationCreated")</summary>
        public const string EventType = "event_type";

        /// <summary>Number of events in batch</summary>
        public const string BatchSize = "batch_size";

        /// <summary>Number of events in result</summary>
        public const string EventCount = "event_count";

        /// <summary>Operation status (success/failure)</summary>
        public const string Status = "status";

        /// <summary>Command type name</summary>
        public const string CommandType = "command_type";

        /// <summary>Handler type name</summary>
        public const string HandlerType = "handler_type";

        /// <summary>Query type name</summary>
        public const string QueryType = "query_type";

        /// <summary>Projection identifier</summary>
        public const string ProjectionId = "projection_id";

        /// <summary>Number of pipeline behaviors</summary>
        public const string BehaviorCount = "behavior_count";

        /// <summary>Whether operation used pagination</summary>
        public const string Paginated = "paginated";

        /// <summary>Error type when status=failure</summary>
        public const string ErrorType = "error_type";

        /// <summary>Error message when status=failure</summary>
        public const string ErrorMessage = "error_message";

        /// <summary>Operation type (for idempotency tracking)</summary>
        public const string OperationType = "operation_type";

        /// <summary>Result of operation (e.g., "hit", "miss" for cache operations)</summary>
        public const string Result = "result";

        /// <summary>Event timestamp for lag calculation</summary>
        public const string EventTimestamp = "event_timestamp";
    }

    /// <summary>
    /// Tag values for status.
    /// </summary>
    public static class StatusValues
    {
        /// <summary>Operation succeeded</summary>
        public const string Success = "success";

        /// <summary>Operation failed</summary>
        public const string Failure = "failure";
    }

    /// <summary>
    /// Tag values for cache operation results.
    /// </summary>
    public static class CacheResults
    {
        /// <summary>Cache hit (item found)</summary>
        public const string Hit = "hit";

        /// <summary>Cache miss (item not found)</summary>
        public const string Miss = "miss";
    }
}
