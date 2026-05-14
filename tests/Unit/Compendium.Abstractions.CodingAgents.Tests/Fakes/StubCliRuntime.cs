// -----------------------------------------------------------------------
// <copyright file="StubCliRuntime.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Compendium.Abstractions.CodingAgents.Tests.Fakes;

/// <summary>
/// Test double for <see cref="CliCodingAgentRuntime"/>: parses NDJSON-style
/// lines like <c>{"type":"output","text":"hi"}</c> and supports overriding
/// the spawned line stream so tests do not actually fork a process.
/// </summary>
internal sealed class StubCliRuntime : CliCodingAgentRuntime
{
    private readonly IReadOnlyList<CliStreamLine> _lines;
    private readonly FakeSandbox _sandbox;

    public StubCliRuntime(IReadOnlyList<CliStreamLine> lines, FakeSandbox sandbox)
    {
        _lines = lines;
        _sandbox = sandbox;
    }

    public override string Engine => "stub-cli";

    public CliCommand? LastCommand { get; private set; }

    public IReadOnlyDictionary<string, string>? LastEnvironment { get; private set; }

    public FakeSandbox Sandbox => _sandbox;

    protected override CliCommand BuildCommand(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request)
    {
        var cmd = new CliCommand("stub", new[] { "--prompt", request.Prompt });
        LastCommand = cmd;
        return cmd;
    }

    protected override IReadOnlyDictionary<string, string> BuildEnvironment(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request)
    {
        var env = base.BuildEnvironment(options, request);
        LastEnvironment = env;
        return env;
    }

    protected override IAgentSandbox CreateSandbox(CodingAgentRuntimeOptions options) => _sandbox;

    protected override CodingAgentStreamEvent? ParseStreamLine(CliStreamLine line)
    {
        if (line.Stream == CliStream.Stderr)
        {
            return new CodingAgentStreamEvent.Error(line.Text, Code: "stderr");
        }

        if (string.IsNullOrWhiteSpace(line.Text))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(line.Text);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString();
        return type switch
        {
            "output" => new CodingAgentStreamEvent.Output(root.GetProperty("text").GetString() ?? string.Empty),
            "tool_call" => new CodingAgentStreamEvent.ToolCall(
                root.GetProperty("name").GetString() ?? string.Empty,
                root.GetProperty("args").GetRawText(),
                root.GetProperty("id").GetString() ?? string.Empty),
            "tool_result" => new CodingAgentStreamEvent.ToolResult(
                CallId: root.GetProperty("id").GetString() ?? string.Empty,
                Result: root.GetProperty("output").GetString() ?? string.Empty,
                IsError: root.TryGetProperty("error", out var err) && err.GetBoolean()),
            "done" => new CodingAgentStreamEvent.Done(
                Success: root.GetProperty("success").GetBoolean(),
                ExitCode: root.TryGetProperty("exit", out var exit) ? exit.GetInt32() : null,
                Summary: root.TryGetProperty("summary", out var s) ? s.GetString() : null),
            _ => null,
        };
    }

    protected override async IAsyncEnumerable<CliStreamLine> SpawnAndStreamLinesAsync(
        CliCommand command,
        IReadOnlyDictionary<string, string> environment,
        IAgentSandbox sandbox,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var line in _lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
            await Task.Yield();
        }
    }
}
