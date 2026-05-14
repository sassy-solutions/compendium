// -----------------------------------------------------------------------
// <copyright file="FakeSandbox.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CodingAgents.Tests.Fakes;

internal sealed class FakeSandbox : IAgentSandbox
{
    private readonly Result _startResult;

    public FakeSandbox(string workingDirectory = "/tmp/agent", Result? startResult = null)
    {
        WorkingDirectory = workingDirectory;
        _startResult = startResult ?? Result.Success();
    }

    public SandboxKind Kind => SandboxKind.LocalProcess;

    public string WorkingDirectory { get; }

    public bool Disposed { get; private set; }

    public bool Started { get; private set; }

    public Task<Result> StartAsync(SandboxOptions options, CancellationToken cancellationToken = default)
    {
        Started = true;
        return Task.FromResult(_startResult);
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
