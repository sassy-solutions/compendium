// -----------------------------------------------------------------------
// <copyright file="CompendiumTelemetryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Telemetry;

namespace Compendium.Core.Tests.Telemetry;

/// <summary>
/// Unit tests for the static <see cref="CompendiumTelemetry"/> registry of ActivitySource,
/// Meter, instruments, activity-name constants, and tag keys.
/// </summary>
public class CompendiumTelemetryTests
{
    [Fact]
    public void SourceName_IsCompendium()
    {
        // Act / Assert
        CompendiumTelemetry.SourceName.Should().Be("Compendium");
    }

    [Fact]
    public void Version_HasSemanticVersionFormat()
    {
        // Act / Assert
        CompendiumTelemetry.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ActivitySource_IsConfigured()
    {
        // Act
        var source = CompendiumTelemetry.ActivitySource;

        // Assert
        source.Should().NotBeNull();
        source.Name.Should().Be(CompendiumTelemetry.SourceName);
        source.Version.Should().Be(CompendiumTelemetry.Version);
    }

    [Fact]
    public void Meter_IsConfigured()
    {
        // Act
        var meter = CompendiumTelemetry.Meter;

        // Assert
        meter.Should().NotBeNull();
        meter.Name.Should().Be(CompendiumTelemetry.SourceName);
        meter.Version.Should().Be(CompendiumTelemetry.Version);
    }

    [Theory]
    [InlineData("EventsAppended", "compendium.eventstore.events_appended")]
    [InlineData("EventsRead", "compendium.eventstore.events_read")]
    [InlineData("CommandsDispatched", "compendium.cqrs.commands_dispatched")]
    [InlineData("QueriesDispatched", "compendium.cqrs.queries_dispatched")]
    [InlineData("ProjectionEventsProcessed", "compendium.projection.events_processed")]
    [InlineData("ProjectionRebuilds", "compendium.projection.rebuilds")]
    [InlineData("ProjectionRebuildsCompleted", "compendium.projection.rebuilds_completed")]
    [InlineData("IdempotencyCacheHits", "compendium.idempotency.cache_hits")]
    [InlineData("IdempotencyCacheMisses", "compendium.idempotency.cache_misses")]
    [InlineData("IdempotencyKeysGenerated", "compendium.idempotency.keys_generated")]
    public void Counter_HasExpectedName(string fieldName, string expectedName)
    {
        // Arrange
        var field = typeof(CompendiumTelemetry).GetField(fieldName);

        // Act
        field.Should().NotBeNull();
        var counter = field!.GetValue(null) as System.Diagnostics.Metrics.Counter<long>;

        // Assert
        counter.Should().NotBeNull();
        counter!.Name.Should().Be(expectedName);
        counter.Unit.Should().NotBeNullOrEmpty();
        counter.Description.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("AppendDuration", "compendium.eventstore.append_duration")]
    [InlineData("ReadDuration", "compendium.eventstore.read_duration")]
    [InlineData("CommandDuration", "compendium.cqrs.command_duration")]
    [InlineData("QueryDuration", "compendium.cqrs.query_duration")]
    [InlineData("ProjectionProcessDuration", "compendium.projection.process_duration")]
    [InlineData("ProjectionRebuildDuration", "compendium.projection.rebuild_duration")]
    [InlineData("ProjectionLag", "compendium.projection.lag_seconds")]
    [InlineData("IdempotencyCheckDuration", "compendium.idempotency.check_duration")]
    public void Histogram_HasExpectedName(string fieldName, string expectedName)
    {
        // Arrange
        var field = typeof(CompendiumTelemetry).GetField(fieldName);

        // Act
        field.Should().NotBeNull();
        var histogram = field!.GetValue(null) as System.Diagnostics.Metrics.Histogram<double>;

        // Assert
        histogram.Should().NotBeNull();
        histogram!.Name.Should().Be(expectedName);
        histogram.Unit.Should().NotBeNullOrEmpty();
        histogram.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void EventStoreActivities_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.EventStoreActivities.AppendEvents.Should().Be("eventstore.append");
        CompendiumTelemetry.EventStoreActivities.GetEvents.Should().Be("eventstore.get");
        CompendiumTelemetry.EventStoreActivities.GetStatistics.Should().Be("eventstore.statistics");
        CompendiumTelemetry.EventStoreActivities.InitializeSchema.Should().Be("eventstore.init_schema");
    }

    [Fact]
    public void CqrsActivities_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.CqrsActivities.DispatchCommand.Should().Be("cqrs.command.dispatch");
        CompendiumTelemetry.CqrsActivities.ExecuteHandler.Should().Be("cqrs.command.handler");
        CompendiumTelemetry.CqrsActivities.ExecuteBehavior.Should().Be("cqrs.command.behavior");
        CompendiumTelemetry.CqrsActivities.DispatchQuery.Should().Be("cqrs.query.dispatch");
        CompendiumTelemetry.CqrsActivities.ExecuteQueryHandler.Should().Be("cqrs.query.handler");
    }

    [Fact]
    public void ProjectionActivities_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.ProjectionActivities.ProcessEvent.Should().Be("projection.process_event");
        CompendiumTelemetry.ProjectionActivities.RebuildProjection.Should().Be("projection.rebuild");
        CompendiumTelemetry.ProjectionActivities.ApplyEvent.Should().Be("projection.apply");
        CompendiumTelemetry.ProjectionActivities.GetState.Should().Be("projection.get_state");
    }

    [Fact]
    public void IdempotencyActivities_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.IdempotencyActivities.CheckKey.Should().Be("idempotency.check");
        CompendiumTelemetry.IdempotencyActivities.StoreKey.Should().Be("idempotency.store");
        CompendiumTelemetry.IdempotencyActivities.GenerateKey.Should().Be("idempotency.generate");
    }

    [Fact]
    public void Tags_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.Tags.AggregateType.Should().Be("aggregate_type");
        CompendiumTelemetry.Tags.AggregateId.Should().Be("aggregate_id");
        CompendiumTelemetry.Tags.TenantId.Should().Be("tenant_id");
        CompendiumTelemetry.Tags.EventType.Should().Be("event_type");
        CompendiumTelemetry.Tags.BatchSize.Should().Be("batch_size");
        CompendiumTelemetry.Tags.EventCount.Should().Be("event_count");
        CompendiumTelemetry.Tags.Status.Should().Be("status");
        CompendiumTelemetry.Tags.CommandType.Should().Be("command_type");
        CompendiumTelemetry.Tags.HandlerType.Should().Be("handler_type");
        CompendiumTelemetry.Tags.QueryType.Should().Be("query_type");
        CompendiumTelemetry.Tags.ProjectionId.Should().Be("projection_id");
        CompendiumTelemetry.Tags.BehaviorCount.Should().Be("behavior_count");
        CompendiumTelemetry.Tags.Paginated.Should().Be("paginated");
        CompendiumTelemetry.Tags.ErrorType.Should().Be("error_type");
        CompendiumTelemetry.Tags.ErrorMessage.Should().Be("error_message");
        CompendiumTelemetry.Tags.OperationType.Should().Be("operation_type");
        CompendiumTelemetry.Tags.Result.Should().Be("result");
        CompendiumTelemetry.Tags.EventTimestamp.Should().Be("event_timestamp");
    }

    [Fact]
    public void StatusValues_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.StatusValues.Success.Should().Be("success");
        CompendiumTelemetry.StatusValues.Failure.Should().Be("failure");
    }

    [Fact]
    public void CacheResults_DefinesExpectedConstants()
    {
        // Assert
        CompendiumTelemetry.CacheResults.Hit.Should().Be("hit");
        CompendiumTelemetry.CacheResults.Miss.Should().Be("miss");
    }

    [Fact]
    public void Counter_CanRecordValueWithoutThrowing()
    {
        // Arrange / Act
        var act = () => CompendiumTelemetry.EventsAppended.Add(
            1,
            new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.AggregateType, "TestAggregate"));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Histogram_CanRecordValueWithoutThrowing()
    {
        // Arrange / Act
        var act = () => CompendiumTelemetry.AppendDuration.Record(
            42.5,
            new KeyValuePair<string, object?>(CompendiumTelemetry.Tags.AggregateType, "TestAggregate"));

        // Assert
        act.Should().NotThrow();
    }
}
