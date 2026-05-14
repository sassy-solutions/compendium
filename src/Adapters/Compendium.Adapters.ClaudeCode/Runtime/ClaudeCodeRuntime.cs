// -----------------------------------------------------------------------
// <copyright file="ClaudeCodeRuntime.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Compendium.Adapters.ClaudeCode.Sandbox;

namespace Compendium.Adapters.ClaudeCode.Runtime;

/// <summary>
/// <see cref="ICodingAgentRuntime"/> implementation that drives the
/// <c>claude</c> CLI. Composes the <see cref="CliCodingAgentRuntime"/>
/// base: builds the argv from <see cref="CodingAgentRuntimeOptions.Parameters"/>,
/// parses the CLI's <c>stream-json</c> NDJSON output via
/// <see cref="ClaudeCodeStreamParser"/>, and provisions an
/// <see cref="IAgentSandbox"/> appropriate for the requested
/// <see cref="SandboxKind"/>.
/// </summary>
/// <remarks>
/// Sandbox selection is driven by <see cref="CodingAgentRuntimeOptions.Sandbox"/>:
/// <list type="bullet">
///   <item><see cref="SandboxKind.None"/> / <see cref="SandboxKind.LocalProcess"/> — uses <see cref="LocalAgentSandbox"/>;</item>
///   <item><see cref="SandboxKind.KubernetesPod"/> — caller injects a factory via the constructor.</item>
/// </list>
/// The runtime sets <c>ANTHROPIC_API_KEY</c> automatically when supplied via
/// <see cref="CodingAgentRuntimeOptions.Auth"/>.
/// </remarks>
public class ClaudeCodeRuntime : CliCodingAgentRuntime
{
    private readonly Func<CodingAgentRuntimeOptions, IAgentSandbox>? _sandboxFactory;

    /// <summary>
    /// Creates a runtime that uses a <see cref="LocalAgentSandbox"/> for any
    /// sandbox kind. Suitable for tests and local development.
    /// </summary>
    public ClaudeCodeRuntime()
    {
    }

    /// <summary>
    /// Creates a runtime that delegates sandbox provisioning to the supplied
    /// factory. Hosts use this constructor to inject a Kubernetes-pod sandbox
    /// (or any other implementation) without coupling the adapter to that
    /// package.
    /// </summary>
    /// <param name="sandboxFactory">Factory invoked once per run with the resolved options.</param>
    public ClaudeCodeRuntime(Func<CodingAgentRuntimeOptions, IAgentSandbox> sandboxFactory)
    {
        ArgumentNullException.ThrowIfNull(sandboxFactory);
        _sandboxFactory = sandboxFactory;
    }

    /// <inheritdoc />
    public override string Engine => ClaudeCodeRuntimeDefaults.EngineId;

    /// <inheritdoc />
    protected override CliCommand BuildCommand(
        CodingAgentRuntimeOptions options,
        CodingAgentRunRequest request)
    {
        var executable = StringParam(options, ClaudeCodeRuntimeDefaults.ParamExecutable)
            ?? ClaudeCodeRuntimeDefaults.DefaultExecutable;

        var args = new List<string>
        {
            "--print",
            "--output-format",
            "stream-json",
            "--verbose",
        };

        if (StringParam(options, ClaudeCodeRuntimeDefaults.ParamModel) is { Length: > 0 } model)
        {
            args.Add("--model");
            args.Add(model);
        }

        if (IntParam(options, ClaudeCodeRuntimeDefaults.ParamMaxTurns) is { } maxTurns)
        {
            args.Add("--max-turns");
            args.Add(maxTurns.ToString(CultureInfo.InvariantCulture));
        }

        if (StringParam(options, ClaudeCodeRuntimeDefaults.ParamAllowedTools) is { Length: > 0 } allowed)
        {
            args.Add("--allowedTools");
            args.Add(allowed);
        }

        if (StringParam(options, ClaudeCodeRuntimeDefaults.ParamDisallowedTools) is { Length: > 0 } denied)
        {
            args.Add("--disallowedTools");
            args.Add(denied);
        }

        if (StringParam(options, ClaudeCodeRuntimeDefaults.ParamMcpConfig) is { Length: > 0 } mcp)
        {
            args.Add("--mcp-config");
            args.Add(mcp);
        }

        if (StringParam(options, ClaudeCodeRuntimeDefaults.ParamPermissionMode) is { Length: > 0 } mode)
        {
            args.Add("--permission-mode");
            args.Add(mode);
        }

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            args.Add("--append-system-prompt");
            args.Add(request.SystemPrompt);
        }

        if (!string.IsNullOrEmpty(request.SessionId))
        {
            args.Add("--resume");
            args.Add(request.SessionId);
        }

        args.Add(request.Prompt);

        return new CliCommand(executable, args);
    }

    /// <inheritdoc />
    protected override CodingAgentStreamEvent? ParseStreamLine(CliStreamLine line)
        => ClaudeCodeStreamParser.Parse(line);

    /// <inheritdoc />
    protected override IAgentSandbox CreateSandbox(CodingAgentRuntimeOptions options)
    {
        if (_sandboxFactory is not null)
        {
            return _sandboxFactory(options);
        }

        return new LocalAgentSandbox();
    }

    private static string? StringParam(CodingAgentRuntimeOptions options, string key)
    {
        if (!options.Parameters.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            _ => value.ToString(),
        };
    }

    private static int? IntParam(CodingAgentRuntimeOptions options, string key)
    {
        if (!options.Parameters.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }
}
