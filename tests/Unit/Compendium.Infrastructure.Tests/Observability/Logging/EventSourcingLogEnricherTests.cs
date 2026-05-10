// -----------------------------------------------------------------------
// <copyright file="EventSourcingLogEnricherTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Observability.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Compendium.Infrastructure.Tests.Observability.Logging;

/// <summary>
/// Unit tests for <see cref="EventSourcingLogEnricher"/> and <see cref="EventSourcingContext"/>.
/// </summary>
public sealed class EventSourcingLogEnricherTests
{
    private readonly ICorrelationIdProvider _correlationProvider;
    private readonly EventSourcingLogEnricher _sut;

    public EventSourcingLogEnricherTests()
    {
        // Arrange
        _correlationProvider = Substitute.For<ICorrelationIdProvider>();
        _correlationProvider.GetCorrelationId().Returns("corr-1");
        _sut = new EventSourcingLogEnricher(_correlationProvider);

        // Reset the AsyncLocal context to keep tests isolated.
        EventSourcingContext.Clear();
    }

    [Fact]
    public void Ctor_NullProvider_Throws()
    {
        // Arrange / Act
        var act = () => new EventSourcingLogEnricher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("correlationIdProvider");
    }

    [Fact]
    public void Enrich_AddsCorrelationIdProperty()
    {
        // Arrange
        var logEvent = CreateLogEvent();
        var factory = new TestLogEventPropertyFactory();

        // Act
        _sut.Enrich(logEvent, factory);

        // Assert
        logEvent.Properties.Should().ContainKey("CorrelationId");
        logEvent.Properties["CorrelationId"].ToString().Should().Contain("corr-1");
    }

    [Fact]
    public void Enrich_WithFullContext_AddsAllProperties()
    {
        // Arrange
        var logEvent = CreateLogEvent();
        var factory = new TestLogEventPropertyFactory();
        EventSourcingContext.Current = new EventSourcingContext
        {
            AggregateId = "agg-1",
            TenantId = "ten-1",
            EventType = "Created",
            AggregateVersion = 5,
        };

        // Act
        _sut.Enrich(logEvent, factory);

        // Assert
        logEvent.Properties.Should().ContainKey("AggregateId");
        logEvent.Properties.Should().ContainKey("TenantId");
        logEvent.Properties.Should().ContainKey("EventType");
        logEvent.Properties.Should().ContainKey("AggregateVersion");
    }

    [Fact]
    public void Enrich_WithEmptyContext_OnlyAddsCorrelationId()
    {
        // Arrange
        var logEvent = CreateLogEvent();
        var factory = new TestLogEventPropertyFactory();

        // Act
        _sut.Enrich(logEvent, factory);

        // Assert
        logEvent.Properties.Should().ContainKey("CorrelationId");
        logEvent.Properties.Should().NotContainKey("AggregateId");
        logEvent.Properties.Should().NotContainKey("TenantId");
        logEvent.Properties.Should().NotContainKey("EventType");
        logEvent.Properties.Should().NotContainKey("AggregateVersion");
    }

    [Fact]
    public void EventSourcingContext_BeginScope_ChangesCurrentAndDisposeRestoresIt()
    {
        // Arrange
        EventSourcingContext.Current = new EventSourcingContext { AggregateId = "outer" };

        // Act
        using (EventSourcingContext.BeginScope("inner-id", "tenant-x", "event-x", 9))
        {
            EventSourcingContext.Current.AggregateId.Should().Be("inner-id");
            EventSourcingContext.Current.TenantId.Should().Be("tenant-x");
            EventSourcingContext.Current.EventType.Should().Be("event-x");
            EventSourcingContext.Current.AggregateVersion.Should().Be(9);
        }

        // Assert
        EventSourcingContext.Current.AggregateId.Should().Be("outer");
    }

    [Fact]
    public void EventSourcingContext_Clear_ResetsContext()
    {
        // Arrange
        EventSourcingContext.Current = new EventSourcingContext
        {
            AggregateId = "agg",
            TenantId = "ten",
            EventType = "evt",
            AggregateVersion = 1,
        };

        // Act
        EventSourcingContext.Clear();

        // Assert
        EventSourcingContext.Current.AggregateId.Should().BeNull();
        EventSourcingContext.Current.TenantId.Should().BeNull();
        EventSourcingContext.Current.EventType.Should().BeNull();
        EventSourcingContext.Current.AggregateVersion.Should().BeNull();
    }

    private static LogEvent CreateLogEvent() => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Information,
        exception: null,
        new MessageTemplate("test", Array.Empty<MessageTemplateToken>()),
        Array.Empty<LogEventProperty>());

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
