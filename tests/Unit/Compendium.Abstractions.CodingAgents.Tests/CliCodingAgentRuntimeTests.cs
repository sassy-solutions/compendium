// -----------------------------------------------------------------------
// <copyright file="CliCodingAgentRuntimeTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CodingAgents.Tests.Fakes;

namespace Compendium.Abstractions.CodingAgents.Tests;

public class CliCodingAgentRuntimeTests
{
    private static CodingAgentRuntimeOptions Options(string engine = "stub-cli") => new()
    {
        Engine = engine,
        Sandbox = new SandboxOptions { Kind = SandboxKind.LocalProcess, WorkingDirectory = "/tmp/agent" },
        Auth = new Dictionary<string, string> { ["API_KEY"] = "secret" },
    };

    private static CodingAgentRunRequest Request(string prompt = "fix the bug") =>
        new() { Prompt = prompt };

    [Fact]
    public async Task RunAsync_parses_output_tool_and_done_events_from_stub_lines()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"output\",\"text\":\"hello\"}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"tool_call\",\"name\":\"bash\",\"args\":{\"cmd\":\"ls\"},\"id\":\"call-1\"}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"tool_result\",\"id\":\"call-1\",\"output\":\"a b c\",\"error\":false}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"done\",\"success\":true,\"exit\":0,\"summary\":\"all good\"}"),
        };
        var sandbox = new FakeSandbox();
        var runtime = new StubCliRuntime(lines, sandbox);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(4);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Output>()
            .Which.Text.Should().Be("hello");
        var call = events[1].Should().BeOfType<CodingAgentStreamEvent.ToolCall>().Subject;
        call.ToolName.Should().Be("bash");
        call.CallId.Should().Be("call-1");
        call.Arguments.Should().Contain("\"cmd\"");
        var result = events[2].Should().BeOfType<CodingAgentStreamEvent.ToolResult>().Subject;
        result.CallId.Should().Be("call-1");
        result.IsError.Should().BeFalse();
        var done = events[3].Should().BeOfType<CodingAgentStreamEvent.Done>().Subject;
        done.Success.Should().BeTrue();
        done.ExitCode.Should().Be(0);
        done.Summary.Should().Be("all good");

        sandbox.Started.Should().BeTrue();
        sandbox.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_skips_blank_lines_and_unknown_types()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, string.Empty),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"banner\",\"version\":\"1\"}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"output\",\"text\":\"hi\"}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"done\",\"success\":true}"),
        };
        var runtime = new StubCliRuntime(lines, new FakeSandbox());

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Output>();
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>().Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_emits_synthetic_done_when_stream_ends_without_one()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"output\",\"text\":\"partial\"}"),
        };
        var runtime = new StubCliRuntime(lines, new FakeSandbox());

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>()
            .Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_routes_stderr_to_error_event_and_marks_synthetic_done_failed()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stderr, "boom"),
        };
        var runtime = new StubCliRuntime(lines, new FakeSandbox());

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        var err = events[0].Should().BeOfType<CodingAgentStreamEvent.Error>().Subject;
        err.Message.Should().Be("boom");
        err.Code.Should().Be("stderr");
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>()
            .Which.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_yields_engine_mismatch_error_when_options_target_other_engine()
    {
        var runtime = new StubCliRuntime(Array.Empty<CliStreamLine>(), new FakeSandbox());
        var options = Options(engine: "claude-code");

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(options, Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Error>()
            .Which.Code.Should().Be("engine_mismatch");
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>()
            .Which.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_propagates_sandbox_start_failure()
    {
        var startFailure = Result.Failure(Error.Unavailable("sandbox.unavailable", "k8s down"));
        var sandbox = new FakeSandbox(startResult: startFailure);
        var runtime = new StubCliRuntime(Array.Empty<CliStreamLine>(), sandbox);

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(2);
        var err = events[0].Should().BeOfType<CodingAgentStreamEvent.Error>().Subject;
        err.Code.Should().Be("sandbox.unavailable");
        err.Message.Should().Contain("k8s");
        events[1].Should().BeOfType<CodingAgentStreamEvent.Done>().Which.Success.Should().BeFalse();
        sandbox.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_stops_streaming_after_done_event()
    {
        var lines = new[]
        {
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"done\",\"success\":true}"),
            new CliStreamLine(CliStream.Stdout, "{\"type\":\"output\",\"text\":\"after-done\"}"),
        };
        var runtime = new StubCliRuntime(lines, new FakeSandbox());

        var events = new List<CodingAgentStreamEvent>();
        await foreach (var ev in runtime.RunAsync(Options(), Request()))
        {
            events.Add(ev);
        }

        events.Should().HaveCount(1);
        events[0].Should().BeOfType<CodingAgentStreamEvent.Done>();
    }

    [Fact]
    public async Task BuildEnvironment_merges_sandbox_env_with_auth_overrides()
    {
        var runtime = new StubCliRuntime(
            new[] { new CliStreamLine(CliStream.Stdout, "{\"type\":\"done\",\"success\":true}") },
            new FakeSandbox());
        var options = new CodingAgentRuntimeOptions
        {
            Engine = "stub-cli",
            Sandbox = new SandboxOptions
            {
                Kind = SandboxKind.LocalProcess,
                WorkingDirectory = "/tmp/agent",
                Environment = new Dictionary<string, string>
                {
                    ["FOO"] = "bar",
                    ["API_KEY"] = "from-sandbox",
                },
            },
            Auth = new Dictionary<string, string>
            {
                ["API_KEY"] = "from-auth",
            },
        };

        await foreach (var _ in runtime.RunAsync(options, Request()))
        {
        }

        runtime.LastEnvironment.Should().NotBeNull();
        runtime.LastEnvironment!["FOO"].Should().Be("bar");
        runtime.LastEnvironment!["API_KEY"].Should().Be("from-auth");
    }

    [Fact]
    public async Task BuildCommand_receives_prompt_and_options()
    {
        var runtime = new StubCliRuntime(
            new[] { new CliStreamLine(CliStream.Stdout, "{\"type\":\"done\",\"success\":true}") },
            new FakeSandbox());

        await foreach (var _ in runtime.RunAsync(Options(), Request("write tests")))
        {
        }

        runtime.LastCommand.Should().NotBeNull();
        runtime.LastCommand!.Executable.Should().Be("stub");
        runtime.LastCommand.Arguments.Should().Equal(new[] { "--prompt", "write tests" });
    }
}
