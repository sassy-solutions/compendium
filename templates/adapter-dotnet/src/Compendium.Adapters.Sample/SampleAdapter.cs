// -----------------------------------------------------------------------
// <copyright file="SampleAdapter.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Sample.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Sample;

/// <summary>
/// Sample adapter — replace with the real vendor implementation.
/// Demonstrates the canonical shape:
/// <list type="bullet">
///   <item>typed options bound from configuration</item>
///   <item>injected logger (no static logging)</item>
///   <item>methods return <c>Result&lt;T&gt;</c> from Compendium.Abstractions</item>
/// </list>
/// </summary>
public sealed class SampleAdapter
{
    private readonly SampleOptions _options;
    private readonly ILogger<SampleAdapter> _logger;

    /// <summary>
    /// Creates a new <see cref="SampleAdapter"/>.
    /// </summary>
    /// <param name="options">Adapter configuration.</param>
    /// <param name="logger">Diagnostic logger.</param>
    public SampleAdapter(IOptions<SampleOptions> options, ILogger<SampleAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Echoes a payload back. Replace with the real adapter operation.
    /// </summary>
    /// <param name="payload">Input payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The same payload, prefixed with the configured base URL.</returns>
    public Task<string> EchoAsync(string payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        _logger.LogDebug("Echoing payload of length {Length}", payload.Length);
        return Task.FromResult($"{_options.BaseUrl}::{payload}");
    }
}
