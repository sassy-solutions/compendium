// -----------------------------------------------------------------------
// <copyright file="EventStoreStatisticsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;

namespace Compendium.Abstractions.Tests.EventSourcing;

public class EventStoreStatisticsTests
{
    [Fact]
    public void EventStoreStatistics_DefaultConstruction_InitializesEmptyCollectionsAndZeroes()
    {
        // Arrange / Act
        var stats = new EventStoreStatistics();

        // Assert
        stats.TotalAggregates.Should().Be(0);
        stats.TotalEvents.Should().Be(0L);
        stats.AggregateStatistics.Should().NotBeNull();
        stats.AggregateStatistics.Should().BeEmpty();
    }

    [Fact]
    public void EventStoreStatistics_SetTotalAggregates_PersistsValue()
    {
        // Arrange
        var stats = new EventStoreStatistics();

        // Act
        stats.TotalAggregates = 42;

        // Assert
        stats.TotalAggregates.Should().Be(42);
    }

    [Fact]
    public void EventStoreStatistics_SetTotalEvents_PersistsValue()
    {
        // Arrange
        var stats = new EventStoreStatistics();

        // Act
        stats.TotalEvents = 1_000_000L;

        // Assert
        stats.TotalEvents.Should().Be(1_000_000L);
    }

    [Fact]
    public void EventStoreStatistics_SetAggregateStatistics_PersistsCustomDictionary()
    {
        // Arrange
        var stats = new EventStoreStatistics();
        var dict = new Dictionary<string, AggregateStatistics>
        {
            ["agg-1"] = new AggregateStatistics { EventCount = 3, CurrentVersion = 3 },
        };

        // Act
        stats.AggregateStatistics = dict;

        // Assert
        stats.AggregateStatistics.Should().BeSameAs(dict);
        stats.AggregateStatistics.Should().ContainKey("agg-1");
        stats.AggregateStatistics["agg-1"].EventCount.Should().Be(3);
    }

    [Fact]
    public void EventStoreStatistics_AddAggregateStatistics_AppendsEntry()
    {
        // Arrange
        var stats = new EventStoreStatistics();

        // Act
        stats.AggregateStatistics.Add("agg-A", new AggregateStatistics { EventCount = 10 });
        stats.AggregateStatistics.Add("agg-B", new AggregateStatistics { EventCount = 20 });

        // Assert
        stats.AggregateStatistics.Should().HaveCount(2);
        stats.AggregateStatistics["agg-A"].EventCount.Should().Be(10);
        stats.AggregateStatistics["agg-B"].EventCount.Should().Be(20);
    }
}
