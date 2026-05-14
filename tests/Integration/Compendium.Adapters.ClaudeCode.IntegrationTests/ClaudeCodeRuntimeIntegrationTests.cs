// -----------------------------------------------------------------------
// <copyright file="ClaudeCodeRuntimeIntegrationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.ClaudeCode.IntegrationTests;

/// <summary>
/// Integration tests that drive the real <c>claude</c> CLI as a subprocess.
/// Every test is gated by <see cref="ClaudeCliFactAttribute"/>, which skips
/// the test when the CLI is not installed (no Anthropic credentials required
/// for <c>--version</c>; the real-run test additionally requires the CLI to
/// be configured/authenticated).
/// </summary>
public class ClaudeCodeRuntimeIntegrationTests
{
    [ClaudeCliFact]
    public async Task RunAsync_against_real_cli_emits_terminal_done_event()
    {
        var runtime = new ClaudeCodeRuntime();
        var tmp = Path.Combine(Path.GetTempPath(), "claude-it-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        try
        {
            var options = new CodingAgentRuntimeOptions
            {
                Engine = ClaudeCodeRuntimeDefaults.EngineId,
                Sandbox = new SandboxOptions { Kind = SandboxKind.LocalProcess, WorkingDirectory = tmp },
                Parameters = new Dictionary<string, object?>
                {
                    [ClaudeCodeRuntimeDefaults.ParamMaxTurns] = 1,
                    [ClaudeCodeRuntimeDefaults.ParamDisallowedTools] = "Bash,Write,Edit",
                },
            };
            var request = new CodingAgentRunRequest
            {
                Prompt = "Reply with exactly the single word: pong",
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var events = new List<CodingAgentStreamEvent>();
            await foreach (var ev in runtime.RunAsync(options, request, cts.Token))
            {
                events.Add(ev);
                if (events.Count > 500)
                {
                    break;
                }
            }

            events.Should().NotBeEmpty();
            events.Last().Should().BeOfType<CodingAgentStreamEvent.Done>(
                "every run must terminate with exactly one Done event");
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }
}
