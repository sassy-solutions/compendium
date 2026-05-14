// -----------------------------------------------------------------------
// <copyright file="DomainEventBaseJsonConstructorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compendium.Core.Tests.Domain.Events;

/// <summary>
/// Verifies that <see cref="DomainEventBase"/> can be deserialized via its
/// <see cref="JsonConstructorAttribute"/>-decorated constructor — exercising the
/// restoration path for AggregateId / AggregateType / OccurredOn / AggregateVersion / EventVersion.
/// </summary>
public class DomainEventBaseJsonConstructorTests
{
    /// <summary>
    /// Concrete derived event whose own constructor delegates to the JSON constructor of
    /// <see cref="DomainEventBase"/>. We expose both constructors so that
    /// System.Text.Json picks the JSON one when deserialising.
    /// </summary>
    private sealed class JsonRoundTripEvent : DomainEventBase
    {
        public JsonRoundTripEvent(string aggregateId, string aggregateType, long aggregateVersion, string payload)
            : base(aggregateId, aggregateType, aggregateVersion)
        {
            Payload = payload;
        }

        [JsonConstructor]
        public JsonRoundTripEvent(
            Guid eventId,
            string aggregateId,
            string aggregateType,
            DateTimeOffset occurredOn,
            long aggregateVersion,
            int eventVersion,
            string payload)
            : base(eventId, aggregateId, aggregateType, occurredOn, aggregateVersion, eventVersion)
        {
            Payload = payload;
        }

        public string Payload { get; }
    }

    [Fact]
    public void DomainEventBase_JsonConstructor_RestoresAllProperties()
    {
        // Arrange
        var original = new JsonRoundTripEvent("agg-1", "Test", 42, "hello");
        var json = JsonSerializer.Serialize(original);

        // Act
        var restored = JsonSerializer.Deserialize<JsonRoundTripEvent>(json);

        // Assert
        restored.Should().NotBeNull();
        restored!.EventId.Should().Be(original.EventId);
        restored.AggregateId.Should().Be(original.AggregateId);
        restored.AggregateType.Should().Be(original.AggregateType);
        restored.AggregateVersion.Should().Be(original.AggregateVersion);
        restored.EventVersion.Should().Be(original.EventVersion);
        restored.OccurredOn.Should().Be(original.OccurredOn);
        restored.Payload.Should().Be("hello");
    }

    [Fact]
    public void DomainEventBase_JsonConstructor_AllowsCustomEventVersion()
    {
        // Arrange
        var original = new JsonRoundTripEvent(
            eventId: Guid.NewGuid(),
            aggregateId: "agg-1",
            aggregateType: "Test",
            occurredOn: DateTimeOffset.Parse("2026-05-10T00:00:00Z"),
            aggregateVersion: 7,
            eventVersion: 3,
            payload: "x");

        // Act / Assert
        original.EventVersion.Should().Be(3);
        original.AggregateVersion.Should().Be(7);
        original.AggregateId.Should().Be("agg-1");
    }
}
