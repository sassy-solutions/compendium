// -----------------------------------------------------------------------
// <copyright file="PostgreSqlFixture.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.IntegrationTests.Infrastructure;
using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Compendium.IntegrationTests.Fixtures;

/// <summary>
/// Shared fixture for PostgreSQL integration tests.
/// Provides connection string and cleanup capabilities.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;
    public bool UsesTestContainer { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        // Try to get connection string from helper (env var or Docker Compose)
        var externalConnectionString = EnvironmentConfigurationHelper.GetPostgreSqlConnectionString();

        if (!string.IsNullOrEmpty(externalConnectionString))
        {
            ConnectionString = externalConnectionString;
            UsesTestContainer = false;
            IsAvailable = true;
            Console.WriteLine($"✅ PostgreSqlFixture: Using external PostgreSQL");
        }
        else
        {
            // Start TestContainer
            Console.WriteLine($"⚠️ PostgreSqlFixture: Starting TestContainer...");
            try
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:15-alpine")
                    .WithDatabase("compendium_test")
                    .WithUsername("test_user")
                    .WithPassword("test_password")
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();
                UsesTestContainer = true;
                IsAvailable = true;
                Console.WriteLine($"✅ PostgreSqlFixture: TestContainer started");
            }
            catch (Exception ex) when (ex is ArgumentException || ex.InnerException is ArgumentException)
            {
                IsAvailable = false;
                UnavailableReason = "Docker is not running or misconfigured. Start Docker or set EVENTSTORE_CONNECTION_STRING.";
                Console.WriteLine($"⚠️ PostgreSqlFixture: {UnavailableReason}");
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            Console.WriteLine($"✅ PostgreSqlFixture: TestContainer disposed");
        }
    }

    /// <summary>
    /// Cleans up a specific table for test isolation.
    /// </summary>
    public async Task CleanTableAsync(string tableName)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync($"DELETE FROM {tableName}");
            Console.WriteLine($"🧹 Cleaned table: {tableName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to clean table {tableName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets PostgreSqlOptions configured for this fixture.
    /// </summary>
    public PostgreSqlOptions GetOptions(string tableName = "event_store_test")
    {
        return new PostgreSqlOptions
        {
            ConnectionString = ConnectionString,
            AutoCreateSchema = true,
            TableName = tableName,
            CommandTimeout = 30,
            BatchSize = 1000
        };
    }
}
