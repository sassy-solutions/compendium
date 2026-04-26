// QuickStart sample: define an Order aggregate, dispatch a command,
// update an in-memory projection, and read it back through a query.
//
// Run: dotnet run

using Compendium.Abstractions.CQRS.Commands;
using Compendium.Abstractions.CQRS.Handlers;
using Compendium.Abstractions.CQRS.Queries;
using Compendium.Application.CQRS;
using Compendium.Core.Domain.Events;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Results;
using Microsoft.Extensions.DependencyInjection;

namespace QuickStart.OrderAggregate;

// ── 1. Domain events ────────────────────────────────────────────────────────

public sealed class OrderPlaced : DomainEventBase
{
    public OrderPlaced(string orderId, string customerId, decimal totalAmount, long version)
        : base(orderId, nameof(Order), version)
    {
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }

    public string CustomerId { get; }
    public decimal TotalAmount { get; }
}

public sealed class OrderShipped : DomainEventBase
{
    public OrderShipped(string orderId, DateTimeOffset shippedAt, long version)
        : base(orderId, nameof(Order), version)
    {
        ShippedAt = shippedAt;
    }

    public DateTimeOffset ShippedAt { get; }
}

// ── 2. Aggregate ────────────────────────────────────────────────────────────

public sealed class Order : AggregateRoot<Guid>
{
    private Order(Guid id) : base(id) { }

    public string CustomerId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTimeOffset? ShippedAt { get; private set; }

    public static Result<Order> Place(Guid id, string customerId, decimal totalAmount)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result.Failure<Order>(Error.Validation("Order.CustomerId.Empty", "CustomerId cannot be empty."));
        }

        if (totalAmount <= 0m)
        {
            return Result.Failure<Order>(Error.Validation("Order.TotalAmount.NotPositive", "TotalAmount must be greater than zero."));
        }

        var order = new Order(id)
        {
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Status = OrderStatus.Placed,
        };

        order.AddDomainEvent(new OrderPlaced(id.ToString(), customerId, totalAmount, order.Version + 1));
        order.IncrementVersion();
        return Result.Success(order);
    }

    public Result Ship()
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(Error.Conflict("Order.NotPlaced", $"Cannot ship order in status {Status}."));
        }

        ShippedAt = DateTimeOffset.UtcNow;
        Status = OrderStatus.Shipped;

        AddDomainEvent(new OrderShipped(Id.ToString(), ShippedAt.Value, Version + 1));
        IncrementVersion();
        return Result.Success();
    }
}

public enum OrderStatus { Pending, Placed, Shipped }

// ── 3. Command + handler ────────────────────────────────────────────────────

public sealed record PlaceOrderCommand(Guid OrderId, string CustomerId, decimal TotalAmount)
    : ICommand<Guid>;

public sealed class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, Guid>
{
    private readonly IOrderEventLog _eventLog;
    private readonly OrderSummaryProjection _projection;

    public PlaceOrderHandler(IOrderEventLog eventLog, OrderSummaryProjection projection)
    {
        _eventLog = eventLog;
        _projection = projection;
    }

    public Task<Result<Guid>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var result = Order.Place(command.OrderId, command.CustomerId, command.TotalAmount);
        if (result.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(result.Error));
        }

        var order = result.Value!;
        var events = order.GetUncommittedEvents();
        _eventLog.Append(events);
        _projection.Apply(events);

        return Task.FromResult(Result.Success(order.Id));
    }
}

// ── 4. Query + handler ──────────────────────────────────────────────────────

public sealed record GetOrderSummaryQuery(Guid OrderId) : IQuery<OrderSummary>;

public sealed record OrderSummary(Guid OrderId, string CustomerId, decimal TotalAmount, string Status);

public sealed class GetOrderSummaryHandler : IQueryHandler<GetOrderSummaryQuery, OrderSummary>
{
    private readonly OrderSummaryProjection _projection;

    public GetOrderSummaryHandler(OrderSummaryProjection projection) => _projection = projection;

    public Task<Result<OrderSummary>> HandleAsync(GetOrderSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var summary = _projection.Get(query.OrderId);
        return Task.FromResult(summary is null
            ? Result.Failure<OrderSummary>(Error.NotFound("Order.NotFound", $"Order {query.OrderId} not found."))
            : Result.Success(summary));
    }
}

// ── 5. In-memory event log + projection ─────────────────────────────────────

public interface IOrderEventLog
{
    void Append(IEnumerable<IDomainEvent> events);
    IReadOnlyList<IDomainEvent> All();
}

public sealed class InMemoryOrderEventLog : IOrderEventLog
{
    private readonly List<IDomainEvent> _events = new();

    public void Append(IEnumerable<IDomainEvent> events) => _events.AddRange(events);

    public IReadOnlyList<IDomainEvent> All() => _events.ToList();
}

public sealed class OrderSummaryProjection
{
    private readonly Dictionary<Guid, OrderSummary> _summaries = new();

    public void Apply(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case OrderPlaced placed:
                    _summaries[Guid.Parse(placed.AggregateId)] = new OrderSummary(
                        Guid.Parse(placed.AggregateId), placed.CustomerId, placed.TotalAmount, "Placed");
                    break;

                case OrderShipped shipped when _summaries.TryGetValue(Guid.Parse(shipped.AggregateId), out var existing):
                    _summaries[Guid.Parse(shipped.AggregateId)] = existing with { Status = "Shipped" };
                    break;
            }
        }
    }

    public OrderSummary? Get(Guid orderId) => _summaries.GetValueOrDefault(orderId);
}

// ── 6. Composition root ─────────────────────────────────────────────────────

public static class Program
{
    public static async Task<int> Main()
    {
        var services = new ServiceCollection();

        // Compendium dispatchers
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();

        // In-memory infrastructure
        services.AddSingleton<IOrderEventLog, InMemoryOrderEventLog>();
        services.AddSingleton<OrderSummaryProjection>();

        // Handlers
        services.AddSingleton<ICommandHandler<PlaceOrderCommand, Guid>, PlaceOrderHandler>();
        services.AddSingleton<IQueryHandler<GetOrderSummaryQuery, OrderSummary>, GetOrderSummaryHandler>();

        await using var provider = services.BuildServiceProvider();

        var commands = provider.GetRequiredService<ICommandDispatcher>();
        var queries = provider.GetRequiredService<IQueryDispatcher>();
        var log = provider.GetRequiredService<IOrderEventLog>();

        Console.WriteLine("=== Compendium QuickStart: Order aggregate ===\n");

        // 1. Place an order
        var orderId = Guid.NewGuid();
        var place = await commands.DispatchAsync<PlaceOrderCommand, Guid>(
            new PlaceOrderCommand(orderId, CustomerId: "cust-001", TotalAmount: 49.95m));

        if (place.IsFailure)
        {
            Console.Error.WriteLine($"PlaceOrder failed: {place.Error.Code} - {place.Error.Message}");
            return 1;
        }

        Console.WriteLine($"  ✓ Order placed: {place.Value}");

        // 2. Read the projection
        var summary = await queries.DispatchAsync<GetOrderSummaryQuery, OrderSummary>(
            new GetOrderSummaryQuery(orderId));

        if (summary.IsFailure)
        {
            Console.Error.WriteLine($"GetOrderSummary failed: {summary.Error.Code} - {summary.Error.Message}");
            return 1;
        }

        Console.WriteLine($"  ✓ Projection: {summary.Value}");

        // 3. Show events that were captured
        Console.WriteLine($"\nDomain events captured ({log.All().Count}):");
        foreach (var @event in log.All())
        {
            Console.WriteLine($"    • {@event}");
        }

        Console.WriteLine("\nDone.");
        return 0;
    }
}
