// -----------------------------------------------------------------------
// <copyright file="RedisFixture.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.IntegrationTests.Infrastructure;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Compendium.IntegrationTests.Fixtures;

/// <summary>
/// Shared fixture for Redis integration tests.
/// Provides connection string and cleanup capabilities.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _connection;

    public string ConnectionString { get; private set; } = string.Empty;
    public bool UsesTestContainer { get; private set; }

    public async Task InitializeAsync()
    {
        // Try to get connection string from helper (env var or Docker Compose)
        var externalConnectionString = EnvironmentConfigurationHelper.GetRedisConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            ConnectionString = externalConnectionString;
            UsesTestContainer = false;
            Console.WriteLine($"✅ RedisFixture: Using external Redis");
        }
        else
        {
            // Start TestContainer
            Console.WriteLine($"⚠️ RedisFixture: Starting TestContainer...");
            _container = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            UsesTestContainer = true;
            Console.WriteLine($"✅ RedisFixture: TestContainer started");
        }

        // Create connection for cleanup operations
        _connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();

        if (_container != null)
        {
            await _container.DisposeAsync();
            Console.WriteLine($"✅ RedisFixture: TestContainer disposed");
        }
    }

    /// <summary>
    /// Flushes all Redis databases for test isolation.
    /// </summary>
    public async Task FlushAllAsync()
    {
        if (_connection == null)
        {
            return;
        }

        try
        {
            var endpoints = _connection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _connection.GetServer(endpoint);
                await server.FlushAllDatabasesAsync();
            }
            Console.WriteLine($"🧹 Redis: All databases flushed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to flush Redis: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a Redis database for testing.
    /// </summary>
    public IDatabase GetDatabase(int db = 0)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Redis connection not initialized");
        }

        return _connection.GetDatabase(db);
    }

    /// <summary>
    /// Gets connection multiplexer for advanced scenarios.
    /// </summary>
    public IConnectionMultiplexer GetConnectionMultiplexer()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Redis connection not initialized");
        }

        return _connection;
    }
}
