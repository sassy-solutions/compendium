// -----------------------------------------------------------------------
// <copyright file="TestableClaudeCodeRuntime.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Compendium.Adapters.ClaudeCode.Tests.Runtime;

/// <summary>
/// Wraps <see cref="ClaudeCodeRuntime"/> by exposing a canned line stream and
/// recording the resolved <c>CliCommand</c> + environment. Lets tests drive
/// the full base-class pipeline without actually spawning <c>claude</c>.
/// </summary>
internal sealed class TestableClaudeCodeRuntime : ClaudeCodeRuntime
{
    private readonly IReadOnlyList<CliStreamLine> _lines;

    public TestableClaudeCodeRuntime(IReadOnlyList<CliStreamLine> lines, IAgentSandbox? sandbox = null)
        : base(_ => sandbox ?? new RecordingSandbox())
    {
        _lines = lines;
    }

    public CliCommand? LastCommand { get; private set; }

    public IReadOnlyDictionary<string, string>? LastEnvironment { get; private set; }

    protected override async IAsyncEnumerable<CliStreamLine> SpawnAndStreamLinesAsync(
        CliCommand command,
        IReadOnlyDictionary<string, string> environment,
        IAgentSandbox sandbox,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastCommand = command;
        LastEnvironment = environment;
        foreach (var line in _lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
            await Task.Yield();
        }
    }
}

internal sealed class RecordingSandbox : IAgentSandbox
{
    public bool Started { get; private set; }

    public bool Disposed { get; private set; }

    public SandboxKind Kind => SandboxKind.LocalProcess;

    public string WorkingDirectory { get; private set; } = "/tmp/agent";

    public Task<Result> StartAsync(SandboxOptions options, CancellationToken cancellationToken = default)
    {
        Started = true;
        if (!string.IsNullOrEmpty(options.WorkingDirectory))
        {
            WorkingDirectory = options.WorkingDirectory;
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<SandboxResult>> ExecBashAsync(string command, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result<string>> ReadFileAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Result> EditFileAsync(string path, string oldText, string newText, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
