// -----------------------------------------------------------------------
// <copyright file="IAgentSandbox.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Sandbox;

/// <summary>
/// An isolated environment in which a coding agent CLI runs. Provides
/// command execution and basic file operations against the sandbox's working
/// tree. Implementations may target a local working directory, a container,
/// or a Kubernetes pod.
/// </summary>
/// <remarks>
/// The sandbox lifecycle is: <see cref="StartAsync"/> -> any number of
/// exec / file calls -> <see cref="DisposeAsync"/>. Implementations must
/// release all backing resources (containers, pods, temp directories) on
/// dispose, even when previous calls failed.
/// </remarks>
public interface IAgentSandbox : IAsyncDisposable
{
    /// <summary>
    /// Gets the kind of sandbox this instance represents.
    /// </summary>
    SandboxKind Kind { get; }

    /// <summary>
    /// Gets the working directory the sandbox is rooted at, after
    /// <see cref="StartAsync"/> has resolved any relative or templated paths.
    /// </summary>
    string WorkingDirectory { get; }

    /// <summary>
    /// Provisions the sandbox according to <paramref name="options"/>. Must be
    /// called exactly once before any exec/file operations.
    /// </summary>
    /// <param name="options">The provisioning options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A success result, or a failure with a provisioning error.</returns>
    Task<Result> StartAsync(SandboxOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a bash command line inside the sandbox.
    /// </summary>
    /// <param name="command">The command line to execute (e.g. <c>"ls -la"</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="SandboxResult"/> with stdout/stderr/exit-code on success,
    /// or a failure if the sandbox itself was unable to dispatch the command.
    /// A non-zero exit code is reported as a successful <see cref="Result{T}"/>
    /// — callers inspect <see cref="SandboxResult.ExitCode"/>.
    /// </returns>
    Task<Result<SandboxResult>> ExecBashAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a UTF-8 text file from the sandbox.
    /// </summary>
    /// <param name="path">A path relative to the sandbox working directory.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file content, or a not-found / IO failure.</returns>
    Task<Result<string>> ReadFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a UTF-8 text file in the sandbox, creating parent directories
    /// as needed and overwriting any existing file.
    /// </summary>
    /// <param name="path">A path relative to the sandbox working directory.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A success result, or a failure with an IO error.</returns>
    Task<Result> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a unique substring inside an existing file. Implementations
    /// must fail if <paramref name="oldText"/> appears zero or more than one
    /// time, to preserve the safety guarantees of the underlying tool calls.
    /// </summary>
    /// <param name="path">A path relative to the sandbox working directory.</param>
    /// <param name="oldText">The exact substring to replace; must occur exactly once.</param>
    /// <param name="newText">The replacement text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A success result, or a failure if the file does not exist or the substring is not unique.</returns>
    Task<Result> EditFileAsync(string path, string oldText, string newText, CancellationToken cancellationToken = default);
}
