// -----------------------------------------------------------------------
// <copyright file="OrderLineAddedEvent.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
