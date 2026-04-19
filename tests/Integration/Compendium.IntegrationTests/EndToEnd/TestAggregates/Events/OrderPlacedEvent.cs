// -----------------------------------------------------------------------
// <copyright file="OrderPlacedEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates.Events;

/// <summary>
/// Domain event raised when an order is placed.
/// </summary>
public sealed record OrderPlacedEvent(
    OrderId OrderId,
    string CustomerId,
    DateTimeOffset CreatedAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string AggregateId { get; init; } = OrderId.ToString();
    public string AggregateType { get; init; } = nameof(TestAggregates.OrderAggregate);
    public DateTimeOffset OccurredOn { get; init; } = CreatedAt;
    public long AggregateVersion { get; init; } = 1;
    public int EventVersion { get; init; } = 1;
}
