// -----------------------------------------------------------------------
// <copyright file="RetryPolicyTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Resilience;

namespace Compendium.Infrastructure.Tests.Resilience;

/// <summary>
/// Comprehensive test suite for RetryPolicy implementation.
/// Tests retry strategies, backoff algorithms, exception handling, and performance.
/// </summary>
public sealed class RetryPolicyTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<RetryPolicy> _logger;
    private readonly RetryOptions _defaultOptions;

    public RetryPolicyTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = Substitute.For<ILogger<RetryPolicy>>();
        _defaultOptions = new RetryOptions
        {
            MaxRetries = 3,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(10)) // Fast for testing
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RetryPolicy(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RetryPolicy(_defaultOptions, null!));
    }

    #endregion

    #region Basic Retry Logic

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnTransientFailure()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return Task.FromResult(Result.Failure<string>(Error.Failure("Transient.Error", "Temporary failure")));
            }
            return Task.FromResult(Result.Success("Success after retries"));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Success after retries");
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRespectMaxAttempts()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 2,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1))
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            return Task.FromResult(Result.Failure<string>(Error.Failure("Persistent.Error", "Always fails")));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Persistent.Error");
        attemptCount.Should().Be(3); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task RetryPolicy_WithSuccessfulFirstAttempt_ShouldNotRetry()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            return Task.FromResult(Result.Success("First attempt success"));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("First attempt success");
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldUseExponentialBackoff()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 3,
            DelayStrategy = new ExponentialBackoffDelayStrategy(
                baseDelay: TimeSpan.FromMilliseconds(10),
                multiplier: 2.0,
                maxDelay: TimeSpan.FromSeconds(1))
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var attemptCount = 0;
        var delays = new List<TimeSpan>();
        var stopwatch = Stopwatch.StartNew();
        var lastTime = stopwatch.Elapsed;

        var operation = () =>
        {
            var currentTime = stopwatch.Elapsed;
            if (attemptCount > 0)
            {
                delays.Add(currentTime - lastTime);
            }
            lastTime = currentTime;
            attemptCount++;

            return Task.FromResult(Result.Failure<string>(Error.Failure("Test.Error", "Test failure")));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        attemptCount.Should().Be(4); // Initial + 3 retries
        delays.Should().HaveCount(3);

        // Verify exponential backoff (allowing for significant timing variance in CI environments)
        // CI runners can have high latency spikes, so we use very wide ranges
        delays[0].TotalMilliseconds.Should().BeInRange(5, 500); // ~10ms base delay
        delays[1].TotalMilliseconds.Should().BeInRange(10, 600); // ~20ms with exponential increase
        delays[2].TotalMilliseconds.Should().BeInRange(15, 700); // ~40ms with exponential increase

        _output.WriteLine($"Delays: {string.Join(", ", delays.Select(d => $"{d.TotalMilliseconds:F1}ms"))}");
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotRetryOnNonTransientFailure()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 3,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1)),
            ShouldRetry = error => error.Code != "NonRetryable.Error"
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            return Task.FromResult(Result.Failure<string>(Error.Validation("NonRetryable.Error", "Should not retry")));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NonRetryable.Error");
        attemptCount.Should().Be(1); // Should not retry
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnRetryableException()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Network error");
            }
            return Task.FromResult(Result.Success("Success after exception retries"));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Success after exception retries");
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotRetryOnNonRetryableException()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            throw new ArgumentNullException("param", "Invalid argument");
#pragma warning disable CS0162 // Unreachable code detected
            return Task.FromResult(Result.Success("Never reached"));
#pragma warning restore CS0162 // Unreachable code detected
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Operation.Failed");
        result.Error.Message.Should().Contain("Invalid argument");
        attemptCount.Should().Be(1); // Should not retry
    }

    [Fact]
    public async Task RetryPolicy_WithCustomExceptionFilter_ShouldRespectFilter()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 2,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1)),
            ShouldRetryException = ex => ex is InvalidOperationException
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            throw new InvalidOperationException("Should retry this");
#pragma warning disable CS0162 // Unreachable code detected
            return Task.FromResult(Result.Success("Never reached"));
#pragma warning restore CS0162 // Unreachable code detected
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Retry.MaxAttemptsExceeded");
        attemptCount.Should().Be(3); // Initial + 2 retries
    }

    #endregion

    #region Non-Generic Operations

    [Fact]
    public async Task RetryPolicy_NonGenericOperation_ShouldWorkCorrectly()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                return Task.FromResult(Result.Failure(Error.Failure("Test.Error", "Test failure")));
            }
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        attemptCount.Should().Be(2);
    }

    #endregion

    #region Delay Strategies

    [Fact]
    public void FixedDelayStrategy_ShouldReturnSameDelay()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(500);
        var strategy = new FixedDelayStrategy(delay);

        // Act & Assert
        strategy.GetDelay(1).Should().Be(delay);
        strategy.GetDelay(5).Should().Be(delay);
        strategy.GetDelay(10).Should().Be(delay);
    }

    [Fact]
    public void ExponentialBackoffDelayStrategy_ShouldIncreaseExponentially()
    {
        // Arrange
        var strategy = new ExponentialBackoffDelayStrategy(
            baseDelay: TimeSpan.FromMilliseconds(100),
            multiplier: 2.0,
            maxDelay: TimeSpan.FromSeconds(10));

        // Act
        var delay1 = strategy.GetDelay(1);
        var delay2 = strategy.GetDelay(2);
        var delay3 = strategy.GetDelay(3);
        var delay4 = strategy.GetDelay(4);

        // Assert
        delay1.TotalMilliseconds.Should().Be(100);   // 100 * 2^0
        delay2.TotalMilliseconds.Should().Be(200);   // 100 * 2^1
        delay3.TotalMilliseconds.Should().Be(400);   // 100 * 2^2
        delay4.TotalMilliseconds.Should().Be(800);   // 100 * 2^3
    }

    [Fact]
    public void ExponentialBackoffDelayStrategy_ShouldRespectMaxDelay()
    {
        // Arrange
        var strategy = new ExponentialBackoffDelayStrategy(
            baseDelay: TimeSpan.FromMilliseconds(100),
            multiplier: 2.0,
            maxDelay: TimeSpan.FromMilliseconds(300));

        // Act
        var delay1 = strategy.GetDelay(1);
        var delay2 = strategy.GetDelay(2);
        var delay3 = strategy.GetDelay(3);
        var delay4 = strategy.GetDelay(4);

        // Assert
        delay1.TotalMilliseconds.Should().Be(100);
        delay2.TotalMilliseconds.Should().Be(200);
        delay3.TotalMilliseconds.Should().Be(300); // Capped at max
        delay4.TotalMilliseconds.Should().Be(300); // Capped at max
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(3.0)]
    [InlineData(1.1)]
    public void ExponentialBackoffDelayStrategy_WithDifferentMultipliers_ShouldWork(double multiplier)
    {
        // Arrange
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var strategy = new ExponentialBackoffDelayStrategy(
            baseDelay: baseDelay,
            multiplier: multiplier,
            maxDelay: TimeSpan.FromSeconds(30));

        // Act
        var delay1 = strategy.GetDelay(1);
        var delay2 = strategy.GetDelay(2);
        var delay3 = strategy.GetDelay(3);

        // Assert
        delay1.TotalMilliseconds.Should().Be(100);
        delay2.TotalMilliseconds.Should().BeApproximately(100 * multiplier, 0.1);
        delay3.TotalMilliseconds.Should().BeApproximately(100 * Math.Pow(multiplier, 2), 0.1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RetryPolicy_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            retryPolicy.ExecuteAsync<string>(null!));
    }

    [Fact]
    public async Task RetryPolicy_WithZeroMaxRetries_ShouldExecuteOnce()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 0,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1))
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            return Task.FromResult(Result.Failure<string>(Error.Failure("Test.Error", "Always fails")));
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeFalse();
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 5,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(100))
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var cts = new CancellationTokenSource();
        var attemptCount = 0;

        var operation = () =>
        {
            attemptCount++;
            return Task.FromResult(Result.Failure<string>(Error.Failure("Test.Error", "Always fails")));
        };

        // Cancel after a short delay
        _ = Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            retryPolicy.ExecuteAsync(operation, cts.Token));

        attemptCount.Should().BeLessThan(6); // Should be cancelled before all retries
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task RetryPolicy_PerformanceTest_ShouldHandleMultipleOperations()
    {
        // Arrange
        const int operationCount = 1000;
        var options = new RetryOptions
        {
            MaxRetries = 1,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1))
        };
        var retryPolicy = new RetryPolicy(options, _logger);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, operationCount)
            .Select(i => retryPolicy.ExecuteAsync(() =>
                Task.FromResult(Result.Success($"Operation {i}"))));

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(operationCount);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        var avgTimePerOperation = (double)stopwatch.ElapsedMilliseconds / operationCount;
        _output.WriteLine($"Processed {operationCount} operations in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {avgTimePerOperation:F3}ms");

        avgTimePerOperation.Should().BeLessThan(5, "Should handle operations efficiently");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RetryPolicy_WithMixedSuccessAndFailure_ShouldHandleCorrectly()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(_defaultOptions, _logger);
        var operations = new List<Func<Task<Result<string>>>>
        {
            () => Task.FromResult(Result.Success("Success 1")),
            () => Task.FromResult(Result.Failure<string>(Error.Failure("Error.1", "Failure 1"))),
            () => Task.FromResult(Result.Success("Success 2")),
        };

        // Act
        var results = new List<Result<string>>();
        foreach (var operation in operations)
        {
            var result = await retryPolicy.ExecuteAsync(operation);
            results.Add(result);
        }

        // Assert
        results[0].IsSuccess.Should().BeTrue();
        results[0].Value.Should().Be("Success 1");

        results[1].IsSuccess.Should().BeFalse();
        results[1].Error.Code.Should().Be("Error.1");

        results[2].IsSuccess.Should().BeTrue();
        results[2].Value.Should().Be("Success 2");
    }

    [Fact]
    public async Task RetryPolicy_WithComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange
        var options = new RetryOptions
        {
            MaxRetries = 3,
            DelayStrategy = new ExponentialBackoffDelayStrategy(
                TimeSpan.FromMilliseconds(5), 2.0, TimeSpan.FromMilliseconds(50)),
            ShouldRetry = error => error.Type != ErrorType.Validation,
            ShouldRetryException = ex => ex is not ArgumentException
        };
        var retryPolicy = new RetryPolicy(options, _logger);

        var attemptCount = 0;
        var operation = () =>
        {
            attemptCount++;
            return attemptCount switch
            {
                1 => Task.FromResult(Result.Failure<string>(Error.Failure("Transient.Error", "Retry this"))),
                2 => Task.FromResult(Result.Failure<string>(Error.Failure("Another.Error", "Retry this too"))),
                3 => Task.FromResult(Result.Success("Finally succeeded")),
                _ => Task.FromResult(Result.Failure<string>(Error.Failure("Unexpected", "Should not reach here")))
            };
        };

        // Act
        var result = await retryPolicy.ExecuteAsync(operation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Finally succeeded");
        attemptCount.Should().Be(3);
    }

    #endregion
}
