// -----------------------------------------------------------------------
// <copyright file="AggregateStatisticsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;

namespace Compendium.Abstractions.Tests.EventSourcing;

public class AggregateStatisticsTests
{
    [Fact]
    public void AggregateStatistics_DefaultConstruction_HasZeroCountAndNullDates()
    {
        // Arrange / Act
        var stats = new AggregateStatistics();

        // Assert
        stats.EventCount.Should().Be(0);
        stats.CurrentVersion.Should().Be(0L);
        stats.FirstEventDate.Should().BeNull();
        stats.LastEventDate.Should().BeNull();
    }

    [Fact]
    public void AggregateStatistics_SetEventCount_PersistsValue()
    {
        // Arrange
        var stats = new AggregateStatistics();

        // Act
        stats.EventCount = 7;

        // Assert
        stats.EventCount.Should().Be(7);
    }

    [Fact]
    public void AggregateStatistics_SetCurrentVersion_PersistsValue()
    {
        // Arrange
        var stats = new AggregateStatistics();

        // Act
        stats.CurrentVersion = 99L;

        // Assert
        stats.CurrentVersion.Should().Be(99L);
    }

    [Fact]
    public void AggregateStatistics_SetFirstAndLastEventDate_PersistValues()
    {
        // Arrange
        var stats = new AggregateStatistics();
        var first = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var last = DateTimeOffset.Parse("2026-05-10T12:00:00Z");

        // Act
        stats.FirstEventDate = first;
        stats.LastEventDate = last;

        // Assert
        stats.FirstEventDate.Should().Be(first);
        stats.LastEventDate.Should().Be(last);
    }

    [Fact]
    public void AggregateStatistics_NullableDates_AcceptNull()
    {
        // Arrange
        var stats = new AggregateStatistics
        {
            FirstEventDate = DateTimeOffset.UtcNow,
            LastEventDate = DateTimeOffset.UtcNow,
        };

        // Act
        stats.FirstEventDate = null;
        stats.LastEventDate = null;

        // Assert
        stats.FirstEventDate.Should().BeNull();
        stats.LastEventDate.Should().BeNull();
    }
}
