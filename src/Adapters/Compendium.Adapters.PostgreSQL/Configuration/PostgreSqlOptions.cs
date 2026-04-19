// -----------------------------------------------------------------------
// <copyright file="PostgreSqlOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.PostgreSQL.Configuration;

/// <summary>
/// Configuration options for PostgreSQL event store.
/// </summary>
public sealed class PostgreSqlOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Compendium:EventStore";

    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum application-level connection pool size.
    /// This uses a SemaphoreSlim to limit concurrent database operations.
    /// Recommended: 200 for high-load scenarios (1000+ concurrent operations).
    /// Note: This is separate from Npgsql's internal connection pooling.
    /// </summary>
    public int MaxPoolSize { get; set; } = 200;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// Increased to 60s to support bulk operations and high-load scenarios.
    /// </summary>
    public int CommandTimeout { get; set; } = 60;

    /// <summary>
    /// Gets or sets the table name for event storage.
    /// </summary>
    public string TableName { get; set; } = "event_store";

    /// <summary>
    /// Gets or sets whether to automatically create the schema.
    /// </summary>
    public bool AutoCreateSchema { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch size for bulk operations.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the minimum Npgsql connection pool size.
    /// Pre-warms connections for immediate availability.
    /// Recommended: 50 for production workloads.
    /// </summary>
    public int MinimumPoolSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum Npgsql connection pool size.
    /// Must be less than PostgreSQL server's max_connections.
    /// Recommended: 200 for high-concurrency scenarios.
    /// </summary>
    public int MaximumPoolSize { get; set; } = 200;

    /// <summary>
    /// Gets or sets the connection idle lifetime in seconds.
    /// Idle connections are closed after this duration.
    /// Recommended: 900 (15 minutes).
    /// </summary>
    public int ConnectionIdleLifetime { get; set; } = 900;

    /// <summary>
    /// Gets or sets the connection lifetime in seconds.
    /// Connections are recycled after this duration regardless of activity.
    /// Recommended: 3600 (1 hour) to prevent stale connections.
    /// </summary>
    public int ConnectionLifetime { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// Time to wait for a connection from the pool.
    /// Recommended: 30 seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the TCP keepalive interval in seconds.
    /// Detects and closes broken connections.
    /// Recommended: 30 seconds. Set to 0 to disable.
    /// </summary>
    public int Keepalive { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable Npgsql connection pooling.
    /// Should always be true for production. Disable only for debugging.
    /// </summary>
    public bool EnablePooling { get; set; } = true;
}
