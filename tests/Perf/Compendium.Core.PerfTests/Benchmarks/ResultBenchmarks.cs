// -----------------------------------------------------------------------
// <copyright file="ResultBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Compendium.Core.Results;

namespace Compendium.Core.PerfTests.Benchmarks;

/// <summary>Result, Error, and Result&lt;T&gt; extension benchmarks.</summary>
[MemoryDiagnoser]
public class ResultBenchmarks
{
    private Error _error = null!;
    private Result<string> _success = null!;

    [GlobalSetup]
    public void Setup()
    {
        _error = Error.Validation("VAL_001", "Validation failed");
        _success = Result.Success("test value");
    }

    [Benchmark]
    public Result Result_Success_NonGeneric() => Result.Success();

    [Benchmark]
    public Result<string> Result_Success_Generic() => Result.Success("value");

    [Benchmark]
    public Result Result_Failure_NonGeneric() => Result.Failure(_error);

    [Benchmark]
    public Result<string> Result_Failure_Generic() => Result.Failure<string>(_error);

    [Benchmark]
    public Error Error_Validation() => Error.Validation("VAL.001", "Validation error");

    [Benchmark]
    public Error Error_Failure() => Error.Failure("FAIL.001", "Failure error");

    [Benchmark]
    public Error Error_NotFound() => Error.NotFound("NF.001", "Not found error");

    [Benchmark]
    public string ResultExtensions_MapBindTapMatch_Chain()
        => _success
            .Map(v => v.Length)
            .Bind(len => Result.Success(len * 2))
            .Tap(_ => { })
            .Match(
                onSuccess: val => val.ToString(),
                onFailure: err => err.Message);
}
