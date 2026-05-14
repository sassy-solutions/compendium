// -----------------------------------------------------------------------
// <copyright file="OrderProjectionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Projections;
using Compendium.Infrastructure.Projections.Examples;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Unit tests for the example projections that ship with Compendium.
/// </summary>
public sealed class OrderProjectionsTests
{
    private static readonly EventMetadata Metadata = new(
        "stream-1", 1, 1, DateTime.UtcNow, "user-1", "tenant-1", null);

    [Fact]
    public void OrderSummaryProjection_HasNameAndVersion()
    {
        // Arrange
        var sut = new OrderSummaryProjection();

        // Act / Assert
        sut.ProjectionName.Should().Be("OrderSummary");
        sut.Version.Should().Be(1);
    }

    [Fact]
    public async Task OrderSummary_OrderPlaced_AddsSummary()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var evt = new OrderPlacedEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            Total = 99.99m,
            Items = new List<OrderItem> { new() { ProductId = Guid.NewGuid(), Quantity = 2, Price = 50m } },
        };

        // Act
        await sut.ApplyAsync(evt, Metadata);

        // Assert
        sut.Summaries.Should().ContainKey(orderId);
        var summary = sut.Summaries[orderId];
        summary.Status.Should().Be(OrderStatus.Placed);
        summary.Total.Should().Be(99.99m);
        summary.ItemCount.Should().Be(1);
        summary.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public async Task OrderSummary_OrderPlaced_WithoutItems_DefaultsItemCountToZero()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var orderId = Guid.NewGuid();
        var evt = new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid(), Total = 0m };

        // Act
        await sut.ApplyAsync(evt, Metadata);

        // Assert
        sut.Summaries[orderId].ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task OrderSummary_OrderShipped_UpdatesExistingSummary()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var orderId = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);

        // Act
        await sut.ApplyAsync(
            new OrderShippedEvent { OrderId = orderId, TrackingNumber = "TRK-1" },
            Metadata);

        // Assert
        var summary = sut.Summaries[orderId];
        summary.Status.Should().Be(OrderStatus.Shipped);
        summary.TrackingNumber.Should().Be("TRK-1");
        summary.ShippedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task OrderSummary_OrderShipped_UnknownOrder_DoesNothing()
    {
        // Arrange
        var sut = new OrderSummaryProjection();

        // Act
        await sut.ApplyAsync(
            new OrderShippedEvent { OrderId = Guid.NewGuid(), TrackingNumber = "TRK" },
            Metadata);

        // Assert
        sut.Summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderSummary_OrderCancelled_UpdatesExistingSummary()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var orderId = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);

        // Act
        await sut.ApplyAsync(
            new OrderCancelledEvent { OrderId = orderId, Reason = "fraud" },
            Metadata);

        // Assert
        var summary = sut.Summaries[orderId];
        summary.Status.Should().Be(OrderStatus.Cancelled);
        summary.CancellationReason.Should().Be("fraud");
        summary.CancelledAt.Should().NotBe(default);
    }

    [Fact]
    public async Task OrderSummary_OrderCancelled_UnknownOrder_DoesNothing()
    {
        // Arrange
        var sut = new OrderSummaryProjection();

        // Act
        await sut.ApplyAsync(
            new OrderCancelledEvent { OrderId = Guid.NewGuid(), Reason = "no" },
            Metadata);

        // Assert
        sut.Summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderSummary_ResetAsync_ClearsSummaries()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);

        // Act
        await sut.ResetAsync();

        // Assert
        sut.Summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderSummary_GetOrdersByCustomer_FiltersCorrectly()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var customer = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = customer, Total = 1m }, Metadata);
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);

        // Act
        var orders = sut.GetOrdersByCustomer(customer);

        // Assert
        orders.Should().HaveCount(1);
        orders.Single().CustomerId.Should().Be(customer);
    }

    [Fact]
    public async Task OrderSummary_GetOrdersByStatus_FiltersCorrectly()
    {
        // Arrange
        var sut = new OrderSummaryProjection();
        var orderId = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = orderId, CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);
        await sut.ApplyAsync(new OrderShippedEvent { OrderId = orderId, TrackingNumber = "T" }, Metadata);

        // Act
        var shipped = sut.GetOrdersByStatus(OrderStatus.Shipped);
        var placed = sut.GetOrdersByStatus(OrderStatus.Placed);

        // Assert
        shipped.Should().HaveCount(1);
        placed.Should().HaveCount(1);
    }

    [Fact]
    public void CustomerStatsProjection_HasNameAndVersion()
    {
        // Arrange
        var sut = new CustomerStatsProjection();

        // Act / Assert
        sut.ProjectionName.Should().Be("CustomerStats");
        sut.Version.Should().Be(1);
    }

    [Fact]
    public async Task CustomerStats_OrderPlaced_FirstTime_CreatesAndUpdatesStats()
    {
        // Arrange
        var sut = new CustomerStatsProjection();
        var customerId = Guid.NewGuid();
        var evt = new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = customerId, Total = 100m };

        // Act
        await sut.ApplyAsync(evt, Metadata);

        // Assert
        sut.Stats.Should().ContainKey(customerId);
        var stats = sut.Stats[customerId];
        stats.TotalOrders.Should().Be(1);
        stats.TotalSpent.Should().Be(100m);
        stats.AverageOrderValue.Should().Be(100m);
    }

    [Fact]
    public async Task CustomerStats_OrderPlaced_MultipleOrders_AggregatesCorrectly()
    {
        // Arrange
        var sut = new CustomerStatsProjection();
        var customerId = Guid.NewGuid();

        // Act
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = customerId, Total = 100m }, Metadata);
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = customerId, Total = 200m }, Metadata);

        // Assert
        var stats = sut.Stats[customerId];
        stats.TotalOrders.Should().Be(2);
        stats.TotalSpent.Should().Be(300m);
        stats.AverageOrderValue.Should().Be(150m);
    }

    [Fact]
    public async Task CustomerStats_OrderShipped_IncrementsShippedOrders()
    {
        // Arrange
        var sut = new CustomerStatsProjection();
        var customerId = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = customerId, Total = 1m }, Metadata);

        // Act
        await sut.ApplyAsync(new OrderShippedEvent { OrderId = Guid.NewGuid(), TrackingNumber = "T" }, Metadata);

        // Assert
        sut.Stats[customerId].ShippedOrders.Should().Be(1);
    }

    [Fact]
    public async Task CustomerStats_ResetAsync_ClearsStats()
    {
        // Arrange
        var sut = new CustomerStatsProjection();
        await sut.ApplyAsync(new OrderPlacedEvent { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Total = 1m }, Metadata);

        // Act
        await sut.ResetAsync();

        // Assert
        sut.Stats.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomerStats_GetTopCustomers_OrdersByTotalSpentDescending()
    {
        // Arrange
        var sut = new CustomerStatsProjection();
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var c3 = Guid.NewGuid();
        await sut.ApplyAsync(new OrderPlacedEvent { CustomerId = c1, Total = 100m }, Metadata);
        await sut.ApplyAsync(new OrderPlacedEvent { CustomerId = c2, Total = 200m }, Metadata);
        await sut.ApplyAsync(new OrderPlacedEvent { CustomerId = c3, Total = 50m }, Metadata);

        // Act
        var top = sut.GetTopCustomers(2).ToList();

        // Assert
        top.Should().HaveCount(2);
        top[0].CustomerId.Should().Be(c2);
        top[1].CustomerId.Should().Be(c1);
    }

    [Fact]
    public void OrderEventBase_OccurredOn_AndAggregateInfo_AreInitialized()
    {
        // Arrange
        var placed = new OrderPlacedEvent();
        var shipped = new OrderShippedEvent();
        var cancelled = new OrderCancelledEvent();

        // Act / Assert
        placed.AggregateType.Should().Be("Order");
        shipped.AggregateType.Should().Be("Order");
        cancelled.AggregateType.Should().Be("Order");
        placed.EventId.Should().NotBe(Guid.Empty);
        placed.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
