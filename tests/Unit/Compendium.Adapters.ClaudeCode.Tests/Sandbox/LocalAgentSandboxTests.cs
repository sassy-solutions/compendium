// -----------------------------------------------------------------------
// <copyright file="LocalAgentSandboxTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.ClaudeCode.Tests.Sandbox;

public sealed class LocalAgentSandboxTests : IAsyncDisposable
{
    private readonly string _root;

    public LocalAgentSandboxTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "compendium-localsandbox-" + Guid.NewGuid().ToString("N"));
    }

    public ValueTask DisposeAsync()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task StartAsync_creates_working_directory_when_missing()
    {
        await using var sb = new LocalAgentSandbox();
        var result = await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });

        result.IsSuccess.Should().BeTrue();
        Directory.Exists(_root).Should().BeTrue();
        sb.WorkingDirectory.Should().Be(_root);
    }

    [Fact]
    public async Task WriteFile_then_ReadFile_round_trips_content()
    {
        await using var sb = new LocalAgentSandbox();
        await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });

        var write = await sb.WriteFileAsync("nested/file.txt", "hello world");
        write.IsSuccess.Should().BeTrue();

        var read = await sb.ReadFileAsync("nested/file.txt");
        read.IsSuccess.Should().BeTrue();
        read.Value.Should().Be("hello world");
    }

    [Fact]
    public async Task ReadFile_returns_not_found_when_missing()
    {
        await using var sb = new LocalAgentSandbox();
        await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });

        var read = await sb.ReadFileAsync("missing.txt");

        read.IsFailure.Should().BeTrue();
        read.Error.Code.Should().Be("sandbox.file_not_found");
    }

    [Fact]
    public async Task EditFile_replaces_unique_substring()
    {
        await using var sb = new LocalAgentSandbox();
        await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });
        await sb.WriteFileAsync("f.txt", "alpha beta gamma");

        var result = await sb.EditFileAsync("f.txt", "beta", "BETA");

        result.IsSuccess.Should().BeTrue();
        (await sb.ReadFileAsync("f.txt")).Value.Should().Be("alpha BETA gamma");
    }

    [Fact]
    public async Task EditFile_fails_when_substring_is_ambiguous()
    {
        await using var sb = new LocalAgentSandbox();
        await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });
        await sb.WriteFileAsync("f.txt", "foo foo");

        var result = await sb.EditFileAsync("f.txt", "foo", "bar");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("sandbox.edit_ambiguous");
    }

    [Fact]
    public async Task ExecBash_runs_a_command_and_captures_stdout()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Skip — /bin/bash not available on Windows.
        }

        await using var sb = new LocalAgentSandbox();
        await sb.StartAsync(new SandboxOptions { WorkingDirectory = _root });

        var result = await sb.ExecBashAsync("echo hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.ExitCode.Should().Be(0);
        result.Value.Stdout.Trim().Should().Be("hello");
    }
}
