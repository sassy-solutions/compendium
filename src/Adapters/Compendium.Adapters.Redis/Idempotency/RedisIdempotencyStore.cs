// -----------------------------------------------------------------------
// <copyright file="RedisIdempotencyStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Adapters.Redis.Configuration;
using Compendium.Application.Idempotency;
using Compendium.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Idempotency;

/// <summary>
/// Redis implementation of IIdempotencyStore for distributed idempotency tracking.
/// Provides high-performance, distributed storage for operation results with configurable TTL.
/// All infrastructure failures (Redis connection, serialization, timeouts) are surfaced via
/// <see cref="Result{T}"/> failures rather than exceptions, matching the Compendium Result pattern.
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
    public async Task<Result<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<bool>(Error.Validation(
                "Redis.Idempotency.InvalidKey",
                "Key cannot be null or empty"));
        }

        if (_disposed)
        {
            return Result.Failure<bool>(Error.Failure(
                "Redis.Idempotency.Disposed",
                "Idempotency store has been disposed"));
        }

        var redisKey = GetRedisKey(key);

        try
        {
            var exists = await _database.KeyExistsAsync(redisKey).ConfigureAwait(false);

            _logger.LogDebug("Checked existence for key {Key}: {Exists}", key, exists);

            return Result.Success(exists);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error checking existence for key {Key}", key);
            return Result.Failure<bool>(Error.Failure("Redis.Idempotency.ExistsFailed", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout checking existence for key {Key}", key);
            return Result.Failure<bool>(Error.Failure("Redis.Idempotency.Timeout", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking existence for key {Key}", key);
            return Result.Failure<bool>(Error.Failure("Redis.Idempotency.ExistsFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TResult?>> GetAsync<TResult>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Success<TResult?>(default);
        }

        if (_disposed)
        {
            return Result.Failure<TResult?>(Error.Failure(
                "Redis.Idempotency.Disposed",
                "Idempotency store has been disposed"));
        }

        var redisKey = GetRedisKey(key);

        try
        {
            var value = await _database.StringGetAsync(redisKey).ConfigureAwait(false);

            if (!value.HasValue)
            {
                _logger.LogDebug("No value found for key {Key}", key);
                return Result.Success<TResult?>(default);
            }

            var deserializedValue = JsonSerializer.Deserialize<TResult>(value!, _jsonOptions);

            _logger.LogDebug("Retrieved value for key {Key}", key);

            return Result.Success<TResult?>(deserializedValue);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize value for key {Key}", key);
            return Result.Failure<TResult?>(Error.Failure("Redis.Idempotency.SerializationFailed", ex.Message));
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error getting value for key {Key}", key);
            return Result.Failure<TResult?>(Error.Failure("Redis.Idempotency.GetFailed", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout getting value for key {Key}", key);
            return Result.Failure<TResult?>(Error.Failure("Redis.Idempotency.Timeout", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting value for key {Key}", key);
            return Result.Failure<TResult?>(Error.Failure("Redis.Idempotency.GetFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure(Error.Validation(
                "Redis.Idempotency.InvalidKey",
                "Key cannot be null or empty"));
        }

        if (expiration <= TimeSpan.Zero)
        {
            return Result.Failure(Error.Validation(
                "Redis.Idempotency.InvalidExpiration",
                "Expiration must be greater than zero"));
        }

        if (_disposed)
        {
            return Result.Failure(Error.Failure(
                "Redis.Idempotency.Disposed",
                "Idempotency store has been disposed"));
        }

        var redisKey = GetRedisKey(key);

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

            var success = await _database.StringSetAsync(redisKey, serializedValue, expiration).ConfigureAwait(false);

            if (!success)
            {
                _logger.LogWarning("Failed to set value for key {Key}", key);
                return Result.Failure(Error.Failure(
                    "Redis.Idempotency.SetFailed",
                    $"Redis StringSet returned false for key {key}"));
            }

            _logger.LogDebug("Set value for key {Key} with expiration {Expiration}", key, expiration);
            return Result.Success();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize value for key {Key}", key);
            return Result.Failure(Error.Failure("Redis.Idempotency.SerializationFailed", ex.Message));
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error setting value for key {Key}", key);
            return Result.Failure(Error.Failure("Redis.Idempotency.SetFailed", ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout setting value for key {Key}", key);
            return Result.Failure(Error.Failure("Redis.Idempotency.Timeout", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting value for key {Key}", key);
            return Result.Failure(Error.Failure("Redis.Idempotency.SetFailed", ex.Message));
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
