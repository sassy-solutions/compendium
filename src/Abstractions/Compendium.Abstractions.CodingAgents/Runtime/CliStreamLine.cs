// -----------------------------------------------------------------------
// <copyright file="CliStreamLine.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// A single line read from a spawned CLI's output, tagged with the stream
/// it came from. Passed to <see cref="CliCodingAgentRuntime.ParseStreamLine"/>
/// so adapters can decide how to translate stdout vs. stderr.
/// </summary>
/// <param name="Stream">Which output stream the line came from.</param>
/// <param name="Text">The line text, without the trailing newline.</param>
public readonly record struct CliStreamLine(CliStream Stream, string Text);

/// <summary>
/// The output stream of a CLI process.
/// </summary>
public enum CliStream
{
    /// <summary>Standard output.</summary>
    Stdout = 0,

    /// <summary>Standard error.</summary>
    Stderr = 1,
}
