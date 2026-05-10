// -----------------------------------------------------------------------
// <copyright file="InMemoryEventStoreSupplementaryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Supplementary tests for <see cref="InMemoryEventStore"/> covering corner cases not exercised
/// by the existing test suite (last-event lookup, statistics, empty state behaviours).
/// </summary>
public sealed class InMemoryEventStoreSupplementaryTests
{
    private readonly IEventDeserializer _deserializer;
    private readonly InMemoryEventStore _sut;

    public InMemoryEventStoreSupplementaryTests()
    {
        // Arrange
        _deserializer = new SecureEventDeserializer(new EventTypeRegistry());
        _sut = new InMemoryEventStore(_deserializer, NullLogger<InMemoryEventStore>.Instance);
    }

    [Fact]
    public void Ctor_NullDeserializer_Throws()
    {
        // Arrange / Act
        var act = () => new InMemoryEventStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLastEventAsync_NoEvents_ReturnsNotFound()
    {
        // Arrange / Act
        var result = await _sut.GetLastEventAsync("missing");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventStore.NoEvents");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLastEventAsync_EmptyAggId_ReturnsValidationFailure(string invalid)
    {
        // Arrange / Act
        var result = await _sut.GetLastEventAsync(invalid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetStatisticsAsync_NoEvents_ReturnsZeroes()
    {
        // Arrange / Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAggregates.Should().Be(0);
        result.Value.TotalEvents.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventsAsync_EmptyAggId_ReturnsValidationFailure(string invalid)
    {
        // Arrange / Act
        var result = await _sut.GetEventsAsync(invalid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsAsync_UnknownAggregate_ReturnsEmpty()
    {
        // Arrange / Act
        var result = await _sut.GetEventsAsync("unknown");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
