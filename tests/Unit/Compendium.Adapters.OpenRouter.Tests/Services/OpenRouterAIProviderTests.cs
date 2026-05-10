// -----------------------------------------------------------------------
// <copyright file="OpenRouterAIProviderTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.OpenRouter.Http;
using Compendium.Adapters.OpenRouter.Tests.TestSupport;

namespace Compendium.Adapters.OpenRouter.Tests.Services;

/// <summary>
/// Unit tests for <see cref="OpenRouterAIProvider"/>. Uses MockHttp to drive the underlying HTTP client.
/// </summary>
public class OpenRouterAIProviderTests
{
    [Fact]
    public void ProviderId_Always_ReturnsOpenrouter()
    {
        // Arrange
        var (httpClient, _) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);

        // Act
        var id = sut.ProviderId;

        // Assert
        id.Should().Be("openrouter");
    }

    // ---------- CompleteAsync ----------

    [Fact]
    public async Task CompleteAsync_OnSuccess_MapsApiResponseToCompletionResponse()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        var json = """
        {
          "id": "gen-1",
          "model": "anthropic/claude-3.5-sonnet",
          "created": 1730000000,
          "choices": [
            { "index": 0, "message": { "role": "assistant", "content": "Hello world" }, "finish_reason": "stop" }
          ],
          "usage": { "prompt_tokens": 12, "completion_tokens": 3, "total_tokens": 15 }
        }
        """;
        handler.When(HttpMethod.Post, "*/chat/completions").Respond("application/json", json);

        var request = new CompletionRequest
        {
            Model = "anthropic/claude-3.5-sonnet",
            Messages = new List<Message>
            {
                Message.User("Hi"),
                Message.Assistant("Yes?"),
                new Message { Role = MessageRole.User, Content = "Tell me a joke", Name = "alice" }
            },
            SystemPrompt = "Be concise.",
            Temperature = 0.5f,
            MaxTokens = 256,
            TopP = 0.9f,
            FrequencyPenalty = 0.1f,
            PresencePenalty = 0.2f,
            StopSequences = new List<string> { "###" }
        };

        // Act
        var result = await sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("gen-1");
        result.Value.Model.Should().Be("anthropic/claude-3.5-sonnet");
        result.Value.Content.Should().Be("Hello world");
        result.Value.FinishReason.Should().Be(FinishReason.Stop);
        result.Value.Usage.PromptTokens.Should().Be(12);
        result.Value.Usage.CompletionTokens.Should().Be(3);
        result.Value.CreatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1730000000).UtcDateTime);
    }

    [Fact]
    public async Task CompleteAsync_WithEmptyChoices_ReturnsEmptyContentAndInProgressReason()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        var result = await sut.CompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().BeEmpty();
        result.Value.FinishReason.Should().Be(FinishReason.InProgress);
        result.Value.Usage.PromptTokens.Should().Be(0);
        result.Value.Usage.CompletionTokens.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_WithNoModel_UsesDefaultFromOptions()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient(o => o.DefaultModel = "openai/gpt-4o");
        var sut = TestFactories.CreateProvider(httpClient, o => o.DefaultModel = "openai/gpt-4o");
        string? capturedBody = null;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .With(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id":"x","model":"openai/gpt-4o","created":0,"choices":[]}""");

        // Act
        var request = new CompletionRequest
        {
            Model = null!,
            Messages = new List<Message> { Message.User("hi") }
        };
        await sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        capturedBody.Should().NotBeNull();
        capturedBody!.Should().Contain("openai/gpt-4o");
    }

    [Fact]
    public async Task CompleteAsync_WithMaxTokensNull_AppliesDefaultMaxTokensFromOptions()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient(o => o.DefaultMaxTokens = 1234);
        var sut = TestFactories.CreateProvider(httpClient, o => o.DefaultMaxTokens = 1234);
        string? body = null;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .With(req =>
            {
                body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await sut.CompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None);

        // Assert
        body.Should().Contain("\"max_tokens\":1234");
    }

    [Fact]
    public async Task CompleteAsync_WithSystemPrompt_PrependsSystemMessage()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        string? body = null;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .With(req =>
            {
                body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        var request = new CompletionRequest
        {
            Model = "m",
            SystemPrompt = "You are helpful.",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        await sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        body.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(body!);
        var messages = doc.RootElement.GetProperty("messages").EnumerateArray().ToList();
        messages.Should().HaveCount(2);
        messages[0].GetProperty("role").GetString().Should().Be("system");
        messages[0].GetProperty("content").GetString().Should().Be("You are helpful.");
        messages[1].GetProperty("role").GetString().Should().Be("user");
    }

    [Fact]
    public async Task CompleteAsync_WithoutSystemPrompt_DoesNotPrependSystemMessage()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        string? body = null;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .With(req =>
            {
                body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await sut.CompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None);

        // Assert
        var doc = JsonDocument.Parse(body!);
        var roles = doc.RootElement.GetProperty("messages").EnumerateArray()
            .Select(m => m.GetProperty("role").GetString()).ToList();
        roles.Should().NotContain("system");
    }

    [Fact]
    public async Task CompleteAsync_OnHttpError_ReturnsFailure()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":{\"message\":\"bad key\"}}");

        // Act
        var result = await sut.CompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidApiKey");
    }

    [Theory]
    [InlineData("stop", FinishReason.Stop)]
    [InlineData("STOP", FinishReason.Stop)]
    [InlineData("length", FinishReason.Length)]
    [InlineData("content_filter", FinishReason.ContentFilter)]
    [InlineData("tool_calls", FinishReason.ToolCall)]
    [InlineData("function_call", FinishReason.ToolCall)]
    [InlineData("weird_other", FinishReason.Other)]
    public async Task CompleteAsync_MapsFinishReasonCorrectly(string apiReason, FinishReason expected)
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        var json = $$"""
        {
          "id": "x", "model": "m", "created": 0,
          "choices": [
            { "index": 0, "message": { "role": "assistant", "content": "" }, "finish_reason": "{{apiReason}}" }
          ]
        }
        """;
        handler.When(HttpMethod.Post, "*/chat/completions").Respond("application/json", json);

        // Act
        var result = await sut.CompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FinishReason.Should().Be(expected);
    }

    // ---------- StreamCompleteAsync ----------

    [Fact]
    public async Task StreamCompleteAsync_OnSuccess_YieldsChunksWithIncrementingIndex_AndStopsOnFinal()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        var stream = string.Join("\n",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"He\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"llo\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":3,\"completion_tokens\":2,\"total_tokens\":5}}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"never\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<CompletionChunk>();
        await foreach (var r in sut.StreamCompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None))
        {
            r.IsSuccess.Should().BeTrue();
            chunks.Add(r.Value);
        }

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].ContentDelta.Should().Be("He");
        chunks[0].Index.Should().Be(0);
        chunks[0].IsFinal.Should().BeFalse();
        chunks[1].ContentDelta.Should().Be("llo");
        chunks[1].Index.Should().Be(1);
        chunks[2].IsFinal.Should().BeTrue();
        chunks[2].FinishReason.Should().Be(FinishReason.Stop);
        chunks[2].Usage!.PromptTokens.Should().Be(3);
        chunks[2].Usage!.CompletionTokens.Should().Be(2);
    }

    [Fact]
    public async Task StreamCompleteAsync_WithNoModel_UsesDefaultFromOptions()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient(o => o.DefaultModel = "openai/gpt-4o-mini");
        var sut = TestFactories.CreateProvider(httpClient, o => o.DefaultModel = "openai/gpt-4o-mini");
        string? body = null;
        handler.When(HttpMethod.Post, "*/chat/completions")
            .With(req =>
            {
                body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return true;
            })
            .Respond("text/event-stream", "data: [DONE]\n");

        var request = new CompletionRequest
        {
            Model = null!,
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        await foreach (var _ in sut.StreamCompleteAsync(request, CancellationToken.None))
        {
        }

        // Assert
        body.Should().NotBeNull();
        body!.Should().Contain("openai/gpt-4o-mini");
        body!.Should().Contain("\"stream\":true");
    }

    [Fact]
    public async Task StreamCompleteAsync_WhenChunkReturnsFailure_YieldsFailureAndStops()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Post, "*/chat/completions")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{\"error\":{\"message\":\"limit\"}}");

        // Act
        var results = new List<Result<CompletionChunk>>();
        await foreach (var r in sut.StreamCompleteAsync(TestFactories.SimpleCompletionRequest(), CancellationToken.None))
        {
            results.Add(r);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Error.Code.Should().Be("AI.RateLimitExceeded");
    }

    // ---------- EmbedAsync ----------

    [Fact]
    public async Task EmbedAsync_Always_ReturnsInvalidRequestFailure()
    {
        // Arrange
        var (httpClient, _) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        var request = new EmbeddingRequest { Model = "any", Inputs = new List<string> { "a", "b" } };

        // Act
        var result = await sut.EmbedAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidRequest");
        result.Error.Message.Should().Contain("Embeddings are not directly supported");
    }

    // ---------- ListModelsAsync ----------

    [Fact]
    public async Task ListModelsAsync_OnSuccess_MapsAllFields()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        var json = """
        {
          "data": [
            {
              "id": "anthropic/claude-3.5-sonnet",
              "name": "Claude 3.5 Sonnet",
              "context_length": 200000,
              "architecture": { "modality": "text+image" },
              "pricing": { "prompt": "0.000003", "completion": "0.000015" },
              "top_provider": { "max_completion_tokens": 8192 }
            },
            {
              "id": "openai/gpt-4o",
              "context_length": 128000,
              "architecture": { "modality": "text" },
              "pricing": { "prompt": "not-a-number", "completion": null }
            },
            { "id": "no-slash-model" }
          ]
        }
        """;
        handler.When(HttpMethod.Get, "*/models").Respond("application/json", json);

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        var claude = result.Value[0];
        claude.Id.Should().Be("anthropic/claude-3.5-sonnet");
        claude.Name.Should().Be("Claude 3.5 Sonnet");
        claude.Provider.Should().Be("anthropic");
        claude.ContextWindow.Should().Be(200000);
        claude.MaxOutputTokens.Should().Be(8192);
        claude.SupportsStreaming.Should().BeTrue();
        claude.SupportsEmbeddings.Should().BeFalse();
        claude.SupportsVision.Should().BeTrue();
        claude.SupportsTools.Should().BeTrue();
        claude.PricingInputPerMillion.Should().Be(3m); // 0.000003 * 1_000_000
        claude.PricingOutputPerMillion.Should().Be(15m); // 0.000015 * 1_000_000

        var gpt = result.Value[1];
        gpt.Id.Should().Be("openai/gpt-4o");
        gpt.Name.Should().Be("openai/gpt-4o"); // falls back to id
        gpt.Provider.Should().Be("openai");
        gpt.SupportsVision.Should().BeFalse();
        gpt.PricingInputPerMillion.Should().BeNull(); // unparseable
        gpt.PricingOutputPerMillion.Should().BeNull(); // null

        var noSlash = result.Value[2];
        noSlash.Provider.Should().Be("unknown");
        noSlash.SupportsVision.Should().BeFalse();
    }

    [Fact]
    public async Task ListModelsAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Get, "*/models")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{}");

        // Act
        var result = await sut.ListModelsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    // ---------- HealthCheckAsync ----------

    [Fact]
    public async Task HealthCheckAsync_WhenModelsListSucceeds_ReturnsSuccess()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Get, "*/models")
            .Respond("application/json", "{\"data\":[]}");

        // Act
        var result = await sut.HealthCheckAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheckAsync_WhenModelsListReturnsFailure_ReturnsFailure()
    {
        // Arrange
        var (httpClient, handler) = TestFactories.CreateHttpClient();
        var sut = TestFactories.CreateProvider(httpClient);
        handler.When(HttpMethod.Get, "*/models")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":{\"message\":\"x\"}}");

        // Act
        var result = await sut.HealthCheckAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidApiKey");
    }

    [Fact]
    public async Task HealthCheckAsync_WhenUnderlyingThrowsUnhandled_ReturnsProviderUnavailable()
    {
        // Arrange — pass a null httpClient to force a NullReferenceException inside the provider's call path
        // is unsafe; instead, drop the HttpClient's BaseAddress so SendAsync throws an InvalidOperationException
        // synchronously inside the provider, exercising the catch in HealthCheckAsync.
        var handler = new MockHttpMessageHandler();
        var options = TestFactories.DefaultOptions();
        var inner = new HttpClient(handler); // no BaseAddress
        var httpClient = new OpenRouterHttpClient(
            inner,
            Options.Create(options),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenRouterHttpClient>.Instance);

        // Force a non-TaskCanceled throw on /models that would bubble up only past HttpClient.GetAsync;
        // ListModelsAsync catches Exception itself and returns Result.Failure(ProviderError),
        // so we instead use a relative URI that fails outside the catch boundaries by removing BaseAddress.
        // Easiest path: set BaseAddress to null after construction is impossible (private). Instead
        // configure the handler to throw a non-TaskCanceledException; ListModelsAsync's catch returns Failure,
        // which feeds the success/failure branch tested above. To exercise the catch in HealthCheckAsync,
        // we make ListModelsAsync itself throw by configuring the handler to throw an exception type
        // that is NOT caught by ListModelsAsync. ListModelsAsync only catches generic Exception, so it WILL
        // catch everything except OperationCanceledException-with-token. A regular cancellation flow leaves
        // the HealthCheckAsync catch unreachable through MockHttp; we accept that branch is best left to
        // a separate path. Instead, simulate by passing a non-cancellation token that triggers a
        // TaskCanceledException with cancellation requested — that re-throws from ListModelsAsync, hitting
        // HealthCheckAsync's catch.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Get, "*/models")
            .Throw(new TaskCanceledException("user cancel"));

        // Note: `when (cancellationToken.IsCancellationRequested)` filter in ListModelsAsync re-throws,
        // and HealthCheckAsync wraps it in catch (Exception ex) → ProviderUnavailable.
        var sut = new OpenRouterAIProvider(
            httpClient,
            Options.Create(options),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenRouterAIProvider>.Instance);

        // Act
        var result = await sut.HealthCheckAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderUnavailable");
    }
}
