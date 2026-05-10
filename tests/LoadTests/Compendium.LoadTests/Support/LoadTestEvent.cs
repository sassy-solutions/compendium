// -----------------------------------------------------------------------
// <copyright file="LoadTestEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;

namespace Compendium.LoadTests.Support;

/// <summary>
/// Minimal domain event used as payload across all load-test scenarios.
/// Kept stable so registry / deserializer setup is identical between scenarios.
/// </summary>
public sealed record LoadTestEvent : IDomainEvent
{
    /// <inheritdoc />
    public required Guid EventId { get; init; }

    /// <inheritdoc />
    public required string AggregateId { get; init; }

    /// <inheritdoc />
    public required string AggregateType { get; init; }

    /// <inheritdoc />
    public required DateTimeOffset OccurredOn { get; init; }

    /// <inheritdoc />
    public required long AggregateVersion { get; init; }

    /// <inheritdoc />
    public int EventVersion { get; init; } = 1;

    /// <summary>
    /// Inline payload used to give each event a non-trivial size, similar to
    /// what a real domain event would carry.
    /// </summary>
    public required string Payload { get; init; }

    /// <summary>
    /// Builds a single deterministic event for a given aggregate / version slot.
    /// </summary>
    public static LoadTestEvent Create(string aggregateId, long version)
    {
        return new LoadTestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = aggregateId,
            AggregateType = "LoadTestAggregate",
            OccurredOn = DateTimeOffset.UtcNow,
            AggregateVersion = version,
            Payload = $"payload-{version}-{Guid.NewGuid():N}",
        };
    }

    /// <summary>
    /// Builds a contiguous batch of events for an aggregate, starting at version 1.
    /// </summary>
    public static List<IDomainEvent> Batch(string aggregateId, int count, long startVersion = 1)
    {
        var list = new List<IDomainEvent>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(Create(aggregateId, startVersion + i));
        }

        return list;
    }
}
