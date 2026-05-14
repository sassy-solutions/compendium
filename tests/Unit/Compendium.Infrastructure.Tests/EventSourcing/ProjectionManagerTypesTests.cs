// -----------------------------------------------------------------------
// <copyright file="ProjectionManagerTypesTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Tests for the simple POCO/record types in the Compendium.Infrastructure.EventSourcing namespace
/// (ProjectionDetails, ProjectionManagerStatistics, ProjectionRebuildProgress).
/// </summary>
public sealed class ProjectionManagerTypesTests
{
    [Fact]
    public void ProjectionDetails_Defaults_AreSensible()
    {
        // Arrange / Act
        var details = new ProjectionDetails();

        // Assert
        details.ProjectionId.Should().Be(string.Empty);
        details.LastProcessedVersion.Should().Be(0);
        details.LastUpdated.Should().Be(default);
        details.IsRebuilding.Should().BeFalse();
    }

    [Fact]
    public void ProjectionDetails_StoresInitializerValues()
    {
        // Arrange
        var ts = DateTimeOffset.UtcNow;

        // Act
        var details = new ProjectionDetails
        {
            ProjectionId = "p1",
            LastProcessedVersion = 7,
            LastUpdated = ts,
            IsRebuilding = true,
        };

        // Assert
        details.ProjectionId.Should().Be("p1");
        details.LastProcessedVersion.Should().Be(7);
        details.LastUpdated.Should().Be(ts);
        details.IsRebuilding.Should().BeTrue();
    }

    [Fact]
    public void ProjectionManagerStatistics_Defaults_AreSensible()
    {
        // Arrange / Act
        var stats = new ProjectionManagerStatistics();

        // Assert
        stats.TotalProjections.Should().Be(0);
        stats.ActiveProjections.Should().Be(0);
        stats.RebuildingProjections.Should().Be(0);
        stats.ProjectionDetails.Should().BeEmpty();
    }

    [Fact]
    public void ProjectionManagerStatistics_StoresInitializerValues()
    {
        // Arrange
        var details = new Dictionary<string, ProjectionDetails> { ["p1"] = new ProjectionDetails() };

        // Act
        var stats = new ProjectionManagerStatistics
        {
            TotalProjections = 3,
            ActiveProjections = 2,
            RebuildingProjections = 1,
            ProjectionDetails = details,
        };

        // Assert
        stats.TotalProjections.Should().Be(3);
        stats.ActiveProjections.Should().Be(2);
        stats.RebuildingProjections.Should().Be(1);
        stats.ProjectionDetails.Should().BeSameAs(details);
    }

    [Fact]
    public void ProjectionRebuildProgress_StoresAllValues()
    {
        // Arrange / Act
        var progress = new ProjectionRebuildProgress("pid", 50, 100, 50.0, false, 25.5);

        // Assert
        progress.ProjectionId.Should().Be("pid");
        progress.ProcessedEvents.Should().Be(50);
        progress.TotalEvents.Should().Be(100);
        progress.PercentComplete.Should().Be(50.0);
        progress.IsCompleted.Should().BeFalse();
        progress.EventsPerSecond.Should().Be(25.5);
    }

    [Fact]
    public void ProjectionRebuildProgress_DefaultEventsPerSecond_IsZero()
    {
        // Arrange / Act
        var progress = new ProjectionRebuildProgress("pid", 0, 0, 0, true);

        // Assert
        progress.EventsPerSecond.Should().Be(0.0);
    }
}
