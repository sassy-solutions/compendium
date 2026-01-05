// -----------------------------------------------------------------------
// <copyright file="PollyResiliencePipelineFactory.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Compendium.Infrastructure.Resilience;

/// <summary>
/// Factory for creating Polly resilience pipelines with circuit breaker, retry, and timeout policies.
/// Provides pre-configured pipelines for common infrastructure components like PostgreSQL and Redis.
/// </summary>
/// <remarks>
/// This factory creates resilience pipelines using Polly 8.x ResiliencePipeline API.
/// Each pipeline combines:
/// - Timeout policy: Cancels operations that exceed the configured duration
/// - Retry policy: Retries transient failures with exponential backoff
/// - Circuit breaker: Prevents cascading failures by temporarily blocking requests
///
/// Example usage:
/// <code>
/// var factory = new PollyResiliencePipelineFactory(loggerFactory, telemetryListener);
/// var pipeline = factory.CreatePostgreSqlPipeline();
/// var result = await pipeline.ExecuteAsync(async ct => await DoWorkAsync(ct), cancellationToken);
/// </code>
/// </remarks>
public sealed class PollyResiliencePipelineFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ResilienceTelemetryListener? _telemetryListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResiliencePipelineFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    /// <param name="telemetryListener">Optional telemetry listener for metrics collection.</param>
    public PollyResiliencePipelineFactory(
        ILoggerFactory loggerFactory,
        ResilienceTelemetryListener? telemetryListener = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _telemetryListener = telemetryListener;
    }

    /// <summary>
    /// Creates a resilience pipeline optimized for PostgreSQL operations.
    /// </summary>
    /// <param name="options">Optional configuration options. Uses defaults if not provided.</param>
    /// <returns>A configured resilience pipeline for PostgreSQL.</returns>
    public ResiliencePipeline CreatePostgreSqlPipeline(PollyResilienceOptions? options = null)
    {
        var opts = options ?? PollyResilienceOptions.PostgreSqlDefaults();
        var logger = _loggerFactory.CreateLogger<PollyResiliencePipelineFactory>();

        var builder = new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = opts.Timeout,
                OnTimeout = args =>
                {
                    logger.LogWarning("PostgreSQL operation timed out after {Timeout}ms",
                        opts.Timeout.TotalMilliseconds);
                    return default;
                }
            });

        // Only add retry strategy if MaxRetryAttempts > 0
        if (opts.MaxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opts.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = opts.BaseRetryDelay,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    IsTransientPostgreSqlException(ex)),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "PostgreSQL operation failed, attempt {AttemptNumber}/{MaxAttempts}. Retrying after {Delay}ms",
                        args.AttemptNumber, opts.MaxRetryAttempts, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            });
        }

        return builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = opts.CircuitBreakerFailureThreshold,
            SamplingDuration = opts.CircuitBreakerSamplingDuration,
            MinimumThroughput = opts.CircuitBreakerMinimumThroughput,
            BreakDuration = opts.CircuitBreakerBreakDuration,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                IsTransientPostgreSqlException(ex)),
            OnOpened = args =>
            {
                logger.LogError(args.Outcome.Exception,
                    "PostgreSQL circuit breaker opened after {FailureCount} failures. Break duration: {BreakDuration}s",
                    args.Outcome.Exception?.Message ?? "unknown",
                    opts.CircuitBreakerBreakDuration.TotalSeconds);
                return default;
            },
            OnClosed = args =>
            {
                logger.LogInformation("PostgreSQL circuit breaker closed. Service recovered");
                return default;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("PostgreSQL circuit breaker half-opened. Testing service recovery");
                return default;
            }
        })
            .Build();
    }

    /// <summary>
    /// Creates a resilience pipeline optimized for Redis operations.
    /// </summary>
    /// <param name="options">Optional configuration options. Uses defaults if not provided.</param>
    /// <returns>A configured resilience pipeline for Redis.</returns>
    public ResiliencePipeline CreateRedisPipeline(PollyResilienceOptions? options = null)
    {
        var opts = options ?? PollyResilienceOptions.RedisDefaults();
        var logger = _loggerFactory.CreateLogger<PollyResiliencePipelineFactory>();

        var builder = new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = opts.Timeout,
                OnTimeout = args =>
                {
                    logger.LogWarning("Redis operation timed out after {Timeout}ms",
                        opts.Timeout.TotalMilliseconds);
                    return default;
                }
            });

        // Only add retry strategy if MaxRetryAttempts > 0
        if (opts.MaxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opts.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = opts.BaseRetryDelay,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    IsTransientRedisException(ex)),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Redis operation failed, attempt {AttemptNumber}/{MaxAttempts}. Retrying after {Delay}ms",
                        args.AttemptNumber, opts.MaxRetryAttempts, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            });
        }

        return builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = opts.CircuitBreakerFailureThreshold,
            SamplingDuration = opts.CircuitBreakerSamplingDuration,
            MinimumThroughput = opts.CircuitBreakerMinimumThroughput,
            BreakDuration = opts.CircuitBreakerBreakDuration,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                IsTransientRedisException(ex)),
            OnOpened = args =>
            {
                logger.LogError(args.Outcome.Exception,
                    "Redis circuit breaker opened after {FailureCount} failures. Break duration: {BreakDuration}s",
                    args.Outcome.Exception?.Message ?? "unknown",
                    opts.CircuitBreakerBreakDuration.TotalSeconds);
                return default;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Redis circuit breaker closed. Service recovered");
                return default;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("Redis circuit breaker half-opened. Testing service recovery");
                return default;
            }
        })
            .Build();
    }

    /// <summary>
    /// Determines if an exception from PostgreSQL is transient and should be retried.
    /// </summary>
    private static bool IsTransientPostgreSqlException(Exception exception)
    {
        // Npgsql transient exceptions that indicate temporary failures
        var exceptionType = exception.GetType().Name;

        return exceptionType switch
        {
            "NpgsqlException" => IsTransientNpgsqlError(exception),
            "TimeoutException" => true,
            "SocketException" => true,
            "IOException" => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if an NpgsqlException has a transient error code.
    /// </summary>
    private static bool IsTransientNpgsqlError(Exception exception)
    {
        // Common transient PostgreSQL error codes:
        // 08000 - connection_exception
        // 08003 - connection_does_not_exist
        // 08006 - connection_failure
        // 53000 - insufficient_resources
        // 53300 - too_many_connections
        // 57P03 - cannot_connect_now
        var message = exception.Message;

        return message.Contains("08000") ||
               message.Contains("08003") ||
               message.Contains("08006") ||
               message.Contains("53000") ||
               message.Contains("53300") ||
               message.Contains("57P03") ||
               message.Contains("connection") ||
               message.Contains("timeout");
    }

    /// <summary>
    /// Determines if an exception from Redis is transient and should be retried.
    /// </summary>
    private static bool IsTransientRedisException(Exception exception)
    {
        var exceptionType = exception.GetType().Name;

        return exceptionType switch
        {
            "RedisConnectionException" => true,
            "RedisTimeoutException" => true,
            "RedisServerException" => exception.Message.Contains("READONLY") || exception.Message.Contains("LOADING"),
            "TimeoutException" => true,
            "SocketException" => true,
            "IOException" => true,
            _ => false
        };
    }
}

/// <summary>
/// Configuration options for Polly resilience pipelines.
/// </summary>
public sealed class PollyResilienceOptions
{
    /// <summary>
    /// Gets or initializes the timeout for operations.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or initializes the maximum number of retry attempts.
    /// Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Gets or initializes the base delay for exponential backoff retry.
    /// Default is 100 milliseconds.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or initializes the failure ratio threshold for circuit breaker.
    /// Default is 0.5 (50% failures).
    /// </summary>
    public double CircuitBreakerFailureThreshold { get; init; } = 0.5;

    /// <summary>
    /// Gets or initializes the sampling duration for circuit breaker failure calculation.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or initializes the minimum throughput required before circuit breaker activates.
    /// Default is 10 requests.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; init; } = 10;

    /// <summary>
    /// Gets or initializes the duration to keep circuit breaker open.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Creates default options optimized for PostgreSQL operations.
    /// </summary>
    public static PollyResilienceOptions PostgreSqlDefaults() => new()
    {
        Timeout = TimeSpan.FromSeconds(5),
        MaxRetryAttempts = 3,
        BaseRetryDelay = TimeSpan.FromMilliseconds(100),
        CircuitBreakerFailureThreshold = 0.5,
        CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
        CircuitBreakerMinimumThroughput = 10,
        CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60)
    };

    /// <summary>
    /// Creates default options optimized for Redis operations.
    /// </summary>
    public static PollyResilienceOptions RedisDefaults() => new()
    {
        Timeout = TimeSpan.FromSeconds(2),  // Redis operations should be faster
        MaxRetryAttempts = 3,
        BaseRetryDelay = TimeSpan.FromMilliseconds(50),  // Shorter delay for cache
        CircuitBreakerFailureThreshold = 0.5,
        CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
        CircuitBreakerMinimumThroughput = 20,  // Higher throughput for cache
        CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30)  // Shorter break for cache
    };
}

/// <summary>
/// Telemetry listener for resilience pipeline events.
/// Collects metrics for monitoring circuit breaker state, retry attempts, and timeouts.
/// </summary>
public sealed class ResilienceTelemetryListener
{
    private readonly ILogger<ResilienceTelemetryListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTelemetryListener"/> class.
    /// </summary>
    /// <param name="logger">Logger for telemetry events.</param>
    public ResilienceTelemetryListener(ILogger<ResilienceTelemetryListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs a circuit breaker state change event.
    /// </summary>
    public void OnCircuitBreakerStateChange(string pipelineName, string state)
    {
        _logger.LogInformation("Circuit breaker '{Pipeline}' state changed to {State}",
            pipelineName, state);
    }

    /// <summary>
    /// Logs a retry attempt event.
    /// </summary>
    public void OnRetry(string pipelineName, int attemptNumber, TimeSpan delay)
    {
        _logger.LogWarning("Pipeline '{Pipeline}' retry attempt {AttemptNumber}, delay: {Delay}ms",
            pipelineName, attemptNumber, delay.TotalMilliseconds);
    }

    /// <summary>
    /// Logs a timeout event.
    /// </summary>
    public void OnTimeout(string pipelineName, TimeSpan timeout)
    {
        _logger.LogError("Pipeline '{Pipeline}' operation timed out after {Timeout}ms",
            pipelineName, timeout.TotalMilliseconds);
    }
}
