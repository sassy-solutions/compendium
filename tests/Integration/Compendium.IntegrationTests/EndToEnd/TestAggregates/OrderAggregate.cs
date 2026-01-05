// -----------------------------------------------------------------------
// <copyright file="OrderAggregate.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Results;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.Events;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates;

/// <summary>
/// Test aggregate representing an order in the e-commerce E2E scenarios.
/// Demonstrates event sourcing, domain events, and business rule enforcement.
/// </summary>
public sealed class OrderAggregate : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _orderLines = [];

    private OrderAggregate(OrderId id) : base(id)
    {
        // Private constructor for reconstitution from events
    }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the order status.
    /// </summary>
    public OrderStatus Status { get; private set; } = OrderStatus.Created;

    /// <summary>
    /// Gets the order lines.
    /// </summary>
    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public new DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the completion timestamp.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the total order value.
    /// </summary>
    public decimal TotalAmount => _orderLines.Sum(line => line.TotalPrice);

    /// <summary>
    /// Places a new order.
    /// </summary>
    public static OrderAggregate PlaceOrder(
        OrderId orderId,
        string customerId,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var order = new OrderAggregate(orderId);
        var @event = new OrderPlacedEvent(orderId, customerId, createdAt);
        order.AddDomainEvent(@event);
        order.Apply(@event);

        return order;
    }

    /// <summary>
    /// Adds a line to the order.
    /// </summary>
    public Result AddOrderLine(
        string lineId,
        string productId,
        int quantity,
        decimal unitPrice)
    {
        // Business rule: Cannot add lines to completed orders
        if (Status == OrderStatus.Completed)
        {
            return Result.Failure(
                Error.Validation("Order.AddLine.Completed", "Cannot add lines to a completed order"));
        }

        // Business rule: Quantity must be positive
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("Order.AddLine.InvalidQuantity", "Quantity must be greater than zero"));
        }

        // Business rule: Unit price must be non-negative
        if (unitPrice < 0)
        {
            return Result.Failure(
                Error.Validation("Order.AddLine.InvalidPrice", "Unit price cannot be negative"));
        }

        var @event = new OrderLineAddedEvent(
            Id,
            lineId,
            productId,
            quantity,
            unitPrice);

        AddDomainEvent(@event);
        Apply(@event);

        return Result.Success();
    }

    /// <summary>
    /// Completes the order.
    /// </summary>
    public Result Complete(DateTimeOffset completedAt)
    {
        // Business rule: Order must have at least one line
        if (_orderLines.Count == 0)
        {
            return Result.Failure(
                Error.Validation("Order.Complete.Empty", "Cannot complete an order with no lines"));
        }

        // Business rule: Cannot complete already completed order
        if (Status == OrderStatus.Completed)
        {
            return Result.Failure(
                Error.Validation("Order.Complete.AlreadyCompleted", "Order is already completed"));
        }

        var @event = new OrderCompletedEvent(Id, completedAt);
        AddDomainEvent(@event);
        Apply(@event);

        return Result.Success();
    }

    /// <summary>
    /// Reconstitutes the aggregate from events.
    /// </summary>
    public static OrderAggregate FromEvents(OrderId orderId, IEnumerable<IDomainEvent> events)
    {
        var order = new OrderAggregate(orderId);
        foreach (var @event in events)
        {
            order.Apply(@event);
            // Increment version for each event applied during reconstitution
            order.IncrementVersion();
        }
        return order;
    }

    /// <summary>
    /// Applies the OrderPlaced event.
    /// </summary>
    private void Apply(OrderPlacedEvent @event)
    {
        CustomerId = @event.CustomerId;
        CreatedAt = @event.CreatedAt;
        Status = OrderStatus.Created;
    }

    /// <summary>
    /// Applies the OrderLineAdded event.
    /// </summary>
    private void Apply(OrderLineAddedEvent @event)
    {
        var orderLine = new OrderLine(
            @event.LineId,
            @event.ProductId,
            @event.Quantity,
            @event.UnitPrice);

        _orderLines.Add(orderLine);
    }

    /// <summary>
    /// Applies the OrderCompleted event.
    /// </summary>
    private void Apply(OrderCompletedEvent @event)
    {
        CompletedAt = @event.CompletedAt;
        Status = OrderStatus.Completed;
    }

    /// <summary>
    /// Routes events to appropriate Apply methods.
    /// </summary>
    private void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent placed:
                Apply(placed);
                break;
            case OrderLineAddedEvent lineAdded:
                Apply(lineAdded);
                break;
            case OrderCompletedEvent completed:
                Apply(completed);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
        }
    }
}
