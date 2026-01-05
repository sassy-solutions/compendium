// -----------------------------------------------------------------------
// <copyright file="OpenRouterHttpClient.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using Compendium.Adapters.OpenRouter.Configuration;
using Compendium.Adapters.OpenRouter.Http.Models;

namespace Compendium.Adapters.OpenRouter.Http;

/// <summary>
/// HTTP client for communicating with the OpenRouter API.
/// </summary>
internal sealed class OpenRouterHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterHttpClient"/> class.
    /// </summary>
    public OpenRouterHttpClient(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");

        if (!string.IsNullOrEmpty(_options.SiteUrl))
        {
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _options.SiteUrl);
        }

        if (!string.IsNullOrEmpty(_options.SiteName))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Title", _options.SiteName);
        }
    }

    /// <summary>
    /// Creates a completion request.
    /// </summary>
    public async Task<Result<OpenRouterCompletionResponse>> CreateCompletionAsync(
        OpenRouterCompletionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (_options.EnableLogging)
            {
                _logger.LogDebug("OpenRouter request: {Request}", json);
            }

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);

            return await HandleResponseAsync<OpenRouterCompletionResponse>(response, cancellationToken);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "OpenRouter request timed out");
            return Result.Failure<OpenRouterCompletionResponse>(
                AIErrors.Timeout(TimeSpan.FromSeconds(_options.TimeoutSeconds)));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error communicating with OpenRouter");
            return Result.Failure<OpenRouterCompletionResponse>(
                AIErrors.ProviderError(ex.Message));
        }
    }

    /// <summary>
    /// Creates a streaming completion request.
    /// </summary>
    public async IAsyncEnumerable<Result<OpenRouterStreamChunk>> CreateCompletionStreamAsync(
        OpenRouterCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        Stream? stream = null;

        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
            {
                Content = content
            };

            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await ParseErrorAsync(response, cancellationToken);
                yield return Result.Failure<OpenRouterStreamChunk>(error);
                yield break;
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrEmpty(line))
                    continue;

                if (!line.StartsWith("data: "))
                    continue;

                var data = line[6..];

                if (data == "[DONE]")
                    yield break;

                OpenRouterStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenRouterStreamChunk>(data, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse stream chunk: {Data}", data);
                    continue;
                }

                if (chunk != null)
                {
                    yield return Result.Success(chunk);
                }
            }
        }
        finally
        {
            stream?.Dispose();
            response?.Dispose();
        }
    }

    /// <summary>
    /// Lists available models.
    /// </summary>
    public async Task<Result<List<OpenRouterModel>>> ListModelsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("/models", cancellationToken);
            var result = await HandleResponseAsync<OpenRouterModelsResponse>(response, cancellationToken);

            return result.Match(
                success => Result.Success(success.Data),
                error => Result.Failure<List<OpenRouterModel>>(error));
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing OpenRouter models");
            return Result.Failure<List<OpenRouterModel>>(
                AIErrors.ProviderError(ex.Message));
        }
    }

    private async Task<Result<T>> HandleResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("OpenRouter response ({StatusCode}): {Content}",
                response.StatusCode, content);
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
                return result != null
                    ? Result.Success(result)
                    : Result.Failure<T>(AIErrors.ProviderError("Empty response from provider"));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize OpenRouter response");
                return Result.Failure<T>(AIErrors.ProviderError("Invalid response format"));
            }
        }

        var error = await ParseErrorAsync(response, cancellationToken);
        return Result.Failure<T>(error);
    }

    private async Task<Error> ParseErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(content, JsonOptions);
            var errorMessage = errorResponse?.Error?.Message ?? content;
            var errorCode = errorResponse?.Error?.Code;

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => AIErrors.InvalidApiKey(),
                HttpStatusCode.TooManyRequests => AIErrors.RateLimitExceeded(),
                HttpStatusCode.PaymentRequired => AIErrors.InsufficientCredits(),
                HttpStatusCode.NotFound => AIErrors.ModelNotFound(errorMessage),
                _ => AIErrors.ProviderError(errorMessage, errorCode)
            };
        }
        catch
        {
            return AIErrors.ProviderError(content);
        }
    }
}
