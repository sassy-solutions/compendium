// -----------------------------------------------------------------------
// <copyright file="LoggingExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Observability.Logging;

namespace Compendium.Infrastructure.Tests.Observability.Logging;

/// <summary>
/// Unit tests for <see cref="LoggingExtensions"/> helpers that wrap log calls
/// with <see cref="EventSourcingContext"/> scopes.
/// </summary>
public sealed class LoggingExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    public LoggingExtensionsTests()
    {
        EventSourcingContext.Clear();
    }

    [Fact]
    public void BeginEventSourcingScope_WithValidArgs_ReturnsScopeAndPopulatesContext()
    {
        // Arrange / Act
        using (_logger.BeginEventSourcingScope("agg-1", "tenant-1", "EventX", 3))
        {
            // Assert (inside scope)
            EventSourcingContext.Current.AggregateId.Should().Be("agg-1");
            EventSourcingContext.Current.TenantId.Should().Be("tenant-1");
            EventSourcingContext.Current.EventType.Should().Be("EventX");
            EventSourcingContext.Current.AggregateVersion.Should().Be(3);
        }

        // Assert (outside scope) — restored
        EventSourcingContext.Current.AggregateId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void BeginEventSourcingScope_WithEmptyAggregateId_Throws(string? invalid)
    {
        // Arrange / Act
        var act = () => _logger.BeginEventSourcingScope(invalid!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogEventAppended_WithValidArgs_LogsInformation()
    {
        // Arrange / Act
        _logger.LogEventAppended("agg-1", "Created", 1, "tenant-1");

        // Assert
        _logger.ReceivedCalls()
            .Any(c => c.GetMethodInfo().Name == nameof(ILogger.Log))
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("", "evt")]
    [InlineData("agg", "")]
    public void LogEventAppended_WithEmptyArgs_Throws(string aggId, string eventType)
    {
        // Arrange / Act
        var act = () => _logger.LogEventAppended(aggId, eventType, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogEventsLoaded_WithToVersion_LogsRange()
    {
        // Arrange / Act
        _logger.LogEventsLoaded("agg-1", 1, 10, "tenant-1");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogEventsLoaded_WithoutToVersion_LogsFromOnly()
    {
        // Arrange / Act
        _logger.LogEventsLoaded("agg-1", 1);

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void LogEventsLoaded_EmptyAggId_Throws(string? invalid)
    {
        // Arrange / Act
        var act = () => _logger.LogEventsLoaded(invalid!, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogProjectionRebuildStarted_WithValidArgs_Logs()
    {
        // Arrange / Act
        _logger.LogProjectionRebuildStarted("Projection-A", 0);

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogProjectionRebuildStarted_EmptyName_Throws()
    {
        // Arrange / Act
        var act = () => _logger.LogProjectionRebuildStarted("", 0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogProjectionRebuildCompleted_WithValidArgs_Logs()
    {
        // Arrange / Act
        _logger.LogProjectionRebuildCompleted("Projection-A", 100, 1500);

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogProjectionRebuildCompleted_EmptyName_Throws()
    {
        // Arrange / Act
        var act = () => _logger.LogProjectionRebuildCompleted("", 1, 2);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogConcurrencyConflict_WithValidArgs_LogsWarning()
    {
        // Arrange / Act
        _logger.LogConcurrencyConflict("agg-1", 1, 2, "tenant-1");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogConcurrencyConflict_EmptyAggId_Throws()
    {
        // Arrange / Act
        var act = () => _logger.LogConcurrencyConflict("", 1, 2);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogSnapshotSaved_WithValidArgs_Logs()
    {
        // Arrange / Act
        _logger.LogSnapshotSaved("agg-1", 5, "tenant-1");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogSnapshotSaved_EmptyAggId_Throws()
    {
        // Arrange / Act
        var act = () => _logger.LogSnapshotSaved("", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LogSnapshotLoaded_WithValidArgs_Logs()
    {
        // Arrange / Act
        _logger.LogSnapshotLoaded("agg-1", 5, "tenant-1");

        // Assert
        _logger.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public void LogSnapshotLoaded_EmptyAggId_Throws()
    {
        // Arrange / Act
        var act = () => _logger.LogSnapshotLoaded("", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
