// -----------------------------------------------------------------------
// <copyright file="KubernetesAgentSandbox.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.Text;
using Compendium.Abstractions.CodingAgents.Sandbox;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.Kubernetes.Sandbox;

/// <summary>
/// <see cref="IAgentSandbox"/> backed by an ephemeral Kubernetes pod. Each
/// instance owns exactly one pod: <see cref="StartAsync"/> creates it and waits
/// for <c>Running</c>; subsequent calls run inside it through
/// <c>kubectl exec</c>-equivalent API streams; <see cref="DisposeAsync"/>
/// deletes the pod (best-effort) so resources are released even if the caller
/// faulted.
/// </summary>
public sealed class KubernetesAgentSandbox : IAgentSandbox
{
    private const int CommandNotFoundExitCode = 127;
    private const string SandboxErrorPrefix = "sandbox.kubernetes";

    private readonly IKubernetes _client;
    private readonly KubernetesSandboxOptions _adapterOptions;
    private readonly ILogger<KubernetesAgentSandbox> _logger;
    private readonly bool _ownsClient;

    private V1Pod? _pod;
    private SandboxOptions? _runOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesAgentSandbox"/> class.
    /// </summary>
    /// <param name="client">The Kubernetes API client.</param>
    /// <param name="adapterOptions">The adapter-level defaults.</param>
    /// <param name="logger">An optional logger.</param>
    /// <param name="ownsClient">Whether this sandbox should dispose <paramref name="client"/> on dispose.</param>
    public KubernetesAgentSandbox(
        IKubernetes client,
        KubernetesSandboxOptions adapterOptions,
        ILogger<KubernetesAgentSandbox>? logger = null,
        bool ownsClient = false)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _adapterOptions = adapterOptions ?? throw new ArgumentNullException(nameof(adapterOptions));
        _logger = logger ?? NullLogger<KubernetesAgentSandbox>.Instance;
        _ownsClient = ownsClient;
    }

    /// <inheritdoc />
    public SandboxKind Kind => SandboxKind.KubernetesPod;

    /// <inheritdoc />
    public string WorkingDirectory => _runOptions?.WorkingDirectory
        ?? _adapterOptions.WorkingDirectory;

    /// <summary>
    /// Gets the name of the pod backing this sandbox, or <see langword="null"/>
    /// if <see cref="StartAsync"/> has not yet been called.
    /// </summary>
    public string? PodName => _pod?.Metadata?.Name;

    /// <summary>
    /// Gets the namespace of the pod backing this sandbox, or <see langword="null"/>
    /// if <see cref="StartAsync"/> has not yet been called.
    /// </summary>
    public string? PodNamespace => _pod?.Metadata?.NamespaceProperty;

    /// <inheritdoc />
    public async Task<Result> StartAsync(SandboxOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_pod is not null)
        {
            return Result.Failure(SandboxError("already_started", "StartAsync was called more than once on the same sandbox."));
        }

        _runOptions = options;
        var podName = BuildPodName(_adapterOptions.PodNamePrefix);
        var spec = PodSpecFactory.Build(podName, _adapterOptions, options);
        var ns = spec.Metadata.NamespaceProperty;

        V1Pod created;
        try
        {
            created = await _client.CoreV1
                .CreateNamespacedPodAsync(spec, ns, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            return Result.Failure(SandboxError("pod_create_failed", $"create pod '{podName}' in '{ns}' failed: {ex.Response?.ReasonPhrase ?? ex.Message}"));
        }

        _pod = created;
        var readyResult = await WaitForPodRunningAsync(created.Metadata.Name, ns, cancellationToken).ConfigureAwait(false);
        if (readyResult.IsFailure)
        {
            return readyResult;
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<SandboxResult>> ExecBashAsync(string command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var notReady = EnsureReady();
        if (notReady is not null)
        {
            return Result.Failure<SandboxResult>(notReady);
        }

        var sw = Stopwatch.StartNew();
        var timeout = _runOptions?.CommandTimeout ?? _adapterOptions.DefaultCommandTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var argv = new[] { "/bin/sh", "-lc", command };

        var execResult = await ExecRawAsync(argv, cts.Token, cancellationToken).ConfigureAwait(false);
        sw.Stop();
        if (execResult.IsFailure)
        {
            return Result.Failure<SandboxResult>(execResult.Error);
        }

        var (exitCode, stdout, stderr, timedOut) = execResult.Value;
        return Result.Success(new SandboxResult
        {
            Command = command,
            ExitCode = exitCode,
            Stdout = stdout,
            Stderr = stderr,
            Duration = sw.Elapsed,
            TimedOut = timedOut,
        });
    }

    /// <inheritdoc />
    public async Task<Result<string>> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var notReady = EnsureReady();
        if (notReady is not null)
        {
            return Result.Failure<string>(notReady);
        }

        var abs = ResolveAbsolute(path);
        // Use base64 to round-trip arbitrary bytes through the multiplexed stream
        // and to dodge shell-locale issues that mangle non-utf8 sequences.
        var argv = new[] { "/bin/sh", "-lc", $"base64 -w0 -- {ShellQuote(abs)}" };
        var execResult = await ExecRawAsync(argv, cancellationToken, cancellationToken).ConfigureAwait(false);
        if (execResult.IsFailure)
        {
            return Result.Failure<string>(execResult.Error);
        }

        var (exitCode, stdout, stderr, _) = execResult.Value;
        if (exitCode == CommandNotFoundExitCode)
        {
            return Result.Failure<string>(SandboxError("toolchain_missing", $"'base64' is not available inside the sandbox image: {stderr.Trim()}"));
        }

        if (exitCode != 0)
        {
            var lowered = stderr.ToLowerInvariant();
            if (lowered.Contains("no such file") || lowered.Contains("not found"))
            {
                return Result.Failure<string>(SandboxError("file_not_found", $"file '{abs}' does not exist in the sandbox."));
            }

            return Result.Failure<string>(SandboxError("read_failed", $"read '{abs}' failed with exit {exitCode}: {stderr.Trim()}"));
        }

        try
        {
            var bytes = Convert.FromBase64String(stdout.Trim());
            return Result.Success(Encoding.UTF8.GetString(bytes));
        }
        catch (FormatException ex)
        {
            return Result.Failure<string>(SandboxError("decode_failed", $"base64 decode of '{abs}' failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(content);
        var notReady = EnsureReady();
        if (notReady is not null)
        {
            return Result.Failure(notReady);
        }

        var abs = ResolveAbsolute(path);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        // mkdir -p then write atomically through a temp file to avoid partial reads.
        var script = $"set -e; dir=$(dirname -- {ShellQuote(abs)}); mkdir -p \"$dir\"; tmp=$(mktemp -- \"$dir/.compendium.XXXXXX\"); printf %s {ShellQuote(encoded)} | base64 -d > \"$tmp\"; mv -- \"$tmp\" {ShellQuote(abs)}";
        var argv = new[] { "/bin/sh", "-lc", script };
        var execResult = await ExecRawAsync(argv, cancellationToken, cancellationToken).ConfigureAwait(false);
        if (execResult.IsFailure)
        {
            return Result.Failure(execResult.Error);
        }

        var (exitCode, _, stderr, _) = execResult.Value;
        return exitCode == 0
            ? Result.Success()
            : Result.Failure(SandboxError("write_failed", $"write '{abs}' failed with exit {exitCode}: {stderr.Trim()}"));
    }

    /// <inheritdoc />
    public async Task<Result> EditFileAsync(string path, string oldText, string newText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(oldText);
        ArgumentNullException.ThrowIfNull(newText);

        var readResult = await ReadFileAsync(path, cancellationToken).ConfigureAwait(false);
        if (readResult.IsFailure)
        {
            return Result.Failure(readResult.Error);
        }

        var original = readResult.Value;
        var first = original.IndexOf(oldText, StringComparison.Ordinal);
        if (first < 0)
        {
            return Result.Failure(SandboxError("edit_no_match", $"substring not found in '{path}'."));
        }

        var second = original.IndexOf(oldText, first + 1, StringComparison.Ordinal);
        if (second >= 0)
        {
            return Result.Failure(SandboxError("edit_not_unique", $"substring matches more than once in '{path}'; refusing to edit non-uniquely."));
        }

        var updated = string.Concat(original.AsSpan(0, first), newText, original.AsSpan(first + oldText.Length));
        return await WriteFileAsync(path, updated, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_pod is { Metadata: { Name: { } name, NamespaceProperty: { } ns } })
        {
            try
            {
                await _client.CoreV1
                    .DeleteNamespacedPodAsync(
                        name,
                        ns,
                        new V1DeleteOptions
                        {
                            GracePeriodSeconds = _adapterOptions.DeleteGracePeriodSeconds,
                            PropagationPolicy = "Background",
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "best-effort delete of pod {Namespace}/{Name} failed", ns, name);
            }
        }

        if (_ownsClient && _client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private Error? EnsureReady()
    {
        if (_disposed)
        {
            return SandboxError("disposed", "the sandbox has already been disposed.");
        }

        if (_pod is null || _runOptions is null)
        {
            return SandboxError("not_started", "StartAsync must be called before any other operation.");
        }

        return null;
    }

    private async Task<Result<(int ExitCode, string Stdout, string Stderr, bool TimedOut)>> ExecRawAsync(
        IList<string> command,
        CancellationToken execToken,
        CancellationToken externalToken)
    {
        var pod = _pod!;
        var name = pod.Metadata.Name;
        var ns = pod.Metadata.NamespaceProperty;
        var container = _adapterOptions.ContainerName;

        using var stdoutBuffer = new MemoryStream();
        using var stderrBuffer = new MemoryStream();

        var stdoutLock = new object();
        var stderrLock = new object();

        try
        {
            var exitCode = await _client
                .NamespacedPodExecAsync(
                    name,
                    ns,
                    container,
                    command,
                    tty: false,
                    action: async (Stream stdIn, Stream stdOut, Stream stdErr) =>
                    {
                        await Task.WhenAll(
                            CopyToBufferAsync(stdOut, stdoutBuffer, stdoutLock, execToken),
                            CopyToBufferAsync(stdErr, stderrBuffer, stderrLock, execToken))
                            .ConfigureAwait(false);
                    },
                    cancellationToken: execToken)
                .ConfigureAwait(false);

            return Result.Success((
                exitCode,
                Encoding.UTF8.GetString(stdoutBuffer.ToArray()),
                Encoding.UTF8.GetString(stderrBuffer.ToArray()),
                false));
        }
        catch (OperationCanceledException) when (execToken.IsCancellationRequested && !externalToken.IsCancellationRequested)
        {
            return Result.Success((
                ExitCode: 124,
                Stdout: Encoding.UTF8.GetString(stdoutBuffer.ToArray()),
                Stderr: Encoding.UTF8.GetString(stderrBuffer.ToArray()),
                TimedOut: true));
        }
        catch (HttpOperationException ex)
        {
            return Result.Failure<(int, string, string, bool)>(SandboxError("exec_failed", $"exec on {ns}/{name} failed: {ex.Response?.ReasonPhrase ?? ex.Message}"));
        }
    }

    private static async Task CopyToBufferAsync(Stream source, MemoryStream sink, object syncRoot, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        while (true)
        {
            int read;
            try
            {
                read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            if (read <= 0)
            {
                return;
            }

            lock (syncRoot)
            {
                sink.Write(buffer, 0, read);
            }
        }
    }

    private async Task<Result> WaitForPodRunningAsync(string name, string ns, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + _adapterOptions.PodReadyTimeout;
        var delay = TimeSpan.FromMilliseconds(250);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            V1Pod current;
            try
            {
                current = await _client.CoreV1.ReadNamespacedPodAsync(name, ns, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpOperationException ex)
            {
                return Result.Failure(SandboxError("pod_read_failed", $"read pod {ns}/{name} failed: {ex.Response?.ReasonPhrase ?? ex.Message}"));
            }

            _pod = current;
            var phase = current.Status?.Phase;
            if (string.Equals(phase, "Running", StringComparison.Ordinal))
            {
                return Result.Success();
            }

            if (string.Equals(phase, "Failed", StringComparison.Ordinal) ||
                string.Equals(phase, "Succeeded", StringComparison.Ordinal))
            {
                return Result.Failure(SandboxError("pod_terminal", $"pod {ns}/{name} reached terminal phase '{phase}' before becoming Running."));
            }

            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // Exponential backoff with a 2-second cap, so short-lived pods come up fast
            // but we don't hammer the API server on slow image pulls.
            if (delay < TimeSpan.FromSeconds(2))
            {
                delay = delay + delay;
            }
        }

        return Result.Failure(SandboxError("pod_ready_timeout", $"pod {ns}/{name} did not become Running within {_adapterOptions.PodReadyTimeout.TotalSeconds:F0}s."));
    }

    private string ResolveAbsolute(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var basePath = WorkingDirectory.TrimEnd('/');
        return $"{basePath}/{path.Replace('\\', '/').TrimStart('/')}";
    }

    private static string ShellQuote(string value)
    {
        // POSIX-safe single quoting: ' -> '\'' and wrap in single quotes.
        return "'" + value.Replace("'", "'\\''") + "'";
    }

    internal static string BuildPodName(string prefix)
    {
        var sanitized = string.Concat((prefix ?? string.Empty).ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-'));
        if (sanitized.Length == 0)
        {
            sanitized = "coding-agent";
        }

        var suffix = Guid.NewGuid().ToString("N").AsSpan(0, 16).ToString();
        var name = $"{sanitized}-{suffix}";
        return name.Length <= 63 ? name : name.Substring(0, 63);
    }

    private static Error SandboxError(string code, string message)
        => Error.Failure($"{SandboxErrorPrefix}.{code}", message);
}
