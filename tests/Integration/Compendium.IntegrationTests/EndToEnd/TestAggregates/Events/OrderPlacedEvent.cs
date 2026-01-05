// -----------------------------------------------------------------------
// <copyright file="OrderPlacedEvent.cs" company="Compendium">
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
