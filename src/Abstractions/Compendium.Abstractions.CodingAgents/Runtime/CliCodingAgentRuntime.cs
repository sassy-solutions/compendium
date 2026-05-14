// -----------------------------------------------------------------------
// <copyright file="CliCodingAgentRuntime.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Compendium.Abstractions.CodingAgents.Events;
using Compendium.Abstractions.CodingAgents.Sandbox;

namespace Compendium.Abstractions.CodingAgents.Runtime;

/// <summary>
/// Template-method base for runtimes that drive a CLI-based coding agent
/// (Claude Code, Codex, Cursor CLI, ...). The base orchestrates:
/// <list type="number">
///   <item>provision the sandbox via <see cref="CreateSandbox"/> + <see cref="IAgentSandbox.StartAsync"/>;</item>
///   <item>build the command line via <see cref="BuildCommand"/>;</item>
///   <item>build the environment via <see cref="BuildEnvironment"/>;</item>
///   <item>spawn the CLI via <see cref="SpawnAndStreamLinesAsync"/> (overridable for tests / non-process sandboxes);</item>
///   <item>parse each output line via <see cref="ParseStreamLine"/>;</item>
///   <item>emit a terminal <see cref="CodingAgentStreamEvent.Done"/>;</item>
///   <item>dispose the sandbox.</item>
/// </list>
/// </summary>
public abstract class CliCodingAgentRuntime : ICodingAgentRuntime
{
    /// <inheritdoc />
    public abstract string Engine { get; }

    /// <inheritdoc />
    public async IAsyncEnumerable<CodingAgentStreamEvent> RunAsync(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(options.Engine, Engine, StringComparison.OrdinalIgnoreCase))
        {
            yield return new CodingAgentStreamEvent.Error(
                $"Runtime '{Engine}' cannot handle engine '{options.Engine}'.",
                Code: "engine_mismatch");
            yield return new CodingAgentStreamEvent.Done(Success: false);
            yield break;
        }

        IAgentSandbox? sandbox = null;
        string? createError = null;
        try
        {
            sandbox = CreateSandbox(options);
        }
        catch (Exception ex)
        {
            createError = ex.Message;
        }

        if (sandbox is null)
        {
            yield return new CodingAgentStreamEvent.Error(createError ?? "sandbox is null", Code: "sandbox_create_failed");
            yield return new CodingAgentStreamEvent.Done(Success: false);
            yield break;
        }

        await using (sandbox.ConfigureAwait(false))
        {
            var startResult = await sandbox.StartAsync(options.Sandbox, cancellationToken).ConfigureAwait(false);
            if (startResult.IsFailure)
            {
                yield return new CodingAgentStreamEvent.Error(
                    startResult.Error.Message,
                    Code: startResult.Error.Code);
                yield return new CodingAgentStreamEvent.Done(Success: false);
                yield break;
            }

            CliCommand? command = null;
            IReadOnlyDictionary<string, string>? environment = null;
            string? buildError = null;
            try
            {
                command = BuildCommand(options, request);
                environment = BuildEnvironment(options, request);
            }
            catch (Exception ex)
            {
                buildError = ex.Message;
            }

            if (command is null || environment is null)
            {
                yield return new CodingAgentStreamEvent.Error(buildError ?? "command/environment is null", Code: "build_command_failed");
                yield return new CodingAgentStreamEvent.Done(Success: false);
                yield break;
            }

            int? exitCode = null;
            var fatal = false;
            await foreach (var line in SpawnAndStreamLinesAsync(command, environment, sandbox, cancellationToken)
                .ConfigureAwait(false))
            {
                CodingAgentStreamEvent? evt;
                string? parseError = null;
                try
                {
                    evt = ParseStreamLine(line);
                }
                catch (Exception ex)
                {
                    evt = null;
                    parseError = ex.Message;
                }

                if (parseError is not null)
                {
                    yield return new CodingAgentStreamEvent.Error(parseError, Code: "parse_failed");
                    continue;
                }

                if (evt is null)
                {
                    continue;
                }

                if (evt is CodingAgentStreamEvent.Done done)
                {
                    exitCode = done.ExitCode ?? exitCode;
                    fatal = !done.Success;
                    yield return done;
                    yield break;
                }

                if (evt is CodingAgentStreamEvent.Error)
                {
                    fatal = true;
                }

                yield return evt;
            }

            yield return new CodingAgentStreamEvent.Done(Success: !fatal, ExitCode: exitCode);
        }
    }

    /// <summary>
    /// Builds the CLI invocation for a given run. Implementations translate
    /// <see cref="CodingAgentRuntimeOptions.Parameters"/> + the prompt into
    /// concrete argv.
    /// </summary>
    /// <param name="options">The runtime options.</param>
    /// <param name="request">The per-call request.</param>
    /// <returns>The executable + argument vector to spawn.</returns>
    protected abstract CliCommand BuildCommand(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request);

    /// <summary>
    /// Parses a single line of CLI output into a neutral stream event. Return
    /// <see langword="null"/> to skip the line (e.g. blank lines or banners).
    /// Returning a <see cref="CodingAgentStreamEvent.Done"/> ends the stream.
    /// </summary>
    /// <param name="line">A single line from stdout or stderr.</param>
    /// <returns>The translated event, or <see langword="null"/> to skip.</returns>
    protected abstract CodingAgentStreamEvent? ParseStreamLine(CliStreamLine line);

    /// <summary>
    /// Builds the environment variables passed to the CLI. The default merges
    /// <see cref="CodingAgentRuntimeOptions.Auth"/> with any
    /// <see cref="SandboxOptions.Environment"/>; auth overrides sandbox.
    /// </summary>
    /// <param name="options">The runtime options.</param>
    /// <param name="request">The per-call request.</param>
    /// <returns>The merged environment.</returns>
    protected virtual IReadOnlyDictionary<string, string> BuildEnvironment(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request)
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        if (options.Sandbox.Environment is { } sandboxEnv)
        {
            foreach (var kv in sandboxEnv)
            {
                merged[kv.Key] = kv.Value;
            }
        }

        foreach (var kv in options.Auth)
        {
            merged[kv.Key] = kv.Value;
        }

        return merged;
    }

    /// <summary>
    /// Creates the sandbox the agent will run inside. Adapters override to
    /// return their concrete implementation (local working dir, container,
    /// Kubernetes pod). The base does not provide a default — there are no
    /// concrete sandbox implementations in the abstractions package.
    /// </summary>
    /// <param name="options">The runtime options.</param>
    /// <returns>An un-started sandbox; the runtime calls <see cref="IAgentSandbox.StartAsync"/>.</returns>
    protected abstract IAgentSandbox CreateSandbox(CodingAgentRuntimeOptions options);

    /// <summary>
    /// Spawns the CLI and streams its stdout/stderr lines. The default
    /// implementation uses <see cref="System.Diagnostics.Process"/> rooted
    /// at <paramref name="sandbox"/>'s working directory; override for
    /// non-host sandboxes (e.g. <c>kubectl exec</c>) or for tests.
    /// </summary>
    /// <param name="command">The command to spawn.</param>
    /// <param name="environment">The environment to apply.</param>
    /// <param name="sandbox">The provisioned sandbox; supplies <see cref="IAgentSandbox.WorkingDirectory"/>.</param>
    /// <param name="cancellationToken">A cancellation token. Cancellation kills the process.</param>
    /// <returns>An async stream of output lines.</returns>
    protected virtual async IAsyncEnumerable<CliStreamLine> SpawnAndStreamLinesAsync(
        CliCommand command,
        IReadOnlyDictionary<string, string> environment,
        IAgentSandbox sandbox,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command.Executable,
            WorkingDirectory = sandbox.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in command.Arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        foreach (var kv in environment)
        {
            psi.Environment[kv.Key] = kv.Value;
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var channel = Channel.CreateUnbounded<CliStreamLine>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                channel.Writer.TryWrite(new CliStreamLine(CliStream.Stdout, e.Data));
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                channel.Writer.TryWrite(new CliStreamLine(CliStream.Stderr, e.Data));
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var waitTask = Task.Run(async () =>
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, CancellationToken.None);

        using var killReg = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // best-effort
            }
        });

        await foreach (var line in channel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
        {
            yield return line;
        }

        await waitTask.ConfigureAwait(false);
    }
}
