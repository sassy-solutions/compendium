// -----------------------------------------------------------------------
// <copyright file="OpenRouterHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.OpenRouter.Http;
using Compendium.Adapters.OpenRouter.Http.Models;
using Compendium.Adapters.OpenRouter.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.OpenRouter.Tests.Http;

/// <summary>
/// Unit tests for <see cref="OpenRouterHttpClient"/>. HTTP transport is mocked with RichardSzalay.MockHttp.
/// </summary>
public class OpenRouterHttpClientTests
{
    private static OpenRouterCompletionRequest BuildRequest(string model = "anthropic/claude-3.5-sonnet") =>
        new()
        {
            Model = model,
            Messages = new List<OpenRouterMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

    [Fact]
    public void Constructor_ConfiguresAuthorizationHeader()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o => o.ApiKey = "sk-or-v1-mykey");

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.Authorization.Should().NotBeNull();
        headers.Authorization!.Scheme.Should().Be("Bearer");
        headers.Authorization!.Parameter.Should().Be("sk-or-v1-mykey");
    }

    [Fact]
    public void Constructor_WithSiteUrlAndName_AddsRankingHeaders()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o =>
        {
            o.SiteUrl = "https://example.com";
            o.SiteName = "Example App";
        });

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.GetValues("HTTP-Referer").Should().ContainSingle().Which.Should().Be("https://example.com");
        headers.GetValues("X-Title").Should().ContainSingle().Which.Should().Be("Example App");
    }

    [Fact]
    public void Constructor_WithoutOptionalHeaders_OmitsRankingHeaders()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient();

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.Contains("HTTP-Referer").Should().BeFalse();
        headers.Contains("X-Title").Should().BeFalse();
    }

    [Fact]
    public async Task CreateCompletionAsync_OnSuccess_ReturnsParsedResponse()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var responseJson = """
        {
          "id": "gen-abc",
          "model": "anthropic/claude-3.5-sonnet",
          "created": 1730000000,
          "choices": [
            {
              "index": 0,
              "message": { "role": "assistant", "content": "Hi!" },
              "finish_reason": "stop"
            }
          ],
          "usage": { "prompt_tokens": 10, "completion_tokens": 4, "total_tokens": 14 }
        }
        """;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", responseJson);

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("gen-abc");
        result.Value.Model.Should().Be("anthropic/claude-3.5-sonnet");
        result.Value.Choices.Should().ContainSingle();
        result.Value.Choices[0].Message!.Content.Should().Be("Hi!");
        result.Value.Usage!.PromptTokens.Should().Be(10);
        result.Value.Usage!.CompletionTokens.Should().Be(4);
    }

    [Fact]
    public async Task CreateCompletionAsync_WhenLoggingEnabled_LogsRequestAndResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) };
        var logger = new TestFactories.RecordingLogger<OpenRouterHttpClient>();
        var sut = new OpenRouterHttpClient(httpClient, Options.Create(options), logger);

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        logger.Entries.Should().Contain(e => e.Message.Contains("OpenRouter request"));
        logger.Entries.Should().Contain(e => e.Message.Contains("OpenRouter response"));
    }

    [Fact]
    public async Task CreateCompletionAsync_WhenResponseIsEmpty_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", "null");

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("Empty response");
    }

    [Fact]
    public async Task CreateCompletionAsync_WhenResponseIsInvalidJson_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", "this is not json");

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("Invalid response format");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AI.InvalidApiKey")]
    [InlineData(HttpStatusCode.TooManyRequests, "AI.RateLimitExceeded")]
    [InlineData(HttpStatusCode.PaymentRequired, "AI.InsufficientCredits")]
    [InlineData(HttpStatusCode.NotFound, "AI.ModelNotFound")]
    [InlineData(HttpStatusCode.InternalServerError, "AI.ProviderError")]
    public async Task CreateCompletionAsync_OnErrorStatus_MapsToTypedError(HttpStatusCode status, string expectedCode)
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(status, "application/json", """{"error":{"code":"E1","message":"boom","type":"x"}}""");

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task CreateCompletionAsync_OnErrorStatus_WithUnparseableBody_ReturnsProviderErrorWithRawBody()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(HttpStatusCode.BadGateway, "text/plain", "raw body text");

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("raw body text");
    }

    [Fact]
    public async Task CreateCompletionAsync_OnErrorStatus_WithEmptyErrorObject_UsesRawContentAsMessage()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"foo\":\"bar\"}");

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        // OpenRouterErrorResponse.error is null → falls back to raw content
        result.Error.Message.Should().Contain("foo");
    }

    [Fact]
    public async Task CreateCompletionAsync_OnHttpRequestException_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Throw(new HttpRequestException("connection refused"));

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("connection refused");
    }

    [Fact]
    public async Task CreateCompletionAsync_OnTimeoutCanceledByClient_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient(o => o.TimeoutSeconds = 7);
        // Throw a TaskCanceledException without a user-cancellation token to simulate HttpClient timeout.
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Throw(new TaskCanceledException("timeout"));

        // Act
        var result = await sut.CreateCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
        result.Error.Message.Should().Contain("7");
    }

    [Fact]
    public async Task CreateCompletionAsync_WhenCallerCancels_PropagatesTaskCanceledException()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Throw(new TaskCanceledException("cancelled"));

        // Act
        var act = async () => await sut.CreateCompletionAsync(BuildRequest(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task ListModelsAsync_OnSuccess_ReturnsModelsList()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var json = """
        {
          "data": [
            { "id": "anthropic/claude-3.5-sonnet", "name": "Claude 3.5 Sonnet", "context_length": 200000 },
            { "id": "openai/gpt-4o", "context_length": 128000 }
          ]
        }
        """;
        handler.When(HttpMethod.Get, "*/models")
            .Respond("application/json", json);

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("anthropic/claude-3.5-sonnet");
        result.Value[1].Id.Should().Be("openai/gpt-4o");
    }

    [Fact]
    public async Task ListModelsAsync_OnNon2xx_ReturnsMappedError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Get, "*/models")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":{\"message\":\"bad key\"}}");

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidApiKey");
    }

    [Fact]
    public async Task ListModelsAsync_OnUnexpectedException_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Get, "*/models")
            .Throw(new InvalidOperationException("DNS failed"));

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("DNS failed");
    }

    [Fact]
    public async Task ListModelsAsync_WhenCallerCancels_PropagatesTaskCanceledException()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Get, "*/models")
            .Throw(new TaskCanceledException("cancelled"));

        // Act
        var act = async () => await sut.ListModelsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task CreateCompletionStreamAsync_OnSuccess_YieldsParsedChunks()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"Hel\"}}]}",
            "",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"lo\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<OpenRouterStreamChunk>();
        await foreach (var c in sut.CreateCompletionStreamAsync(BuildRequest(), CancellationToken.None))
        {
            c.IsSuccess.Should().BeTrue();
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].Choices[0].Delta!.Content.Should().Be("Hel");
        chunks[1].Choices[0].Delta!.Content.Should().Be("lo");
        chunks[2].Choices[0].FinishReason.Should().Be("stop");
    }

    [Fact]
    public async Task CreateCompletionStreamAsync_OnNon2xxStatus_YieldsSingleFailureAndStops()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{\"error\":{\"message\":\"slow down\"}}");

        // Act
        var results = new List<Result<OpenRouterStreamChunk>>();
        await foreach (var r in sut.CreateCompletionStreamAsync(BuildRequest(), CancellationToken.None))
        {
            results.Add(r);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Error.Code.Should().Be("AI.RateLimitExceeded");
    }

    [Fact]
    public async Task CreateCompletionStreamAsync_SkipsBlankLines_NonDataLines_AndUnparseableData()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "",
            ": comment line",
            "event: message",
            "data: not-json",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"ok\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<OpenRouterStreamChunk>();
        await foreach (var c in sut.CreateCompletionStreamAsync(BuildRequest(), CancellationToken.None))
        {
            c.IsSuccess.Should().BeTrue();
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].Choices[0].Delta!.Content.Should().Be("ok");
    }

    [Fact]
    public async Task CreateCompletionStreamAsync_StopsWhenCancellationRequested()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"a\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"b\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"c\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("text/event-stream", stream);

        using var cts = new CancellationTokenSource();
        var chunks = new List<OpenRouterStreamChunk>();

        // Act
        await foreach (var c in sut.CreateCompletionStreamAsync(BuildRequest(), cts.Token))
        {
            if (c.IsSuccess)
            {
                chunks.Add(c.Value);
            }
            cts.Cancel();
        }

        // Assert
        chunks.Should().HaveCountGreaterThan(0);
        chunks.Should().HaveCountLessThan(4);
    }

    [Fact]
    public async Task CreateCompletionStreamAsync_NullChunkAfterDeserialization_IsSkipped()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        // "null" deserializes to null and should be skipped without yielding.
        var stream = string.Join("\n",
            "data: null",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"x\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<OpenRouterStreamChunk>();
        await foreach (var c in sut.CreateCompletionStreamAsync(BuildRequest(), CancellationToken.None))
        {
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].Choices[0].Delta!.Content.Should().Be("x");
    }

    /// <summary>
    /// Reflection helper to extract the underlying <see cref="HttpClient"/> for header inspection.
    /// </summary>
    private static HttpClient GetUnderlyingHttpClient(OpenRouterHttpClient sut)
    {
        var field = typeof(OpenRouterHttpClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (HttpClient)field!.GetValue(sut)!;
    }
}
