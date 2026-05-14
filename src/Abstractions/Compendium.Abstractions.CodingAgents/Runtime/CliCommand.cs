// -----------------------------------------------------------------------
// <copyright file="CliCommand.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// A resolved CLI invocation: the executable to run and the argument vector.
/// Used by <see cref="CliCodingAgentRuntime"/> as the output of
/// <see cref="CliCodingAgentRuntime.BuildCommand"/>.
/// </summary>
/// <param name="Executable">The executable path or name (resolved via <c>PATH</c>).</param>
/// <param name="Arguments">The argument vector (each element passed as a separate argv slot).</param>
public sealed record CliCommand(string Executable, IReadOnlyList<string> Arguments);
