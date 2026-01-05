// -----------------------------------------------------------------------
// <copyright file="OrderLifecycleE2ETests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Core.Domain.Events;
using Compendium.IntegrationTests.EndToEnd.Infrastructure;
using Compendium.IntegrationTests.EndToEnd.TestAggregates;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// E2E Test Scenario 1: Complete Order Lifecycle (Happy Path).
/// Tests the full lifecycle of an order from creation through completion,
/// including event sourcing, aggregate reconstitution, and domain logic.
/// </summary>
[Trait("Category", "E2E")]
public sealed class OrderLifecycleE2ETests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private PostgreSqlEventStore? _eventStore;
    private E2EEventDeserializer? _eventDeserializer;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for connection string fallback
        var externalConnectionString = Compendium.IntegrationTests.Infrastructure.EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            _connectionString = externalConnectionString;
        }
        else
        {
            // Fallback to TestContainers
            Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL (E2E)...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_e2e")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithCleanUp(true)
                .Build();

            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
        }

        // Initialize event store
        var options = Options.Create(new PostgreSqlOptions
        {
            ConnectionString = _connectionString,
            AutoCreateSchema = true,
            TableName = "order_events_e2e",
            CommandTimeout = 30,
            BatchSize = 1000
        });

        _eventDeserializer = new E2EEventDeserializer();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        _eventStore = new PostgreSqlEventStore(options, _eventDeserializer, logger);

        // Initialize schema
        var initResult = await _eventStore.InitializeSchemaAsync();
        initResult.IsSuccess.Should().BeTrue();
    }

    public async Task DisposeAsync()
    {
        if (_eventStore != null)
        {
            await _eventStore.DisposeAsync();
        }

        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task CompleteOrderLifecycle_HappyPath_ShouldSucceed()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = "customer-001";
        var createdAt = DateTimeOffset.UtcNow;

        // **Step 1: Create Order (PlaceOrder)**
        var order = OrderAggregate.PlaceOrder(orderId, customerId, createdAt);
        order.Status.Should().Be(OrderStatus.Created);
        order.CustomerId.Should().Be(customerId);
        order.CreatedAt.Should().Be(createdAt);

        // Verify domain event was raised
        var domainEvents = order.DomainEvents.ToList();
        domainEvents.Should().HaveCount(1);
        order.ClearDomainEvents();

        // Append OrderPlaced event (version 1)
        var result1 = await _eventStore!.AppendEventsAsync(orderId.ToString(), domainEvents, 0);
        result1.IsSuccess.Should().BeTrue();

        // **Step 2: Add Order Lines (3 lines)**
        var lineResults = new[]
        {
            order.AddOrderLine("line-1", "product-A", 2, 10.00m),
            order.AddOrderLine("line-2", "product-B", 1, 25.00m),
            order.AddOrderLine("line-3", "product-C", 5, 5.00m)
        };

        lineResults.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        order.OrderLines.Should().HaveCount(3);
        order.TotalAmount.Should().Be(70.00m); // (2*10) + (1*25) + (5*5) = 70

        // Append OrderLineAdded events (versions 2, 3, 4)
        var line1Events = order.DomainEvents.ToList();
        line1Events.Should().HaveCount(3);
        order.ClearDomainEvents();

        var result2 = await _eventStore.AppendEventsAsync(orderId.ToString(), line1Events, 1);
        result2.IsSuccess.Should().BeTrue();

        // **Step 3: Complete Order**
        var completedAt = DateTimeOffset.UtcNow;
        var completeResult = order.Complete(completedAt);
        completeResult.IsSuccess.Should().BeTrue();

        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().Be(completedAt);

        // Append OrderCompleted event (version 5)
        var completedEvents = order.DomainEvents.ToList();
        completedEvents.Should().HaveCount(1);
        order.ClearDomainEvents();

        var result3 = await _eventStore.AppendEventsAsync(orderId.ToString(), completedEvents, 4);
        result3.IsSuccess.Should().BeTrue();

        // **Step 4: Reconstitute Aggregate from Events**
        var eventsResult = await _eventStore.GetEventsAsync(orderId.ToString());
        eventsResult.IsSuccess.Should().BeTrue();
        eventsResult.Value.Should().HaveCount(5);

        var reconstitutedOrder = OrderAggregate.FromEvents(orderId, eventsResult.Value);

        // **Step 5: Verify Reconstituted State**
        reconstitutedOrder.Id.Should().Be(orderId);
        reconstitutedOrder.CustomerId.Should().Be(customerId);
        reconstitutedOrder.Status.Should().Be(OrderStatus.Completed);
        reconstitutedOrder.OrderLines.Should().HaveCount(3);
        reconstitutedOrder.TotalAmount.Should().Be(70.00m);
        reconstitutedOrder.CreatedAt.Should().Be(createdAt);
        reconstitutedOrder.CompletedAt.Should().Be(completedAt);
        reconstitutedOrder.Version.Should().Be(5);

        // **Expected Results:**
        // ✅ All 5 events appended successfully
        // ✅ Aggregate reconstituted correctly from events
        // ✅ All business rules enforced
        // ✅ No concurrency conflicts
    }

    [Fact]
    public void AddLineToCompletedOrder_ShouldFail()
    {
        // Arrange
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-002", DateTimeOffset.UtcNow);

        // Add a line and complete the order
        order.AddOrderLine("line-1", "product-A", 1, 10.00m);
        order.Complete(DateTimeOffset.UtcNow);

        // Act - Try to add line to completed order
        var result = order.AddOrderLine("line-2", "product-B", 1, 5.00m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Order.AddLine.Completed");
    }

    [Fact]
    public void CompleteEmptyOrder_ShouldFail()
    {
        // Arrange
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-003", DateTimeOffset.UtcNow);

        // Act - Try to complete order with no lines
        var result = order.Complete(DateTimeOffset.UtcNow);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Order.Complete.Empty");
    }

    [Fact]
    public void AddLineWithInvalidQuantity_ShouldFail()
    {
        // Arrange
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-004", DateTimeOffset.UtcNow);

        // Act - Try to add line with zero quantity
        var result = order.AddOrderLine("line-1", "product-A", 0, 10.00m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Order.AddLine.InvalidQuantity");
    }

    [Fact]
    public void AddLineWithNegativePrice_ShouldFail()
    {
        // Arrange
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-005", DateTimeOffset.UtcNow);

        // Act - Try to add line with negative price
        var result = order.AddOrderLine("line-1", "product-A", 1, -10.00m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Order.AddLine.InvalidPrice");
    }

    [Fact]
    public async Task LargeOrderWithManyLines_ShouldHandlePerformantly()
    {
        // Arrange
        var orderId = OrderId.New();
        var order = OrderAggregate.PlaceOrder(orderId, "customer-006", DateTimeOffset.UtcNow);

        var initialEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        var result = await _eventStore!.AppendEventsAsync(orderId.ToString(), initialEvents, 0);
        result.IsSuccess.Should().BeTrue();

        // Act - Add 100 order lines
        var stopwatch = Stopwatch.StartNew();

        var allLineEvents = new List<IDomainEvent>();
        for (int i = 1; i <= 100; i++)
        {
            var lineResult = order.AddOrderLine($"line-{i}", $"product-{i}", 1, 10.00m);
            lineResult.IsSuccess.Should().BeTrue();

            var lineEvents = order.DomainEvents.ToList();
            allLineEvents.AddRange(lineEvents);
            order.ClearDomainEvents();
        }

        // Append all events in batches
        var appendResult = await _eventStore.AppendEventsAsync(orderId.ToString(), allLineEvents, 1);
        appendResult.IsSuccess.Should().BeTrue();

        stopwatch.Stop();

        // Assert
        order.OrderLines.Should().HaveCount(100);

        // Performance: Should complete in < 5 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);

        Console.WriteLine($"Added 100 order lines in {stopwatch.ElapsedMilliseconds}ms");
    }
}
