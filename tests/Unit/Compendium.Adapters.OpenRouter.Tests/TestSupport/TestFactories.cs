// -----------------------------------------------------------------------
// <copyright file="TestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http;
using Compendium.Adapters.OpenRouter.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.OpenRouter.Tests.TestSupport;

/// <summary>
/// Helpers to construct internal SUTs (HttpClient + Options) for unit tests.
/// </summary>
internal static class TestFactories
{
    public const string DefaultBaseUrl = "https://openrouter.ai/api/v1";
    public const string DefaultApiKey = "sk-or-v1-test-key";

    public static OpenRouterOptions DefaultOptions(Action<OpenRouterOptions>? configure = null)
    {
        var options = new OpenRouterOptions
        {
            ApiKey = DefaultApiKey,
            BaseUrl = DefaultBaseUrl,
            DefaultModel = "anthropic/claude-3.5-sonnet",
            DefaultMaxTokens = 4096,
            TimeoutSeconds = 120,
            EnableLogging = false
        };
        configure?.Invoke(options);
        return options;
    }

    public static (OpenRouterHttpClient Client, MockHttpMessageHandler Handler) CreateHttpClient(
        Action<OpenRouterOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl)
        };
        var sut = new OpenRouterHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<OpenRouterHttpClient>.Instance);
        return (sut, handler);
    }

    public static OpenRouterAIProvider CreateProvider(
        OpenRouterHttpClient httpClient,
        Action<OpenRouterOptions>? configure = null)
    {
        var options = DefaultOptions(configure);
        return new OpenRouterAIProvider(
            httpClient,
            Options.Create(options),
            NullLogger<OpenRouterAIProvider>.Instance);
    }

    public static CompletionRequest SimpleCompletionRequest(string? model = null)
    {
        return new CompletionRequest
        {
            Model = model ?? "anthropic/claude-3.5-sonnet",
            Messages = new List<Message> { Message.User("Hello") }
        };
    }

    /// <summary>
    /// Recording logger used to verify that log methods were invoked.
    /// </summary>
    public sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
