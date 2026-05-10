// -----------------------------------------------------------------------
// <copyright file="ScenarioContext.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.LoadTests.Support;

/// <summary>
/// Simple value object carrying the user-supplied knobs that every scenario
/// can react to. Parsed once in <see cref="ArgsParser"/>.
/// </summary>
public sealed record ScenarioOptions
{
    /// <summary>
    /// The name of the scenario the user asked to run (lower-case, hyphenated).
    /// </summary>
    public required string Scenario { get; init; }

    /// <summary>
    /// Total duration of the scenario. Defaults to a short value so the suite
    /// is runnable locally without a long wait. Override with <c>--duration</c>.
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Output folder for NBomber reports and the JSON summary file.
    /// </summary>
    public string ArtifactsFolder { get; init; } = Path.Combine("artifacts", "load");

    /// <summary>
    /// When true, the scenario tries to execute a single warm-up iteration
    /// before NBomber starts collecting stats. Always true unless the caller
    /// passes <c>--no-warmup</c>.
    /// </summary>
    public bool Warmup { get; init; } = true;
}

/// <summary>
/// Tiny argv parser tailored to the small set of flags used by the load
/// tests. Avoids pulling in a full CLI library for a single executable.
/// </summary>
public static class ArgsParser
{
    /// <summary>
    /// Parses the argv array passed to <c>Main</c>. Returns <c>null</c> when
    /// the user asked for help or supplied an unknown shape (the dispatcher
    /// will print usage and exit gracefully).
    /// </summary>
    public static ScenarioOptions? Parse(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        if (args.Any(a => a is "-h" or "--help"))
        {
            return null;
        }

        string? scenario = null;
        var duration = TimeSpan.FromSeconds(15);
        var artifacts = Path.Combine("artifacts", "load");
        var warmup = true;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--scenario" when i + 1 < args.Length:
                    scenario = args[++i].Trim().ToLowerInvariant();
                    break;
                case "--duration" when i + 1 < args.Length:
                    if (TryParseDuration(args[++i], out var d))
                    {
                        duration = d;
                    }
                    break;
                case "--artifacts" when i + 1 < args.Length:
                    artifacts = args[++i];
                    break;
                case "--no-warmup":
                    warmup = false;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(scenario))
        {
            return null;
        }

        return new ScenarioOptions
        {
            Scenario = scenario,
            Duration = duration,
            ArtifactsFolder = artifacts,
            Warmup = warmup,
        };
    }

    /// <summary>
    /// Parses a duration of the form <c>30s</c>, <c>2m</c>, <c>500ms</c> or a
    /// plain integer (interpreted as seconds).
    /// </summary>
    private static bool TryParseDuration(string raw, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim().ToLowerInvariant();

        if (trimmed.EndsWith("ms", StringComparison.Ordinal) &&
            int.TryParse(trimmed[..^2], out var ms))
        {
            value = TimeSpan.FromMilliseconds(ms);
            return true;
        }

        if (trimmed.EndsWith('s') && int.TryParse(trimmed[..^1], out var s))
        {
            value = TimeSpan.FromSeconds(s);
            return true;
        }

        if (trimmed.EndsWith('m') && int.TryParse(trimmed[..^1], out var m))
        {
            value = TimeSpan.FromMinutes(m);
            return true;
        }

        if (int.TryParse(trimmed, out var seconds))
        {
            value = TimeSpan.FromSeconds(seconds);
            return true;
        }

        return false;
    }
}
