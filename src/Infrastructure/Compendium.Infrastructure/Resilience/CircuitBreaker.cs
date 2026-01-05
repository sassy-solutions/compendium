using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Infrastructure.Resilience;

/// <summary>
/// Provides circuit breaker functionality to prevent cascading failures in distributed systems.
/// Monitors operation failures and temporarily blocks requests when failure thresholds are exceeded.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Executes an operation that returns a result through the circuit breaker.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation that returns a simple result through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<Result> ExecuteAsync(Func<Task<Result>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    CircuitBreakerState State { get; }
}

/// <summary>
/// Defines the possible states of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// The circuit breaker is closed and allowing requests to pass through.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit breaker is open and blocking requests due to failures.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit breaker is half-open and testing if the service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Default implementation of a circuit breaker that tracks failures and manages state transitions.
/// Provides protection against cascading failures by temporarily blocking requests to failing services.
/// </summary>
public sealed class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly object _lock = new();

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _nextAttemptTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="options">The circuit breaker configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Executes an operation that returns a result through the circuit breaker.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow < _nextAttemptTime)
                {
                    _logger.LogWarning("Circuit breaker is open, rejecting request");
                    return Result.Failure<T>(Error.Unavailable("CircuitBreaker.Open", "Circuit breaker is open"));
                }

                _logger.LogInformation("Circuit breaker transitioning to half-open state");
                _state = CircuitBreakerState.HalfOpen;
            }
        }

        try
        {
            _logger.LogDebug("Executing operation through circuit breaker in {State} state", _state);
            var result = await operation();

            if (result.IsSuccess)
            {
                OnSuccess();
                return result;
            }

            if (_options.ShouldTripOnError(result.Error))
            {
                OnFailure();
            }

            return result;
        }
        catch (Exception ex) when (_options.ShouldTripOnException(ex))
        {
            _logger.LogError(ex, "Operation failed with exception that should trip circuit breaker");
            OnFailure();
            return Result.Failure<T>(Error.Failure("Operation.Failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed with exception that should not trip circuit breaker");
            return Result.Failure<T>(Error.Failure("Operation.Failed", ex.Message));
        }
    }

    /// <summary>
    /// Executes an operation that returns a simple result through the circuit breaker.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
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

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker transitioning to closed state after successful operation");
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
            }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogWarning("Circuit breaker transitioning to open state after failure in half-open state");
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow.Add(_options.OpenTimeout);
            }
            else if (_failureCount >= _options.FailureThreshold)
            {
                _logger.LogWarning("Circuit breaker transitioning to open state after {FailureCount} failures", _failureCount);
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow.Add(_options.OpenTimeout);
            }
        }
    }
}

/// <summary>
/// Configuration options for circuit breaker behavior.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or initializes the number of consecutive failures required to open the circuit.
    /// Default is 5.
    /// </summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Gets or initializes the duration to keep the circuit open before transitioning to half-open.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan OpenTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or initializes the function that determines if an error should trip the circuit breaker.
    /// </summary>
    public Func<Error, bool> ShouldTripOnError { get; init; } = DefaultShouldTripOnError;

    /// <summary>
    /// Gets or initializes the function that determines if an exception should trip the circuit breaker.
    /// </summary>
    public Func<Exception, bool> ShouldTripOnException { get; init; } = DefaultShouldTripOnException;

    private static bool DefaultShouldTripOnError(Error error) =>
        error.Type == ErrorType.Failure || error.Type == ErrorType.Unexpected;

    private static bool DefaultShouldTripOnException(Exception exception) =>
        exception is not ArgumentException and not ArgumentNullException and not InvalidOperationException;
}

/// <summary>
/// Registry for managing multiple circuit breakers by name.
/// Provides a centralized way to create and access circuit breakers across the application.
/// </summary>
public sealed class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, ICircuitBreaker> _circuitBreakers = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerRegistry"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    public CircuitBreakerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets an existing circuit breaker by name or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="name">The unique name of the circuit breaker.</param>
    /// <param name="options">Optional configuration options. Uses defaults if not provided.</param>
    /// <returns>The circuit breaker instance.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required dependencies are not registered.</exception>
    public ICircuitBreaker GetOrCreate(string name, CircuitBreakerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(name));
        }

        return _circuitBreakers.GetOrAdd(name, _ =>
        {
            var logger = _serviceProvider.GetService<ILogger<CircuitBreaker>>() ??
                         throw new InvalidOperationException("Logger<CircuitBreaker> not registered");

            return new CircuitBreaker(options ?? new CircuitBreakerOptions(), logger);
        });
    }
}
