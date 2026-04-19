// -----------------------------------------------------------------------
// <copyright file="RedisIdempotencyStoreIntegrationTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Idempotency;
using Compendium.Application.Idempotency;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.Redis;
using Compendium.IntegrationTests.Fixtures;
using Xunit;

namespace Compendium.IntegrationTests.Idempotency;

/// <summary>
/// Integration tests for RedisIdempotencyStore using real Redis container.
/// Tests caching behavior, expiration, and concurrent access scenarios.
/// </summary>
public sealed class RedisIdempotencyStoreIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _redis;
    private IConnectionMultiplexer? _connectionMultiplexer;
    private RedisIdempotencyStore? _idempotencyStore;
    private IdempotencyService? _idempotencyService;
    private string? _redisConnectionString;

    public async Task InitializeAsync()
    {
        // Check for environment variable first (CI/CD with remote infrastructure)
        _redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(_redisConnectionString))
        {
            // Fallback to TestContainers for local development
            _redis = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await _redis.StartAsync();
            _redisConnectionString = _redis.GetConnectionString();
        }

        var options = Options.Create(new RedisOptions
        {
            ConnectionString = _redisConnectionString,
            DefaultDatabase = 0,
            ConnectTimeout = 5000,
            CommandTimeout = 5000,
            KeyPrefix = "test"
        });

        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);

        var logger = Substitute.For<ILogger<RedisIdempotencyStore>>();

        _idempotencyStore = new RedisIdempotencyStore(_connectionMultiplexer, options, logger);
        _idempotencyService = new IdempotencyService(_idempotencyStore, TimeSpan.FromMinutes(5));
    }

    public async Task DisposeAsync()
    {
        if (_idempotencyStore != null)
        {
            await _idempotencyStore.DisposeAsync();
        }

        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        if (_redis != null)
        {
            await _redis.DisposeAsync();
        }
    }

    [RequiresDockerFact]
    public async Task SetAsync_AndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var key = "test-key-1";
        var value = new TestData { Id = 123, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        var setResult = await _idempotencyStore!.SetAsync(key, value, expiration);
        var getResult = await _idempotencyStore.GetAsync<TestData>(key);

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value!.Id.Should().Be(123);
        getResult.Value.Name.Should().Be("Test");
    }

    [RequiresDockerFact]
    public async Task ExistsAsync_ShouldReturnCorrectValue()
    {
        // Arrange
        var existingKey = "existing-key";
        var nonExistingKey = "non-existing-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(5);

        await _idempotencyStore!.SetAsync(existingKey, value, expiration);

        // Act & Assert
        var exists = await _idempotencyStore.ExistsAsync(existingKey);
        exists.IsSuccess.Should().BeTrue();
        exists.Value.Should().BeTrue();

        var notExists = await _idempotencyStore.ExistsAsync(nonExistingKey);
        notExists.IsSuccess.Should().BeTrue();
        notExists.Value.Should().BeFalse();
    }

    [RequiresDockerFact]
    public async Task SetAsync_WithExpiration_ShouldExpireAfterTTL()
    {
        // Arrange
        var key = "expiring-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMilliseconds(500);

        // Act
        await _idempotencyStore!.SetAsync(key, value, expiration);

        // Assert - Should exist immediately
        var existsImmediately = await _idempotencyStore.ExistsAsync(key);
        existsImmediately.IsSuccess.Should().BeTrue();
        existsImmediately.Value.Should().BeTrue();

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert - Should not exist after expiration
        var existsAfterExpiration = await _idempotencyStore.ExistsAsync(key);
        existsAfterExpiration.IsSuccess.Should().BeTrue();
        existsAfterExpiration.Value.Should().BeFalse();
    }

    [RequiresDockerFact]
    public async Task IdempotencyService_IsProcessedAsync_ShouldDetectProcessedOperations()
    {
        // Arrange
        var operationKey = "operation-123";

        // Act & Assert - Initially not processed
        var isProcessedInitially = await _idempotencyService!.IsProcessedAsync(operationKey);
        isProcessedInitially.Should().BeFalse();

        // Mark as processed
        await _idempotencyService.MarkAsProcessedAsync(operationKey);

        // Should now be processed
        var isProcessedAfterMarking = await _idempotencyService.IsProcessedAsync(operationKey);
        isProcessedAfterMarking.Should().BeTrue();
    }

    [RequiresDockerFact]
    public async Task IdempotencyService_SetResultAsync_AndGetResultAsync_ShouldStoreAndRetrieveResult()
    {
        // Arrange
        var operationKey = "operation-with-result";
        var result = new TestResult { Success = true, Message = "Operation completed" };

        // Act
        await _idempotencyService!.SetResultAsync(operationKey, result);
        var retrievedResult = await _idempotencyService.GetResultAsync<TestResult>(operationKey);

        // Assert
        retrievedResult.Should().NotBeNull();
        retrievedResult!.Success.Should().BeTrue();
        retrievedResult.Message.Should().Be("Operation completed");

        // Should also be marked as processed
        var isProcessed = await _idempotencyService.IsProcessedAsync(operationKey);
        isProcessed.Should().BeTrue();
    }

    [RequiresDockerFact]
    public async Task ConcurrentAccess_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var key = "concurrent-key";
        var tasks = new List<Task>();

        // Act - Multiple concurrent writes
        for (int i = 0; i < 10; i++)
        {
            var value = i;
            tasks.Add(Task.Run(async () =>
            {
                await _idempotencyStore!.SetAsync($"{key}-{value}", $"value-{value}", TimeSpan.FromMinutes(5));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All values should be stored correctly
        for (int i = 0; i < 10; i++)
        {
            var exists = await _idempotencyStore!.ExistsAsync($"{key}-{i}");
            exists.IsSuccess.Should().BeTrue();
            exists.Value.Should().BeTrue();

            var valueResult = await _idempotencyStore.GetAsync<string>($"{key}-{i}");
            valueResult.IsSuccess.Should().BeTrue();
            valueResult.Value.Should().Be($"value-{i}");
        }
    }

    [RequiresDockerFact]
    public async Task LargeDataStorage_ShouldHandleLargeValues()
    {
        // Arrange
        var key = "large-data-key";
        var largeData = new TestData
        {
            Id = 999,
            Name = new string('X', 10000), // 10KB string
            Data = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToArray()
        };

        // Act
        await _idempotencyStore!.SetAsync(key, largeData, TimeSpan.FromMinutes(5));
        var retrievedResult = await _idempotencyStore.GetAsync<TestData>(key);

        // Assert
        retrievedResult.IsSuccess.Should().BeTrue();
        retrievedResult.Value.Should().NotBeNull();
        retrievedResult.Value!.Id.Should().Be(999);
        retrievedResult.Value.Name.Length.Should().Be(10000);
        retrievedResult.Value.Data.Should().HaveCount(1000);
    }

    [RequiresDockerFact]
    public async Task KeyPrefix_ShouldIsolateKeys()
    {
        // Arrange
        var key = "isolation-test";
        var value1 = "value1";
        var value2 = "value2";

        // Create another store with different prefix
        var options2 = Options.Create(new RedisOptions
        {
            ConnectionString = _redisConnectionString!,
            DefaultDatabase = 0,
            KeyPrefix = "different-prefix"
        });

        var logger2 = Substitute.For<ILogger<RedisIdempotencyStore>>();
        await using var store2 = new RedisIdempotencyStore(_connectionMultiplexer!, options2, logger2);

        // Act
        await _idempotencyStore!.SetAsync(key, value1, TimeSpan.FromMinutes(5));
        await store2.SetAsync(key, value2, TimeSpan.FromMinutes(5));

        // Assert - Both stores should have their own values
        var retrieved1 = await _idempotencyStore.GetAsync<string>(key);
        var retrieved2 = await store2.GetAsync<string>(key);

        retrieved1.IsSuccess.Should().BeTrue();
        retrieved2.IsSuccess.Should().BeTrue();
        retrieved1.Value.Should().Be(value1);
        retrieved2.Value.Should().Be(value2);
    }

    /// <summary>
    /// Test data class for testing serialization.
    /// </summary>
    private sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] Data { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Test result class for testing idempotency.
    /// </summary>
    private sealed class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
