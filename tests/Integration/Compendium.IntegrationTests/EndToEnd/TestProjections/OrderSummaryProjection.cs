// -----------------------------------------------------------------------
// <copyright file="OrderSummaryProjection.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Infrastructure.Projections;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.Events;

namespace Compendium.IntegrationTests.EndToEnd.TestProjections;

/// <summary>
/// Test projection for order summary data.
/// Demonstrates projection pattern for E2E scenarios.
/// </summary>
public sealed class OrderSummaryProjection :
    IProjection<OrderPlacedEvent>,
    IProjection<OrderLineAddedEvent>,
    IProjection<OrderCompletedEvent>
{
    private readonly ConcurrentDictionary<string, OrderSummaryDto> _summaries = new();

    public string ProjectionName => "E2E_OrderSummary";
    public int Version => 1;

    public Task ApplyAsync(OrderPlacedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        var summary = new OrderSummaryDto
        {
            OrderId = @event.OrderId.ToString(),
            CustomerId = @event.CustomerId,
            Status = "Created",
            LineCount = 0,
            TotalAmount = 0,
            CreatedAt = @event.CreatedAt,
            CompletedAt = null
        };

        _summaries[summary.OrderId] = summary;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(OrderLineAddedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_summaries.TryGetValue(@event.OrderId.ToString(), out var summary))
        {
            summary.LineCount++;
            summary.TotalAmount += @event.Quantity * @event.UnitPrice;
        }

        return Task.CompletedTask;
    }

    public Task ApplyAsync(OrderCompletedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_summaries.TryGetValue(@event.OrderId.ToString(), out var summary))
        {
            summary.Status = "Completed";
            summary.CompletedAt = @event.CompletedAt;
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _summaries.Clear();
        return Task.CompletedTask;
    }

    public OrderSummaryDto? GetOrderSummary(string orderId)
    {
        _summaries.TryGetValue(orderId, out var summary);
        return summary;
    }

    public IEnumerable<OrderSummaryDto> GetAllSummaries()
    {
        return _summaries.Values.OrderByDescending(s => s.CreatedAt);
    }

    public IEnumerable<OrderSummaryDto> GetCustomerOrders(string customerId)
    {
        return _summaries.Values
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt);
    }
}

/// <summary>
/// DTO representing order summary for queries.
/// </summary>
public sealed class OrderSummaryDto
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required string Status { get; set; }
    public required int LineCount { get; set; }
    public required decimal TotalAmount { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
}
