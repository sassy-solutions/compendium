// -----------------------------------------------------------------------
// <copyright file="ListmonkHttpClient.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Http.Models;

namespace Compendium.Adapters.Listmonk.Http;

/// <summary>
/// HTTP client for communicating with the Listmonk REST API.
/// </summary>
internal sealed class ListmonkHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ListmonkOptions _options;
    private readonly ILogger<ListmonkHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private static string SanitizeEndpointForLog(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint)) return endpoint ?? string.Empty;
        var qIdx = endpoint.IndexOf('?');
        return qIdx >= 0 ? endpoint[..qIdx] : endpoint;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListmonkHttpClient"/> class.
    /// </summary>
    public ListmonkHttpClient(
        HttpClient httpClient,
        IOptions<ListmonkOptions> options,
        ILogger<ListmonkHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/api/");

        // Basic authentication
        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ============================================================================
    // Subscriber Operations
    // ============================================================================

    /// <summary>
    /// Creates a new subscriber.
    /// </summary>
    public async Task<Result<ListmonkSubscriber>> CreateSubscriberAsync(
        ListmonkCreateSubscriberRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<ListmonkCreateSubscriberRequest, ListmonkSubscriber>(
            "subscribers", request, cancellationToken);
    }

    /// <summary>
    /// Gets a subscriber by ID.
    /// </summary>
    public async Task<Result<ListmonkSubscriber>> GetSubscriberAsync(
        int subscriberId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ListmonkSubscriber>($"subscribers/{subscriberId}", cancellationToken);
    }

    /// <summary>
    /// Gets a subscriber by email.
    /// </summary>
    public async Task<Result<ListmonkSubscriber>> GetSubscriberByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var result = await GetPaginatedAsync<ListmonkSubscriber>(
            $"subscribers?query=email='{encodedEmail}'&per_page=1", cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var subscriber = result.Value.Results?.FirstOrDefault();
        if (subscriber is null)
        {
            return EmailErrors.SubscriberNotFound(email);
        }

        return subscriber;
    }

    /// <summary>
    /// Updates a subscriber.
    /// </summary>
    public async Task<Result<ListmonkSubscriber>> UpdateSubscriberAsync(
        int subscriberId,
        ListmonkUpdateSubscriberRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<ListmonkUpdateSubscriberRequest, ListmonkSubscriber>(
            $"subscribers/{subscriberId}", request, cancellationToken);
    }

    /// <summary>
    /// Deletes a subscriber.
    /// </summary>
    public async Task<Result> DeleteSubscriberAsync(
        int subscriberId,
        CancellationToken cancellationToken = default)
    {
        return await DeleteAsync($"subscribers/{subscriberId}", cancellationToken);
    }

    /// <summary>
    /// Lists subscribers.
    /// </summary>
    public async Task<Result<ListmonkPaginatedData<ListmonkSubscriber>>> ListSubscribersAsync(
        int page = 1,
        int perPage = 50,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"subscribers?page={page}&per_page={perPage}";
        if (!string.IsNullOrEmpty(query))
        {
            url += $"&query={Uri.EscapeDataString(query)}";
        }

        return await GetPaginatedAsync<ListmonkSubscriber>(url, cancellationToken);
    }

    /// <summary>
    /// Adds subscriber to lists.
    /// </summary>
    public async Task<Result> AddSubscriberToListsAsync(
        int subscriberId,
        List<int> listIds,
        CancellationToken cancellationToken = default)
    {
        var request = new { ids = new[] { subscriberId }, lists = listIds, action = "add" };
        return await PutAsync("subscribers/lists", request, cancellationToken);
    }

    /// <summary>
    /// Removes subscriber from lists.
    /// </summary>
    public async Task<Result> RemoveSubscriberFromListsAsync(
        int subscriberId,
        List<int> listIds,
        CancellationToken cancellationToken = default)
    {
        var request = new { ids = new[] { subscriberId }, lists = listIds, action = "remove" };
        return await PutAsync("subscribers/lists", request, cancellationToken);
    }

    // ============================================================================
    // List Operations
    // ============================================================================

    /// <summary>
    /// Creates a new mailing list.
    /// </summary>
    public async Task<Result<ListmonkList>> CreateListAsync(
        ListmonkCreateListRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<ListmonkCreateListRequest, ListmonkList>(
            "lists", request, cancellationToken);
    }

    /// <summary>
    /// Gets a list by ID.
    /// </summary>
    public async Task<Result<ListmonkList>> GetListAsync(
        int listId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ListmonkList>($"lists/{listId}", cancellationToken);
    }

    /// <summary>
    /// Lists all mailing lists.
    /// </summary>
    public async Task<Result<ListmonkPaginatedData<ListmonkList>>> ListListsAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        return await GetPaginatedAsync<ListmonkList>(
            $"lists?page={page}&per_page={perPage}", cancellationToken);
    }

    /// <summary>
    /// Deletes a mailing list.
    /// </summary>
    public async Task<Result> DeleteListAsync(
        int listId,
        CancellationToken cancellationToken = default)
    {
        return await DeleteAsync($"lists/{listId}", cancellationToken);
    }

    // ============================================================================
    // Template Operations
    // ============================================================================

    /// <summary>
    /// Gets all templates.
    /// </summary>
    public async Task<Result<List<ListmonkTemplate>>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetListAsync<ListmonkTemplate>("templates", cancellationToken);
    }

    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    public async Task<Result<ListmonkTemplate>> GetTemplateAsync(
        int templateId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ListmonkTemplate>($"templates/{templateId}", cancellationToken);
    }

    // ============================================================================
    // Transactional Email Operations
    // ============================================================================

    /// <summary>
    /// Sends a transactional email.
    /// </summary>
    public async Task<Result> SendTransactionalAsync(
        ListmonkTransactionalRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "tx", request, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync(response, cancellationToken);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error sending transactional email");
            return EmailErrors.SendFailed(ex.Message);
        }
    }

    // ============================================================================
    // Campaign Operations
    // ============================================================================

    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    public async Task<Result<ListmonkCampaign>> CreateCampaignAsync(
        ListmonkCreateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<ListmonkCreateCampaignRequest, ListmonkCampaign>(
            "campaigns", request, cancellationToken);
    }

    /// <summary>
    /// Gets a campaign by ID.
    /// </summary>
    public async Task<Result<ListmonkCampaign>> GetCampaignAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ListmonkCampaign>($"campaigns/{campaignId}", cancellationToken);
    }

    /// <summary>
    /// Starts a campaign.
    /// </summary>
    public async Task<Result<ListmonkCampaign>> StartCampaignAsync(
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<object, ListmonkCampaign>(
            $"campaigns/{campaignId}/status", new { status = "running" }, cancellationToken);
    }

    // ============================================================================
    // HTTP Helper Methods
    // ============================================================================

    private async Task<Result<T>> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<T>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ListmonkResponse<T>>(
                _jsonOptions, cancellationToken);

            if (wrapper is null || wrapper.Data is null)
            {
                return Error.Failure("Listmonk.EmptyResponse", "Empty response from Listmonk API");
            }

            return Result<T>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result<List<T>>> GetListAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<List<T>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ListmonkResponse<List<T>>>(
                _jsonOptions, cancellationToken);

            return wrapper?.Data ?? new List<T>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result<ListmonkPaginatedData<T>>> GetPaginatedAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<ListmonkPaginatedData<T>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ListmonkPaginatedResponse<T>>(
                _jsonOptions, cancellationToken);

            return wrapper?.Data ?? new ListmonkPaginatedData<T>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                endpoint, request, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<TResponse>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ListmonkResponse<TResponse>>(
                _jsonOptions, cancellationToken);

            if (wrapper is null || wrapper.Data is null)
            {
                return Error.Failure("Listmonk.EmptyResponse", "Empty response from Listmonk API");
            }

            return Result<TResponse>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                endpoint, request, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<TResponse>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ListmonkResponse<TResponse>>(
                _jsonOptions, cancellationToken);

            if (wrapper is null || wrapper.Data is null)
            {
                return Error.Failure("Listmonk.EmptyResponse", "Empty response from Listmonk API");
            }

            return Result<TResponse>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result> PutAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                endpoint, request, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync(response, cancellationToken);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result> DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync(response, cancellationToken);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Listmonk API: {Endpoint}", SanitizeEndpointForLog(endpoint));
            return Error.Failure("Listmonk.HttpError", ex.Message);
        }
    }

    private async Task<Result<T>> HandleErrorResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var error = await HandleErrorResponseAsync(response, cancellationToken);
        return error.Error;
    }

    private async Task<Result> HandleErrorResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string errorMessage;
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ListmonkErrorResponse>(
                _jsonOptions, cancellationToken);
            errorMessage = errorResponse?.Message ?? response.ReasonPhrase ?? "Unknown error";
        }
        catch
        {
            errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        _logger.LogWarning("Listmonk API error: {StatusCode} - {Error}",
            response.StatusCode, errorMessage);

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => Error.NotFound("Listmonk.NotFound", errorMessage),
            HttpStatusCode.BadRequest => Error.Validation("Listmonk.BadRequest", errorMessage),
            HttpStatusCode.Unauthorized => Error.Failure("Listmonk.Unauthorized", "Invalid credentials"),
            HttpStatusCode.Forbidden => Error.Failure("Listmonk.Forbidden", "Access denied"),
            HttpStatusCode.Conflict => Error.Conflict("Listmonk.Conflict", errorMessage),
            HttpStatusCode.TooManyRequests => Error.Failure("Listmonk.RateLimited", "Rate limit exceeded"),
            _ => Error.Failure("Listmonk.Error", errorMessage)
        };
    }
}
