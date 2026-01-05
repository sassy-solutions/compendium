// -----------------------------------------------------------------------
// <copyright file="EnvironmentConfigurationHelper.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.IntegrationTests.Infrastructure;

/// <summary>
/// Helper for managing test environment configuration with fallback strategy:
/// 1. Environment variables (CI/CD or manual override)
/// 2. Docker Compose local (localhost)
/// 3. TestContainers (automatic)
/// </summary>
public static class EnvironmentConfigurationHelper
{
    /// <summary>
    /// Gets the PostgreSQL connection string using fallback strategy.
    /// </summary>
    public static string GetPostgreSqlConnectionString()
    {
        // Priority 1: Environment variable (CI/CD or manual override)
        var envConnectionString = Environment.GetEnvironmentVariable("EVENTSTORE_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            Console.WriteLine($"✅ Using PostgreSQL from environment variable");
            return envConnectionString;
        }

        // Priority 2: Check if Docker Compose is running locally
        var dockerConnectionString = "Host=localhost;Database=compendium;Username=compendium_user;Password=compendium_password;Port=5432;Timeout=30;Command Timeout=30";
        if (IsPostgreSqlAvailable(dockerConnectionString))
        {
            Console.WriteLine($"✅ Using PostgreSQL from Docker Compose (localhost:5432)");
            return dockerConnectionString;
        }

        // Priority 3: TestContainers will be used (handled by test fixture)
        Console.WriteLine($"⚠️ No local PostgreSQL found. TestContainers will be used.");
        return string.Empty; // Empty indicates TestContainers should be used
    }

    /// <summary>
    /// Gets the Redis connection string using fallback strategy.
    /// </summary>
    public static string GetRedisConnectionString()
    {
        // Priority 1: Environment variable
        var envConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            Console.WriteLine($"✅ Using Redis from environment variable");
            return envConnectionString;
        }

        // Priority 2: Check if Docker Compose Redis is running
        var dockerConnectionString = "localhost:6379";
        if (IsRedisAvailable(dockerConnectionString))
        {
            Console.WriteLine($"✅ Using Redis from Docker Compose (localhost:6379)");
            return dockerConnectionString;
        }

        // Priority 3: TestContainers
        Console.WriteLine($"⚠️ No local Redis found. TestContainers will be used.");
        return string.Empty;
    }

    /// <summary>
    /// Checks if PostgreSQL is available at the given connection string.
    /// </summary>
    private static bool IsPostgreSqlAvailable(string connectionString)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            connection.Open();
            connection.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if Redis is available at the given connection string.
    /// </summary>
    private static bool IsRedisAvailable(string connectionString)
    {
        try
        {
            var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            var pingResult = redis.GetDatabase().Ping();
            redis.Dispose();
            return pingResult.TotalMilliseconds > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if TestContainers should be used based on available infrastructure.
    /// </summary>
    public static bool ShouldUseTestContainers()
    {
        var postgresConnectionString = GetPostgreSqlConnectionString();
        var redisConnectionString = GetRedisConnectionString();

        return string.IsNullOrEmpty(postgresConnectionString) || string.IsNullOrEmpty(redisConnectionString);
    }

    /// <summary>
    /// Gets a summary of the current test infrastructure configuration.
    /// </summary>
    public static string GetInfrastructureSummary()
    {
        var postgres = !string.IsNullOrEmpty(GetPostgreSqlConnectionString()) ? "External/Docker" : "TestContainers";
        var redis = !string.IsNullOrEmpty(GetRedisConnectionString()) ? "External/Docker" : "TestContainers";

        return $"PostgreSQL: {postgres}, Redis: {redis}";
    }
}
