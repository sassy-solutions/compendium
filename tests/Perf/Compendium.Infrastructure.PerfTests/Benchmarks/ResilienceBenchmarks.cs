// -----------------------------------------------------------------------
// <copyright file="ResilienceBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Compendium.Core.Results;
using Compendium.Infrastructure.Resilience;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.PerfTests.Benchmarks;

/// <summary>CircuitBreaker and RetryPolicy throughput.</summary>
[MemoryDiagnoser]
public class ResilienceBenchmarks
{
    private CircuitBreaker _circuitBreaker = null!;
    private RetryPolicy _retryPolicy = null!;
    private Func<Task<Result>> _successOp = null!;
    private Func<Task<Result<string>>> _successOpString = null!;

    [GlobalSetup]
    public void Setup()
    {
        var cbOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenTimeout = TimeSpan.FromMilliseconds(100),
        };
        _circuitBreaker = new CircuitBreaker(cbOptions, NullLogger<CircuitBreaker>.Instance);

        var retryOptions = new RetryOptions
        {
            MaxRetries = 1,
            DelayStrategy = new FixedDelayStrategy(TimeSpan.FromMilliseconds(1)),
        };
        _retryPolicy = new RetryPolicy(retryOptions, NullLogger<RetryPolicy>.Instance);

        _successOp = () => Task.FromResult(Result.Success());
        _successOpString = () => Task.FromResult(Result.Success("ok"));
    }

    [Benchmark]
    public async Task<Result> CircuitBreaker_ExecuteAsync_Success()
        => await _circuitBreaker.ExecuteAsync(_successOp);

    [Benchmark]
    public async Task<Result<string>> RetryPolicy_ExecuteAsync_Success()
        => await _retryPolicy.ExecuteAsync(_successOpString);
}
