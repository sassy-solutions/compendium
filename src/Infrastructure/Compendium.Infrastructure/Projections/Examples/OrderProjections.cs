// -----------------------------------------------------------------------
// <copyright file="OrderProjections.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Compendium.Infrastructure.Projections.Examples;

/// <summary>
/// Example projection that maintains order summaries for quick lookups.
/// Demonstrates how to build read models from domain events.
/// </summary>
public class OrderSummaryProjection : IProjection<OrderPlacedEvent>, IProjection<OrderShippedEvent>, IProjection<OrderCancelledEvent>
{
    private readonly Dictionary<Guid, OrderSummary> _summaries = new();

    [JsonInclude]
    public string ProjectionName => "OrderSummary";

    [JsonInclude]
    public int Version => 1;

    [JsonInclude]
    public IReadOnlyDictionary<Guid, OrderSummary> Summaries => _summaries;

    public Task ApplyAsync(OrderPlacedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        _summaries[@event.OrderId] = new OrderSummary
        {
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            Total = @event.Total,
            Status = OrderStatus.Placed,
            PlacedAt = @event.OccurredOn.DateTime,
            TenantId = metadata.TenantId,
            ItemCount = @event.Items?.Count ?? 0
        };

        return Task.CompletedTask;
    }

    public Task ApplyAsync(OrderShippedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_summaries.TryGetValue(@event.OrderId, out var summary))
        {
            summary.Status = OrderStatus.Shipped;
            summary.ShippedAt = @event.OccurredOn.DateTime;
            summary.TrackingNumber = @event.TrackingNumber;
        }

        return Task.CompletedTask;
    }

    public Task ApplyAsync(OrderCancelledEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_summaries.TryGetValue(@event.OrderId, out var summary))
        {
            summary.Status = OrderStatus.Cancelled;
            summary.CancelledAt = @event.OccurredOn.DateTime;
            summary.CancellationReason = @event.Reason;
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _summaries.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets orders by customer ID.
    /// </summary>
    public IEnumerable<OrderSummary> GetOrdersByCustomer(Guid customerId)
    {
        return _summaries.Values.Where(o => o.CustomerId == customerId);
    }

    /// <summary>
    /// Gets orders by status.
    /// </summary>
    public IEnumerable<OrderSummary> GetOrdersByStatus(OrderStatus status)
    {
        return _summaries.Values.Where(o => o.Status == status);
    }
}

/// <summary>
/// Example projection that tracks customer statistics.
/// Demonstrates aggregation and analytics capabilities.
/// </summary>
public class CustomerStatsProjection : IProjection<OrderPlacedEvent>, IProjection<OrderShippedEvent>
{
    private readonly Dictionary<Guid, CustomerStats> _stats = new();

    [JsonInclude]
    public string ProjectionName => "CustomerStats";

    [JsonInclude]
    public int Version => 1;

    [JsonInclude]
    public IReadOnlyDictionary<Guid, CustomerStats> Stats => _stats;

    public Task ApplyAsync(OrderPlacedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (!_stats.ContainsKey(@event.CustomerId))
        {
            _stats[@event.CustomerId] = new CustomerStats
            {
                CustomerId = @event.CustomerId,
                FirstOrderDate = @event.OccurredOn.DateTime
            };
        }

        var stats = _stats[@event.CustomerId];
        stats.TotalOrders++;
        stats.TotalSpent += @event.Total;
        stats.LastOrderDate = @event.OccurredOn.DateTime;
        stats.AverageOrderValue = stats.TotalSpent / stats.TotalOrders;

        return Task.CompletedTask;
    }

    public Task ApplyAsync(OrderShippedEvent @event, EventMetadata metadata, CancellationToken cancellationToken = default)
    {
        // Update shipped order count if we have the customer info from order placed
        var orderCustomer = _stats.Values.FirstOrDefault(s =>
            _stats.ContainsKey(s.CustomerId)); // This is a simplified lookup

        if (orderCustomer != null)
        {
            orderCustomer.ShippedOrders++;
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _stats.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets top customers by total spent.
    /// </summary>
    public IEnumerable<CustomerStats> GetTopCustomers(int count = 10)
    {
        return _stats.Values
            .OrderByDescending(s => s.TotalSpent)
            .Take(count);
    }
}

// Example domain events
public class OrderPlacedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string AggregateId { get; init; } = string.Empty;
    public string AggregateType { get; init; } = "Order";
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;

    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Total { get; init; }
    public List<OrderItem>? Items { get; init; }
}

public class OrderShippedEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string AggregateId { get; init; } = string.Empty;
    public string AggregateType { get; init; } = "Order";
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;

    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime ShippedDate { get; init; }
}

public class OrderCancelledEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string AggregateId { get; init; } = string.Empty;
    public string AggregateType { get; init; } = "Order";
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public long AggregateVersion { get; init; }
    public int EventVersion { get; init; } = 1;

    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

// Supporting data structures
public class OrderSummary
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CancellationReason { get; set; }
    public string? TenantId { get; set; }
    public int ItemCount { get; set; }
}

public class CustomerStats
{
    public Guid CustomerId { get; set; }
    public int TotalOrders { get; set; }
    public int ShippedOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime FirstOrderDate { get; set; }
    public DateTime LastOrderDate { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus
{
    Placed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
