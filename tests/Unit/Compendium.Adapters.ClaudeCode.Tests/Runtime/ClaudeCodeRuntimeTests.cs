// -----------------------------------------------------------------------
// <copyright file="ClaudeCodeRuntimeTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.ClaudeCode.Tests.Runtime;

public class ClaudeCodeRuntimeTests
{
    private static CodingAgentRuntimeOptions Options(
        Dictionary<string, object?>? parameters = null,
        Dictionary<string, string>? auth = null,
        SandboxOptions? sandbox = null) => new()
    {
        Engine = ClaudeCodeRuntimeDefaults.EngineId,
        Parameters = parameters ?? new Dictionary<string, object?>(),
        Sandbox = sandbox ?? new SandboxOptions { Kind = SandboxKind.LocalProcess, WorkingDirectory = "/tmp/agent" },
        Auth = auth ?? new Dictionary<string, string>(),
    };

    private static CodingAgentRunRequest Request(string prompt = "fix the bug") => new()
    {
        Prompt = prompt,
    };

    [Fact]
    public void Engine_id_is_claude_code()
    {
        new ClaudeCodeRuntime().Engine.Should().Be("claude-code");
    }

    [Fact]
    public async Task RunAsync_translates_stream_json_assistant_text_into_output_event()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout,
                """{"type":"system","subtype":"init","session_id":"s1"}"""),
            new CliStreamLine(CliStream.Stdout,
                """{"type":"assistant","message":{"content":[{"type":"text","text":"hello world"}]}}"""),
            new CliStreamLine(CliStream.Stdout,
                """{"type":"result","subtype":"success","is_error":false,"result":"done"}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Output>()
            .Which.Text.Should().Be("hello world");
        var done = events[1].Should().BeOfType<CodingAgentStreamEvent.Done>().Subject;
        done.Success.Should().BeTrue();
        done.Summary.Should().Be("done");
    }

    [Fact]
    public async Task RunAsync_translates_tool_use_block_into_tool_call_event()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout,
                """{"type":"assistant","message":{"content":[{"type":"tool_use","id":"tool-1","name":"Bash","input":{"command":"ls -la"}}]}}"""),
            new CliStreamLine(CliStream.Stdout,
                """{"type":"user","message":{"content":[{"type":"tool_result","tool_use_id":"tool-1","content":"file1\nfile2","is_error":false}]}}"""),
            new CliStreamLine(CliStream.Stdout,
                """{"type":"result","subtype":"success","is_error":false}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(3);
        var toolCall = events[0].Should().BeOfType<CodingAgentStreamEvent.ToolCall>().Subject;
        toolCall.ToolName.Should().Be("Bash");
        toolCall.CallId.Should().Be("tool-1");
        toolCall.Arguments.Should().Contain("\"command\"");

        var toolResult = events[1].Should().BeOfType<CodingAgentStreamEvent.ToolResult>().Subject;
        toolResult.CallId.Should().Be("tool-1");
        toolResult.Result.Should().Contain("file1");
        toolResult.IsError.Should().BeFalse();

        events[2].Should().BeOfType<CodingAgentStreamEvent.Done>()
            .Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_marks_done_failed_when_result_is_error()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout,
                """{"type":"result","subtype":"error_max_turns","is_error":true,"result":"turn limit"}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().ContainSingle();
        var done = events[0].Should().BeOfType<CodingAgentStreamEvent.Done>().Subject;
        done.Success.Should().BeFalse();
        done.Summary.Should().Be("turn limit");
    }

    [Fact]
    public async Task RunAsync_routes_stderr_to_error_event_with_classified_code()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stderr, "Invalid API key provided"),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        var err = events[0].Should().BeOfType<CodingAgentStreamEvent.Error>().Subject;
        err.Code.Should().Be(ClaudeCodeStreamParser.CodeAuth);
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>()
            .Which.Success.Should().BeFalse();
    }

    [Theory]
    [InlineData("Rate limit exceeded, please retry", "rate_limit")]
    [InlineData("Got HTTP 429", "rate_limit")]
    [InlineData("Connection refused: ECONNREFUSED", "network")]
    [InlineData("getaddrinfo ENOTFOUND api.anthropic.com", "network")]
    [InlineData("Request timeout after 30000ms", "network")]
    [InlineData("Unauthorized: missing credentials", "auth_failed")]
    [InlineData("something else entirely", "stderr")]
    public void Stderr_classifier_maps_known_failure_modes(string text, string expected)
    {
        ClaudeCodeStreamParser.ClassifyStderr(text).Should().Be(expected);
    }

    [Fact]
    public async Task RunAsync_returns_engine_mismatch_for_other_engine()
    {
        var runtime = new TestableClaudeCodeRuntime(Array.Empty<CliStreamLine>());
        var options = Options() with { Engine = "codex" };

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(options, Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Error>()
            .Which.Code.Should().Be("engine_mismatch");
    }

    [Fact]
    public async Task BuildCommand_emits_default_executable_and_stream_json_args()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, """{"type":"result","subtype":"success","is_error":false}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);

        await foreach (var _ in runtime.RunAsync(Options(), Request("write tests")))
        {
        }

        runtime.LastCommand.Should().NotBeNull();
        runtime.LastCommand!.Executable.Should().Be("claude");
        runtime.LastCommand.Arguments.Should().StartWith(new[] { "--print", "--output-format", "stream-json", "--verbose" });
        runtime.LastCommand.Arguments.Should().EndWith(new[] { "write tests" });
    }

    [Fact]
    public async Task BuildCommand_passes_through_known_parameters()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, """{"type":"result","subtype":"success","is_error":false}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);
        var options = Options(parameters: new Dictionary<string, object?>
        {
            [ClaudeCodeRuntimeDefaults.ParamExecutable] = "/usr/local/bin/claude",
            [ClaudeCodeRuntimeDefaults.ParamModel] = "sonnet",
            [ClaudeCodeRuntimeDefaults.ParamMaxTurns] = 5,
            [ClaudeCodeRuntimeDefaults.ParamAllowedTools] = "Bash,Read",
            [ClaudeCodeRuntimeDefaults.ParamMcpConfig] = "/tmp/mcp.json",
            [ClaudeCodeRuntimeDefaults.ParamPermissionMode] = "acceptEdits",
        });
        var request = new CodingAgentRunRequest
        {
            Prompt = "do work",
            SystemPrompt = "be terse",
            SessionId = "sess-1",
        };

        await foreach (var _ in runtime.RunAsync(options, request))
        {
        }

        var args = runtime.LastCommand!.Arguments;
        runtime.LastCommand.Executable.Should().Be("/usr/local/bin/claude");
        args.Should().Contain("--model").And.Contain("sonnet");
        args.Should().Contain("--max-turns").And.Contain("5");
        args.Should().Contain("--allowedTools").And.Contain("Bash,Read");
        args.Should().Contain("--mcp-config").And.Contain("/tmp/mcp.json");
        args.Should().Contain("--permission-mode").And.Contain("acceptEdits");
        args.Should().Contain("--append-system-prompt").And.Contain("be terse");
        args.Should().Contain("--resume").And.Contain("sess-1");
        args.Last().Should().Be("do work");
    }

    [Fact]
    public async Task BuildEnvironment_forwards_anthropic_api_key()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, """{"type":"result","subtype":"success","is_error":false}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines);
        var options = Options(auth: new Dictionary<string, string>
        {
            [ClaudeCodeRuntimeDefaults.AuthAnthropicApiKey] = "sk-test-123",
        });

        await foreach (var _ in runtime.RunAsync(options, Request()))
        {
        }

        runtime.LastEnvironment.Should().NotBeNull();
        runtime.LastEnvironment![ClaudeCodeRuntimeDefaults.AuthAnthropicApiKey].Should().Be("sk-test-123");
    }

    [Fact]
    public async Task RunAsync_disposes_sandbox_on_completion()
    {
        var sandbox = new RecordingSandbox();
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, """{"type":"result","subtype":"success","is_error":false}"""),
        };
        var runtime = new TestableClaudeCodeRuntime(lines, sandbox);

        await foreach (var _ in runtime.RunAsync(Options(), Request()))
        {
        }

        sandbox.Started.Should().BeTrue();
        sandbox.Disposed.Should().BeTrue();
    }
}
