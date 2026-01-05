// -----------------------------------------------------------------------
// <copyright file="RedisOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Redis.Configuration;

/// <summary>
/// Configuration options for Redis connections and behavior.
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Compendium:Redis";

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the default database number to use.
    /// </summary>
    public int DefaultDatabase { get; set; } = 0;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the command timeout in milliseconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed operations.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to enable connection string validation.
    /// </summary>
    public bool ValidateConnectionString { get; set; } = true;

    /// <summary>
    /// Gets or sets the key prefix for all Redis operations.
    /// </summary>
    public string KeyPrefix { get; set; } = "compendium";
}
