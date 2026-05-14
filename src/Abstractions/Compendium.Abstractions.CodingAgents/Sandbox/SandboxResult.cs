// -----------------------------------------------------------------------
// <copyright file="SandboxResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Sandbox;

/// <summary>
/// The outcome of executing a single shell command inside an
/// <see cref="IAgentSandbox"/>.
/// </summary>
public sealed record SandboxResult
{
    /// <summary>
    /// Gets the command line that was executed (for diagnostics / audit).
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Gets the process exit code. Zero conventionally means success.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the captured standard output.
    /// </summary>
    public string Stdout { get; init; } = string.Empty;

    /// <summary>
    /// Gets the captured standard error.
    /// </summary>
    public string Stderr { get; init; } = string.Empty;

    /// <summary>
    /// Gets the wall-clock duration of the command.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command timed out and was forcibly
    /// terminated by the sandbox.
    /// </summary>
    public bool TimedOut { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="ExitCode"/> is zero and the
    /// command did not time out.
    /// </summary>
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}
