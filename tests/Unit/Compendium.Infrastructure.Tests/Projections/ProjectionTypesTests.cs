// -----------------------------------------------------------------------
// <copyright file="ProjectionTypesTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Projections;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Tests for simple projection record/POCO types whose coverage was 0%
/// (constructors, init-only properties, default values, derived metrics).
/// </summary>
public sealed class ProjectionTypesTests
{
    [Fact]
    public void RebuildProgress_PercentComplete_NoEvents_IsZero()
    {
        // Arrange
        var progress = new RebuildProgress { TotalEvents = 0, ProcessedEvents = 0 };

        // Act / Assert
        progress.PercentComplete.Should().Be(0);
    }

    [Fact]
    public void RebuildProgress_PercentComplete_PartiallyDone_ReturnsPercentage()
    {
        // Arrange
        var progress = new RebuildProgress { TotalEvents = 200, ProcessedEvents = 50 };

        // Act / Assert
        progress.PercentComplete.Should().Be(25);
    }

    [Fact]
    public void RebuildProgress_DefaultsAreSensible()
    {
        // Arrange
        var progress = new RebuildProgress();

        // Act / Assert
        progress.ProjectionName.Should().Be(string.Empty);
        progress.TotalEvents.Should().Be(0);
        progress.ProcessedEvents.Should().Be(0);
        progress.ElapsedTime.Should().Be(TimeSpan.Zero);
        progress.EventsPerSecond.Should().Be(0);
        progress.EstimatedTimeRemaining.Should().Be(TimeSpan.Zero);
        progress.CurrentBatch.Should().Be(0);
        progress.BatchSize.Should().Be(0);
    }

    [Fact]
    public void EventData_DefaultsAreSensible()
    {
        // Arrange
        var ev = new EventData();

        // Act / Assert
        ev.EventId.Should().Be(Guid.Empty);
        ev.StreamId.Should().Be(string.Empty);
        ev.StreamPosition.Should().Be(0);
        ev.GlobalPosition.Should().Be(0);
        ev.EventType.Should().Be(string.Empty);
        ev.EventDataJson.Should().Be(string.Empty);
        ev.UserId.Should().BeNull();
        ev.TenantId.Should().BeNull();
        ev.Headers.Should().BeNull();
    }

    [Fact]
    public void EventData_SettersThroughInitializers_StoreValues()
    {
        // Arrange / Act
        var ev = new EventData
        {
            EventId = Guid.NewGuid(),
            StreamId = "stream-1",
            StreamPosition = 1,
            GlobalPosition = 100,
            EventType = "Created",
            EventDataJson = "{}",
            Timestamp = DateTime.UtcNow,
            UserId = "u-1",
            TenantId = "t-1",
            Headers = new Dictionary<string, object> { ["k"] = "v" },
        };

        // Assert
        ev.StreamId.Should().Be("stream-1");
        ev.GlobalPosition.Should().Be(100);
        ev.Headers.Should().ContainKey("k");
    }

    [Fact]
    public void ProjectionState_DefaultsAreSensible()
    {
        // Arrange
        var state = new ProjectionState();

        // Act / Assert
        state.ProjectionName.Should().Be(string.Empty);
        state.Status.Should().Be(ProjectionStatus.Idle);
        state.LastProcessedPosition.Should().Be(0);
        state.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ProjectionState_AllStatusesAreReachable()
    {
        // Arrange / Act / Assert
        Enum.GetValues<ProjectionStatus>()
            .Should()
            .Contain(new[]
            {
                ProjectionStatus.Idle,
                ProjectionStatus.Building,
                ProjectionStatus.Rebuilding,
                ProjectionStatus.Paused,
                ProjectionStatus.Failed,
                ProjectionStatus.Completed,
            });
    }

    [Fact]
    public void EventMetadata_StoresAllFields()
    {
        // Arrange
        var ts = DateTime.UtcNow;
        var headers = new Dictionary<string, object> { ["h"] = 1 };

        // Act
        var metadata = new EventMetadata("s", 1, 2, ts, "u", "t", headers);

        // Assert
        metadata.StreamId.Should().Be("s");
        metadata.StreamPosition.Should().Be(1);
        metadata.GlobalPosition.Should().Be(2);
        metadata.Timestamp.Should().Be(ts);
        metadata.UserId.Should().Be("u");
        metadata.TenantId.Should().Be("t");
        metadata.Headers.Should().BeSameAs(headers);
    }
}
