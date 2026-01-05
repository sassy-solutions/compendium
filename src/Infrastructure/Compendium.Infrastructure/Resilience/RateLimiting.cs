using System.Collections.Concurrent;

namespace Compendium.Infrastructure.Resilience;

/// <summary>
/// Provides rate limiting functionality to control the rate of requests or operations.
/// Helps prevent system overload and ensures fair resource usage.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Executes an operation with rate limiting based on the provided key.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="key">The rate limiting key (e.g., user ID, IP address).</param>
    /// <param name="operation">The operation to execute if rate limit allows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result<T>> ExecuteAsync<T>(string key, Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with rate limiting based on the provided key.
    /// </summary>
    /// <param name="key">The rate limiting key (e.g., user ID, IP address).</param>
    /// <param name="operation">The operation to execute if rate limit allows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result> ExecuteAsync(string key, Func<Task<Result>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a request with the given key is allowed under the current rate limit.
    /// </summary>
    /// <param name="key">The rate limiting key to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean indicating if allowed.</returns>
    Task<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Token bucket algorithm implementation of rate limiter.
/// Maintains a bucket of tokens for each key, refilling at a steady rate and consuming tokens for requests.
/// </summary>
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly ILogger<TokenBucketRateLimiter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBucketRateLimiter"/> class.
    /// </summary>
    /// <param name="options">The rate limiting options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public TokenBucketRateLimiter(RateLimitOptions options, ILogger<TokenBucketRateLimiter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with rate limiting based on the provided key.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="key">The rate limiting key (e.g., user ID, IP address).</param>
    /// <param name="operation">The operation to execute if rate limit allows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    public async Task<Result<T>> ExecuteAsync<T>(string key, Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Result.Failure<T>(Error.Validation("RateLimit.KeyEmpty", "Rate limit key cannot be null or empty"));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var isAllowed = await IsAllowedAsync(key, cancellationToken);
        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key {Key}", key);
            return Result.Failure<T>(Error.TooManyRequests("RateLimit.Exceeded", $"Rate limit exceeded for key {key}"));
        }

        return await operation();
    }

    /// <summary>
    /// Executes an operation with rate limiting based on the provided key.
    /// </summary>
    /// <param name="key">The rate limiting key (e.g., user ID, IP address).</param>
    /// <param name="operation">The operation to execute if rate limit allows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    public async Task<Result> ExecuteAsync(string key, Func<Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        var wrappedOperation = async () =>
        {
            var result = await operation();
            return result.IsSuccess ? Result.Success<object?>(null) : Result.Failure<object?>(result.Error);
        };

        var wrappedResult = await ExecuteAsync(key, wrappedOperation, cancellationToken);
        return wrappedResult.IsSuccess ? Result.Success() : Result.Failure(wrappedResult.Error);
    }

    /// <summary>
    /// Checks if a request with the given key is allowed under the current rate limit.
    /// </summary>
    /// <param name="key">The rate limiting key to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean indicating if allowed.</returns>
    public Task<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(false);
        }

        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(_options));
        var isAllowed = bucket.TryConsume();

        _logger.LogDebug("Rate limit check for key {Key}: {Result}", key, isAllowed ? "Allowed" : "Denied");
        return Task.FromResult(isAllowed);
    }
}

/// <summary>
/// Configuration options for rate limiting behavior.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    /// Gets or initializes the maximum number of requests allowed in the time window.
    /// Default is 100.
    /// </summary>
    public int MaxRequests { get; init; } = 100;

    /// <summary>
    /// Gets or initializes the time window for rate limiting.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or initializes the interval at which tokens are refilled in the bucket.
    /// Default is 1 second.
    /// </summary>
    public TimeSpan RefillInterval { get; init; } = TimeSpan.FromSeconds(1);
}

internal sealed class TokenBucket
{
    private readonly RateLimitOptions _options;
    private readonly object _lock = new();
    private int _tokens;
    private DateTime _lastRefill;

    public TokenBucket(RateLimitOptions options)
    {
        _options = options;
        _tokens = options.MaxRequests;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            Refill();

            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }

            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = now - _lastRefill;

        if (timeSinceLastRefill >= _options.RefillInterval)
        {
            var intervalsElapsed = (int)(timeSinceLastRefill.TotalMilliseconds / _options.RefillInterval.TotalMilliseconds);
            var tokensToAdd = (int)((double)_options.MaxRequests / (_options.Window.TotalMilliseconds / _options.RefillInterval.TotalMilliseconds)) * intervalsElapsed;

            _tokens = Math.Min(_tokens + tokensToAdd, _options.MaxRequests);
            _lastRefill = now;
        }
    }
}
