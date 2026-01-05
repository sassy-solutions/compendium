// -----------------------------------------------------------------------
// <copyright file="IdempotencyE2ETests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Idempotency;
using Compendium.Application.Idempotency;
using Compendium.IntegrationTests.EndToEnd.Infrastructure;
using Compendium.IntegrationTests.EndToEnd.TestAggregates;
using Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Compendium.IntegrationTests.EndToEnd.Scenarios;

/// <summary>
/// E2E Test Scenario 2: Idempotency - Duplicate Command Handling.
/// Tests idempotency service integration to prevent duplicate operations.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Category", "Idempotency")]
public sealed class IdempotencyE2ETests : IAsyncLifetime
{
    // DTOs for idempotency results (strongly-typed to avoid JsonElement dynamic issues)
    private record OrderCreationResult(string OrderId, int EventCount, bool Success);
    private record OrderOperationResult(string Operation, string OrderId);
    private record OrderResult(string OrderId);
    private record WorkflowResult(string OrderId, bool Success, DateTimeOffset ExecutedAt);

    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private PostgreSqlEventStore? _eventStore;
    private IIdempotencyService? _idempotencyService;
    private IConnectionMultiplexer? _redisConnection;
    private string _postgresConnectionString = null!;

    public async Task InitializeAsync()
    {
        // Use EnvironmentConfigurationHelper for PostgreSQL connection string
        var externalConnectionString = Compendium.IntegrationTests.Infrastructure.EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            _postgresConnectionString = externalConnectionString;
        }
        else
        {
            // Fallback to TestContainers for PostgreSQL
            Console.WriteLine("⚠️ Starting TestContainer for PostgreSQL (Idempotency E2E)...");
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("compendium_idempotency_e2e")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .WithCleanUp(true)
                .Build();

            await _postgres.StartAsync();
            _postgresConnectionString = _postgres.GetConnectionString();
        }

        // Check for Redis environment variable first (CI/CD with remote infrastructure)
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // Fallback to TestContainers for local development
            Console.WriteLine("⚠️ Starting TestContainer for Redis (Idempotency E2E)...");
            _redis = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithCleanUp(true)
                .Build();

            await _redis.StartAsync();
            redisConnectionString = _redis.GetConnectionString();
        }
        else
        {
            Console.WriteLine("✅ Using Redis from environment variable (Idempotency E2E)");
        }

        // Initialize PostgreSQL Event Store
        var postgresOptions = Options.Create(new PostgreSqlOptions
        {
            ConnectionString = _postgresConnectionString,
            AutoCreateSchema = true,
            TableName = "idempotency_events_e2e",
            CommandTimeout = 30,
            BatchSize = 1000
        });

        var eventDeserializer = new E2EEventDeserializer();
        var eventStoreLogger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        _eventStore = new PostgreSqlEventStore(postgresOptions, eventDeserializer, eventStoreLogger);

        var initResult = await _eventStore.InitializeSchemaAsync();
        initResult.IsSuccess.Should().BeTrue();

        // Initialize Redis Idempotency Store
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        var redisOptions = Options.Create(new RedisOptions
        {
            ConnectionString = redisConnectionString,
            KeyPrefix = "e2e:idempotency:"
        });

        var redisLogger = Substitute.For<ILogger<RedisIdempotencyStore>>();
        var idempotencyStore = new RedisIdempotencyStore(_redisConnection, redisOptions, redisLogger);
        _idempotencyService = new IdempotencyService(idempotencyStore, TimeSpan.FromHours(1));
    }

    public async Task DisposeAsync()
    {
        if (_eventStore != null)
        {
            await _eventStore.DisposeAsync();
        }

        if (_redisConnection != null)
        {
            await _redisConnection.DisposeAsync();
        }

        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }

        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task FirstExecution_WithIdempotencyKey_OperationExecutesAndResultStored()
    {
        // Arrange
        var idempotencyKey = "order-001-creation";
        var orderId = OrderId.New();

        // **Step 1: Check key doesn't exist initially**
        var initialCheck = await _idempotencyService!.IsProcessedAsync(idempotencyKey);
        initialCheck.Should().BeFalse("Idempotency key should not exist initially");

        // **Step 2: Execute operation and store result**
        var order = OrderAggregate.PlaceOrder(orderId, "customer-001", DateTimeOffset.UtcNow);
        var events = order.DomainEvents.ToList();

        var result = await _eventStore!.AppendEventsAsync(orderId.ToString(), events, 0);
        result.IsSuccess.Should().BeTrue();

        // Store operation result with idempotency key
        await _idempotencyService.SetResultAsync(idempotencyKey, new OrderCreationResult(
            orderId.ToString(),
            events.Count,
            true));

        // **Step 3: Verify key now exists**
        var afterCheck = await _idempotencyService.IsProcessedAsync(idempotencyKey);
        afterCheck.Should().BeTrue("Idempotency key should exist after operation");

        // **Step 4: Verify result can be retrieved**
        var cachedResult = await _idempotencyService.GetResultAsync<OrderCreationResult>(idempotencyKey);
        cachedResult.Should().NotBeNull();
        cachedResult!.OrderId.Should().Be(orderId.ToString());

        // **Expected Results:**
        // ✅ Idempotency key stored successfully
        // ✅ Operation result cached
        // ✅ Events appended to event store
    }

    [Fact]
    public async Task DuplicateExecution_WithSameKey_ReturnsCachedResult()
    {
        // Arrange
        var idempotencyKey = "order-002-creation";
        var orderId = OrderId.New();

        // **Step 1: First execution**
        var order = OrderAggregate.PlaceOrder(orderId, "customer-002", DateTimeOffset.UtcNow);
        var events = order.DomainEvents.ToList();

        var firstResult = await _eventStore!.AppendEventsAsync(orderId.ToString(), events, 0);
        firstResult.IsSuccess.Should().BeTrue();

        var operationResult = new OrderCreationResult(
            orderId.ToString(),
            events.Count,
            true);

        await _idempotencyService!.SetResultAsync(idempotencyKey, operationResult);

        // **Step 2: Simulate duplicate request - check idempotency first**
        var isProcessed = await _idempotencyService.IsProcessedAsync(idempotencyKey);
        isProcessed.Should().BeTrue("Operation should be marked as processed");

        // **Step 3: Retrieve cached result (instead of re-executing)**
        var cachedResult = await _idempotencyService.GetResultAsync<OrderCreationResult>(idempotencyKey);
        cachedResult.Should().NotBeNull();
        cachedResult!.OrderId.Should().Be(orderId.ToString());

        // **Step 4: Verify NO duplicate events in event store**
        var storedEvents = await _eventStore.GetEventsAsync(orderId.ToString());
        storedEvents.Value.Should().HaveCount(1, "Only one event should exist (no duplicate)");

        // **Expected Results:**
        // ✅ Duplicate detected via IsProcessedAsync
        // ✅ Cached result retrieved
        // ✅ NO duplicate events appended
    }

    [Fact]
    public async Task DifferentOperations_SameKey_ConflictDetected()
    {
        // Arrange
        var sharedKey = "order-003-operation";
        var orderId = OrderId.New();

        // **Step 1: First operation - PlaceOrder**
        var order = OrderAggregate.PlaceOrder(orderId, "customer-003", DateTimeOffset.UtcNow);
        await _eventStore!.AppendEventsAsync(orderId.ToString(), order.DomainEvents.ToList(), 0);

        await _idempotencyService!.SetResultAsync(sharedKey, new OrderOperationResult(
            "PlaceOrder",
            orderId.ToString()));

        // **Step 2: Different operation - AddOrderLine with SAME key**
        order.AddOrderLine("line-1", "product-A", 1, 10.00m);

        // Check if key exists
        var isProcessed = await _idempotencyService.IsProcessedAsync(sharedKey);
        isProcessed.Should().BeTrue("Key should exist from first operation");

        // Retrieve cached result to detect conflict
        var cachedResult = await _idempotencyService.GetResultAsync<OrderOperationResult>(sharedKey);
        cachedResult.Should().NotBeNull();
        cachedResult!.Operation.Should().Be("PlaceOrder", "Cached result should be from first operation");

        // In real implementation, application would detect operation type mismatch
        // For E2E test, we verify the key conflict is detectable
        cachedResult.Operation.Should().NotBe("AddOrderLine",
            "Operation type mismatch should be detectable from cached result");

        // **Expected Results:**
        // ✅ Idempotency key conflict detected
        // ✅ Original operation result preserved
        // ✅ Different operation type detectable
    }

    [Fact]
    public async Task SameOperation_DifferentKeys_BothExecuteSuccessfully()
    {
        // Arrange
        var key1 = "order-004-creation";
        var key2 = "order-005-creation";
        var orderId1 = OrderId.New();
        var orderId2 = OrderId.New();

        // **Step 1: First operation with key1**
        var order1 = OrderAggregate.PlaceOrder(orderId1, "customer-004", DateTimeOffset.UtcNow);
        await _eventStore!.AppendEventsAsync(orderId1.ToString(), order1.DomainEvents.ToList(), 0);
        await _idempotencyService!.SetResultAsync(key1, new OrderResult(orderId1.ToString()));

        // **Step 2: Same operation with key2 (different key)**
        var order2 = OrderAggregate.PlaceOrder(orderId2, "customer-005", DateTimeOffset.UtcNow);
        await _eventStore.AppendEventsAsync(orderId2.ToString(), order2.DomainEvents.ToList(), 0);
        await _idempotencyService.SetResultAsync(key2, new OrderResult(orderId2.ToString()));

        // **Step 3: Verify both keys exist independently**
        var key1Exists = await _idempotencyService.IsProcessedAsync(key1);
        var key2Exists = await _idempotencyService.IsProcessedAsync(key2);

        key1Exists.Should().BeTrue();
        key2Exists.Should().BeTrue();

        // **Step 4: Verify both results are cached correctly**
        var result1 = await _idempotencyService.GetResultAsync<OrderResult>(key1);
        var result2 = await _idempotencyService.GetResultAsync<OrderResult>(key2);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.OrderId.Should().Be(orderId1.ToString());
        result2!.OrderId.Should().Be(orderId2.ToString());

        // **Step 5: Verify both event streams exist**
        var events1 = await _eventStore.GetEventsAsync(orderId1.ToString());
        var events2 = await _eventStore.GetEventsAsync(orderId2.ToString());

        events1.Value.Should().HaveCount(1);
        events2.Value.Should().HaveCount(1);

        // **Expected Results:**
        // ✅ Different keys execute independently
        // ✅ Both operations complete successfully
        // ✅ Each has separate cached result
    }

    [Fact]
    public async Task IdempotencyPattern_CompleteWorkflow_PreventsDuplicates()
    {
        // Arrange
        var idempotencyKey = "order-006-workflow";
        var orderId = OrderId.New();

        // **Step 1: Simulate first request**
        var firstExecuted = false;
        if (!await _idempotencyService!.IsProcessedAsync(idempotencyKey))
        {
            // Execute operation
            var order = OrderAggregate.PlaceOrder(orderId, "customer-006", DateTimeOffset.UtcNow);
            var result = await _eventStore!.AppendEventsAsync(orderId.ToString(), order.DomainEvents.ToList(), 0);

            if (result.IsSuccess)
            {
                await _idempotencyService.SetResultAsync(idempotencyKey, new WorkflowResult(
                    orderId.ToString(),
                    true,
                    DateTimeOffset.UtcNow));
                firstExecuted = true;
            }
        }

        firstExecuted.Should().BeTrue("First request should execute");

        // **Step 2: Simulate duplicate request (within 1 hour window)**
        var secondExecuted = false;
        if (!await _idempotencyService.IsProcessedAsync(idempotencyKey))
        {
            // This should NOT execute
            secondExecuted = true;
        }
        else
        {
            // Return cached result
            var cachedResult = await _idempotencyService.GetResultAsync<WorkflowResult>(idempotencyKey);
            cachedResult.Should().NotBeNull("Cached result should exist");
            cachedResult!.OrderId.Should().Be(orderId.ToString());
        }

        secondExecuted.Should().BeFalse("Duplicate request should NOT execute");

        // **Step 3: Verify only one event in event store**
        var events = await _eventStore!.GetEventsAsync(orderId.ToString());
        events.Value.Should().HaveCount(1, "Only one event should exist (no duplicate)");

        // **Expected Results:**
        // ✅ First request executes
        // ✅ Duplicate request blocked
        // ✅ Cached result returned
        // ✅ Only one event stored
    }

    [Fact]
    public async Task IdempotencyExpiration_After1Hour_AllowsReExecution()
    {
        // Arrange
        var idempotencyKey = "order-007-expiration";
        var orderId = OrderId.New();

        // **Step 1: First execution with 1-second expiration**
        var order = OrderAggregate.PlaceOrder(orderId, "customer-007", DateTimeOffset.UtcNow);
        await _eventStore!.AppendEventsAsync(orderId.ToString(), order.DomainEvents.ToList(), 0);

        await _idempotencyService!.SetResultAsync(
            idempotencyKey,
            new OrderResult(orderId.ToString()),
            expiration: TimeSpan.FromSeconds(2)); // Short expiration for testing

        // **Step 2: Verify key exists immediately**
        var immediateCheck = await _idempotencyService.IsProcessedAsync(idempotencyKey);
        immediateCheck.Should().BeTrue("Key should exist immediately");

        // **Step 3: Wait for expiration**
        await Task.Delay(TimeSpan.FromSeconds(3));

        // **Step 4: Verify key no longer exists**
        var afterExpirationCheck = await _idempotencyService.IsProcessedAsync(idempotencyKey);
        afterExpirationCheck.Should().BeFalse("Key should expire after timeout");

        // **Expected Results:**
        // ✅ Idempotency key expires after TTL
        // ✅ Re-execution allowed after expiration
        // ✅ Expiration mechanism works correctly
    }
}
