// -----------------------------------------------------------------------
// <copyright file="LocalAgentSandbox.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Compendium.Adapters.ClaudeCode.Sandbox;

/// <summary>
/// A minimal <see cref="IAgentSandbox"/> that runs commands directly on the
/// host, rooted at a chosen working directory. Provides file-system scoping
/// but no kernel-level isolation. Intended for local development and tests
/// against the Claude Code CLI when a Kubernetes sandbox is not available.
/// </summary>
public sealed class LocalAgentSandbox : IAgentSandbox
{
    private string? _workingDirectory;

    /// <inheritdoc />
    public SandboxKind Kind => SandboxKind.LocalProcess;

    /// <inheritdoc />
    public string WorkingDirectory =>
        _workingDirectory ?? throw new InvalidOperationException("Sandbox has not been started.");

    /// <inheritdoc />
    public Task<Result> StartAsync(SandboxOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var dir = options.WorkingDirectory ?? Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(
                Error.Unavailable("sandbox.start_failed", $"Could not provision working directory '{dir}': {ex.Message}")));
        }

        _workingDirectory = dir;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result<SandboxResult>> ExecBashAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_workingDirectory is null)
        {
            return Result.Failure<SandboxResult>(Error.Failure("sandbox.not_started", "Sandbox must be started before exec."));
        }

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(command);

        using var process = new Process { StartInfo = psi };
        var sw = Stopwatch.StartNew();
        try
        {
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            sw.Stop();

            return new SandboxResult
            {
                Command = command,
                ExitCode = process.ExitCode,
                Stdout = await stdoutTask.ConfigureAwait(false),
                Stderr = await stderrTask.ConfigureAwait(false),
                Duration = sw.Elapsed,
                TimedOut = false,
            };
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); } } catch { }
            return Result.Failure<SandboxResult>(Error.Failure("sandbox.cancelled", "Command was cancelled."));
        }
        catch (Exception ex)
        {
            return Result.Failure<SandboxResult>(Error.Unexpected("sandbox.exec_failed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_workingDirectory is null)
        {
            return Result.Failure<string>(Error.Failure("sandbox.not_started", "Sandbox must be started."));
        }

        var full = Path.Combine(_workingDirectory, path);
        if (!File.Exists(full))
        {
            return Result.Failure<string>(Error.NotFound("sandbox.file_not_found", $"File '{path}' not found."));
        }

        try
        {
            var content = await File.ReadAllTextAsync(full, cancellationToken).ConfigureAwait(false);
            return content;
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Unexpected("sandbox.read_failed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        if (_workingDirectory is null)
        {
            return Result.Failure(Error.Failure("sandbox.not_started", "Sandbox must be started."));
        }

        try
        {
            var full = Path.Combine(_workingDirectory, path);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(full, content, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Unexpected("sandbox.write_failed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> EditFileAsync(string path, string oldText, string newText, CancellationToken cancellationToken = default)
    {
        var read = await ReadFileAsync(path, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return Result.Failure(read.Error);
        }

        var content = read.Value;
        var first = content.IndexOf(oldText, StringComparison.Ordinal);
        if (first < 0)
        {
            return Result.Failure(Error.NotFound("sandbox.edit_no_match", "Old text not found in file."));
        }

        var second = content.IndexOf(oldText, first + oldText.Length, StringComparison.Ordinal);
        if (second >= 0)
        {
            return Result.Failure(Error.Conflict("sandbox.edit_ambiguous", "Old text occurs more than once in file."));
        }

        var updated = string.Concat(content.AsSpan(0, first), newText, content.AsSpan(first + oldText.Length));
        return await WriteFileAsync(path, updated, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _workingDirectory = null;
        return ValueTask.CompletedTask;
    }
}
