namespace Compendium.Infrastructure.Resilience;

/// <summary>
/// Provides retry functionality for operations that may fail transiently.
/// Automatically retries failed operations based on configurable policies.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Executes an operation with retry logic based on the configured policy.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute with retry logic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with retry logic based on the configured policy.
    /// </summary>
    /// <param name="operation">The operation to execute with retry logic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result> ExecuteAsync(Func<Task<Result>> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of retry policy that supports configurable retry strategies,
/// delay strategies, and retry conditions.
/// </summary>
public sealed class RetryPolicy : IRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<RetryPolicy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="options">The retry configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public RetryPolicy(RetryOptions options, ILogger<RetryPolicy> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation with retry logic based on the configured policy.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute with retry logic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var attempt = 0;
        var exceptions = new List<Exception>();

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                _logger.LogDebug("Executing operation, attempt {Attempt}/{MaxAttempts}", attempt + 1, _options.MaxRetries + 1);

                var result = await operation();

                if (result.IsSuccess || !_options.ShouldRetry(result.Error))
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Operation succeeded after {Attempts} attempts", attempt + 1);
                    }
                    return result;
                }

                if (attempt == _options.MaxRetries)
                {
                    _logger.LogWarning("Operation failed after {MaxAttempts} attempts. Final error: {Error}",
                        _options.MaxRetries + 1, result.Error.Message);
                    return result;
                }

                _logger.LogWarning("Operation failed on attempt {Attempt}, retrying. Error: {Error}",
                    attempt + 1, result.Error.Message);
            }
            catch (Exception ex) when (_options.ShouldRetryException(ex))
            {
                exceptions.Add(ex);

                if (attempt == _options.MaxRetries)
                {
                    _logger.LogError(ex, "Operation failed after {MaxAttempts} attempts", _options.MaxRetries + 1);
                    return Result.Failure<T>(Error.Failure("Retry.MaxAttemptsExceeded",
                        $"Operation failed after {_options.MaxRetries + 1} attempts. Last exception: {ex.Message}"));
                }

                _logger.LogWarning(ex, "Operation failed on attempt {Attempt}, retrying", attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation failed with non-retryable exception on attempt {Attempt}", attempt + 1);
                return Result.Failure<T>(Error.Failure("Operation.Failed", ex.Message));
            }

            attempt++;

            if (attempt <= _options.MaxRetries)
            {
                var delay = _options.DelayStrategy.GetDelay(attempt);
                _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt}", delay.TotalMilliseconds, attempt + 1);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return Result.Failure<T>(Error.Failure("Retry.Unexpected", "Unexpected end of retry loop"));
    }

    /// <summary>
    /// Executes an operation with retry logic based on the configured policy.
    /// </summary>
    /// <param name="operation">The operation to execute with retry logic.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    public async Task<Result> ExecuteAsync(Func<Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        var wrappedOperation = async () =>
        {
            var result = await operation();
            return result.IsSuccess ? Result.Success<object?>(null) : Result.Failure<object?>(result.Error);
        };

        var wrappedResult = await ExecuteAsync(wrappedOperation, cancellationToken);
        return wrappedResult.IsSuccess ? Result.Success() : Result.Failure(wrappedResult.Error);
    }
}

/// <summary>
/// Configuration options for retry behavior.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Gets or initializes the maximum number of retry attempts.
    /// Default is 3.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets or initializes the delay strategy to use between retry attempts.
    /// Default is exponential backoff.
    /// </summary>
    public IDelayStrategy DelayStrategy { get; init; } = new ExponentialBackoffDelayStrategy();

    /// <summary>
    /// Gets or initializes the function that determines if an error should trigger a retry.
    /// </summary>
    public Func<Error, bool> ShouldRetry { get; init; } = DefaultShouldRetry;

    /// <summary>
    /// Gets or initializes the function that determines if an exception should trigger a retry.
    /// </summary>
    public Func<Exception, bool> ShouldRetryException { get; init; } = DefaultShouldRetryException;

    private static bool DefaultShouldRetry(Error error) =>
        error.Type == ErrorType.Failure || error.Type == ErrorType.Unexpected;

    private static bool DefaultShouldRetryException(Exception exception) =>
        exception is not ArgumentException and not ArgumentNullException and not InvalidOperationException;
}

/// <summary>
/// Defines a strategy for calculating delays between retry attempts.
/// </summary>
public interface IDelayStrategy
{
    /// <summary>
    /// Calculates the delay for the specified retry attempt.
    /// </summary>
    /// <param name="attempt">The current attempt number (1-based).</param>
    /// <returns>The delay duration before the next retry attempt.</returns>
    TimeSpan GetDelay(int attempt);
}

/// <summary>
/// A delay strategy that uses a fixed delay between all retry attempts.
/// </summary>
public sealed class FixedDelayStrategy : IDelayStrategy
{
    private readonly TimeSpan _delay;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDelayStrategy"/> class.
    /// </summary>
    /// <param name="delay">The fixed delay to use between attempts.</param>
    public FixedDelayStrategy(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <summary>
    /// Gets the fixed delay for any retry attempt.
    /// </summary>
    /// <param name="attempt">The current attempt number (ignored in this strategy).</param>
    /// <returns>The fixed delay duration.</returns>
    public TimeSpan GetDelay(int attempt) => _delay;
}

/// <summary>
/// A delay strategy that implements exponential backoff with jitter.
/// Delay increases exponentially with each retry attempt to reduce load on failing services.
/// </summary>
public sealed class ExponentialBackoffDelayStrategy : IDelayStrategy
{
    private readonly TimeSpan _baseDelay;
    private readonly double _multiplier;
    private readonly TimeSpan _maxDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffDelayStrategy"/> class.
    /// </summary>
    /// <param name="baseDelay">The base delay for the first retry. Default is 100ms.</param>
    /// <param name="multiplier">The multiplier for exponential backoff. Default is 2.0.</param>
    /// <param name="maxDelay">The maximum delay allowed. Default is 30 seconds.</param>
    public ExponentialBackoffDelayStrategy(TimeSpan? baseDelay = null, double multiplier = 2.0, TimeSpan? maxDelay = null)
    {
        _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(100);
        _multiplier = multiplier;
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Calculates an exponentially increasing delay based on the attempt number.
    /// </summary>
    /// <param name="attempt">The current attempt number (1-based).</param>
    /// <returns>The calculated delay, capped at the maximum delay.</returns>
    public TimeSpan GetDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(_multiplier, attempt - 1));
        return delay > _maxDelay ? _maxDelay : delay;
    }
}
