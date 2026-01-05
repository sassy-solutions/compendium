// -----------------------------------------------------------------------
// <copyright file="RedisIdempotencyStore.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Adapters.Redis.Configuration;
using Compendium.Application.Idempotency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Idempotency;

/// <summary>
/// Redis implementation of IIdempotencyStore for distributed idempotency tracking.
/// Provides high-performance, distributed storage for operation results with configurable TTL.
/// </summary>
public sealed class RedisIdempotencyStore : IIdempotencyStore, IAsyncDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisIdempotencyStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisIdempotencyStore"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    /// <param name="options">Redis configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public RedisIdempotencyStore(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisOptions> options,
        ILogger<RedisIdempotencyStore> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _database = _connectionMultiplexer.GetDatabase(_options.DefaultDatabase);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        ThrowIfDisposed();

        var redisKey = GetRedisKey(key);

        try
        {
            var exists = await _database.KeyExistsAsync(redisKey).ConfigureAwait(false);

            _logger.LogDebug("Checked existence for key {Key}: {Exists}", key, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence for key {Key}", key);

            // In case of Redis failure, assume key doesn't exist to allow operation to proceed
            // This provides graceful degradation when Redis is unavailable
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<TResult?> GetAsync<TResult>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        ThrowIfDisposed();

        var redisKey = GetRedisKey(key);

        try
        {
            var value = await _database.StringGetAsync(redisKey).ConfigureAwait(false);

            if (!value.HasValue)
            {
                _logger.LogDebug("No value found for key {Key}", key);
                return default;
            }

            var deserializedValue = JsonSerializer.Deserialize<TResult>(value!, _jsonOptions);

            _logger.LogDebug("Retrieved value for key {Key}", key);

            return deserializedValue;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize value for key {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value for key {Key}", key);

            // In case of Redis failure, return null to allow operation to proceed
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Expiration must be greater than zero", nameof(expiration));
        }

        ThrowIfDisposed();

        var redisKey = GetRedisKey(key);

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

            var success = await _database.StringSetAsync(redisKey, serializedValue, expiration).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogWarning("Failed to set value for key {Key}", key);
                return;
            }

            _logger.LogDebug("Set value for key {Key} with expiration {Expiration}", key, expiration);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize value for key {Key}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value for key {Key}", key);

            // Re-throw to ensure the caller knows the operation failed
            // Idempotency failures should be visible to prevent duplicate processing
            throw;
        }
    }

    /// <summary>
    /// Creates a Redis key with the configured prefix.
    /// </summary>
    /// <param name="key">The original key.</param>
    /// <returns>The prefixed Redis key.</returns>
    private string GetRedisKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? key
            : $"{_options.KeyPrefix}:{key}";
    }

    /// <summary>
    /// Throws an exception if the instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RedisIdempotencyStore));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogDebug("Redis idempotency store disposed");
        }

        return ValueTask.CompletedTask;
    }
}
