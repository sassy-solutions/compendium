// -----------------------------------------------------------------------
// <copyright file="OpenRouterAIProvider.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.OpenRouter.Configuration;
using Compendium.Adapters.OpenRouter.Http;
using Compendium.Adapters.OpenRouter.Http.Models;

namespace Compendium.Adapters.OpenRouter.Services;

/// <summary>
/// OpenRouter implementation of <see cref="IAIProvider"/>.
/// Provides access to 100+ LLM models through a unified API.
/// </summary>
public sealed class OpenRouterAIProvider : IAIProvider
{
    private readonly OpenRouterHttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterAIProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterAIProvider"/> class.
    /// </summary>
    public OpenRouterAIProvider(
        OpenRouterHttpClient httpClient,
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderId => "openrouter";

    /// <inheritdoc />
    public async Task<Result<CompletionResponse>> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _options.DefaultModel;

        _logger.LogDebug("Sending completion request to model {Model}", model);

        var apiRequest = MapToApiRequest(request, model, stream: false);
        var result = await _httpClient.CreateCompletionAsync(apiRequest, cancellationToken);

        return result.Match(
            apiResponse => Result.Success(MapToCompletionResponse(apiResponse)),
            error => Result.Failure<CompletionResponse>(error));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Result<CompletionChunk>> StreamCompleteAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? _options.DefaultModel;

        _logger.LogDebug("Sending streaming completion request to model {Model}", model);

        var apiRequest = MapToApiRequest(request, model, stream: true);

        var index = 0;
        await foreach (var chunk in _httpClient.CreateCompletionStreamAsync(apiRequest, cancellationToken))
        {
            if (chunk.IsFailure)
            {
                yield return Result.Failure<CompletionChunk>(chunk.Error);
                yield break;
            }

            var completionChunk = MapToCompletionChunk(chunk.Value, index++);
            yield return Result.Success(completionChunk);

            if (completionChunk.IsFinal)
            {
                yield break;
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<EmbeddingResponse>> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending embedding request for {Count} inputs", request.Inputs.Count);

        // OpenRouter doesn't directly support embeddings,
        // but some models via their native APIs do.
        // For now, return not supported error.
        // In future, could route to specific embedding models.
        return await Task.FromResult(
            Result.Failure<EmbeddingResponse>(
                AIErrors.InvalidRequest("Embeddings are not directly supported via OpenRouter. Use a dedicated embedding provider.")));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AIModel>>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching available models from OpenRouter");

        var result = await _httpClient.ListModelsAsync(cancellationToken);

        return result.Match(
            apiModels => Result.Success<IReadOnlyList<AIModel>>(
                apiModels.Select(MapToAIModel).ToList()),
            error => Result.Failure<IReadOnlyList<AIModel>>(error));
    }

    /// <inheritdoc />
    public async Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.ListModelsAsync(cancellationToken);
            return result.IsSuccess
                ? Result.Success()
                : Result.Failure(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for OpenRouter provider");
            return Result.Failure(AIErrors.ProviderUnavailable("openrouter"));
        }
    }

    private OpenRouterCompletionRequest MapToApiRequest(CompletionRequest request, string model, bool stream)
    {
        var messages = new List<OpenRouterMessage>();

        // Add system prompt if present
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new OpenRouterMessage { Role = "system", Content = request.SystemPrompt });
        }

        // Add conversation messages
        foreach (var msg in request.Messages)
        {
            messages.Add(new OpenRouterMessage
            {
                Role = msg.Role.ToString().ToLowerInvariant(),
                Content = msg.Content,
                Name = msg.Name
            });
        }

        return new OpenRouterCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
            TopP = request.TopP,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            Stop = request.StopSequences?.ToList(),
            Stream = stream
        };
    }

    private static CompletionResponse MapToCompletionResponse(OpenRouterCompletionResponse apiResponse)
    {
        var choice = apiResponse.Choices.FirstOrDefault();

        return new CompletionResponse
        {
            Id = apiResponse.Id,
            Model = apiResponse.Model,
            Content = choice?.Message?.Content ?? string.Empty,
            FinishReason = MapFinishReason(choice?.FinishReason),
            Usage = new UsageStats
            {
                PromptTokens = apiResponse.Usage?.PromptTokens ?? 0,
                CompletionTokens = apiResponse.Usage?.CompletionTokens ?? 0,
                EstimatedCostUsd = null // OpenRouter provides this in a separate field
            },
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(apiResponse.Created).UtcDateTime
        };
    }

    private static CompletionChunk MapToCompletionChunk(OpenRouterStreamChunk chunk, int index)
    {
        var choice = chunk.Choices.FirstOrDefault();
        var isFinal = choice?.FinishReason != null;

        return new CompletionChunk
        {
            Id = chunk.Id,
            ContentDelta = choice?.Delta?.Content ?? string.Empty,
            Index = index,
            IsFinal = isFinal,
            FinishReason = isFinal ? MapFinishReason(choice?.FinishReason) : null,
            Usage = chunk.Usage != null
                ? new UsageStats
                {
                    PromptTokens = chunk.Usage.PromptTokens,
                    CompletionTokens = chunk.Usage.CompletionTokens
                }
                : null
        };
    }

    private static FinishReason MapFinishReason(string? reason) => reason?.ToLowerInvariant() switch
    {
        "stop" => FinishReason.Stop,
        "length" => FinishReason.Length,
        "content_filter" => FinishReason.ContentFilter,
        "tool_calls" or "function_call" => FinishReason.ToolCall,
        null => FinishReason.InProgress,
        _ => FinishReason.Other
    };

    private static AIModel MapToAIModel(OpenRouterModel model)
    {
        return new AIModel
        {
            Id = model.Id,
            Name = model.Name ?? model.Id,
            Provider = ExtractProvider(model.Id),
            ContextWindow = model.ContextLength,
            MaxOutputTokens = model.TopProvider?.MaxCompletionTokens,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = model.Architecture?.Modality?.Contains("image") ?? false,
            SupportsTools = true, // Most models support function calling
            PricingInputPerMillion = ParsePricing(model.Pricing?.Prompt),
            PricingOutputPerMillion = ParsePricing(model.Pricing?.Completion)
        };
    }

    private static string ExtractProvider(string modelId)
    {
        var slashIndex = modelId.IndexOf('/');
        return slashIndex > 0 ? modelId[..slashIndex] : "unknown";
    }

    private static decimal? ParsePricing(string? pricing)
    {
        if (string.IsNullOrEmpty(pricing)) return null;

        // OpenRouter returns pricing per token, we want per million
        if (decimal.TryParse(pricing, out var perToken))
        {
            return perToken * 1_000_000;
        }
        return null;
    }
}
