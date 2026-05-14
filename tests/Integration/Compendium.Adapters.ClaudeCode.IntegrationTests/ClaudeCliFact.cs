// -----------------------------------------------------------------------
// <copyright file="ClaudeCliFact.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Compendium.Adapters.ClaudeCode.IntegrationTests;

/// <summary>
/// xUnit <c>[Fact]</c> that auto-skips when the <c>claude</c> CLI is not on
/// <c>PATH</c>. Lets the integration test suite pass on machines without the
/// CLI installed (CI, contributors who haven't configured Anthropic creds).
/// </summary>
public sealed class ClaudeCliFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> ClaudeAvailable = new(ResolveClaudeAvailability);

    public ClaudeCliFactAttribute()
    {
        if (!ClaudeAvailable.Value)
        {
            Skip = "claude CLI not installed (set CLAUDE_CLI=1 to require it).";
        }
    }

    private static bool ResolveClaudeAvailability()
    {
        try
        {
            var psi = new ProcessStartInfo("claude", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null)
            {
                return false;
            }

            return p.WaitForExit(5000) && p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
