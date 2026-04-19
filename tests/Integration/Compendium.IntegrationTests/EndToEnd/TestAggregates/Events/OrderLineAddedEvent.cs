// -----------------------------------------------------------------------
// <copyright file="OrderLineAddedEvent.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates.Events;

/// <summary>
/// Domain event raised when a line is added to an order.
/// </summary>
public sealed record OrderLineAddedEvent(
    OrderId OrderId,
    string LineId,
    string ProductId,
    int Quantity,
    decimal UnitPrice) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string AggregateId { get; init; } = OrderId.ToString();
    public string AggregateType { get; init; } = nameof(TestAggregates.OrderAggregate);
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;
}
