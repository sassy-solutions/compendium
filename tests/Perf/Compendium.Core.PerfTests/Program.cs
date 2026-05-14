// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Running;

namespace Compendium.Core.PerfTests;

/// <summary>
/// Entry point for the benchmark runner.
/// </summary>
/// <remarks>
/// Run all benchmarks: <c>dotnet run -c Release --project tests/Perf/Compendium.Core.PerfTests</c>
/// Run a single class: <c>dotnet run -c Release --project tests/Perf/Compendium.Core.PerfTests -- --filter '*ValueObject*'</c>
/// </remarks>
public static class Program
{
    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments passed to BenchmarkDotNet's switcher.</param>
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
