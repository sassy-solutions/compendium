// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyHttpClient.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http.Models;

namespace Compendium.Adapters.LemonSqueezy.Http;

/// <summary>
/// HTTP client for communicating with the LemonSqueezy REST API.
/// Uses JSON:API format as specified by LemonSqueezy.
/// </summary>
internal sealed class LemonSqueezyHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly LemonSqueezyOptions _options;
    private readonly ILogger<LemonSqueezyHttpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LemonSqueezyHttpClient"/> class.
    /// </summary>
    public LemonSqueezyHttpClient(
        HttpClient httpClient,
        IOptions<LemonSqueezyOptions> options,
        ILogger<LemonSqueezyHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        // Bearer token authentication
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        // JSON:API content type
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
    }

    // ============================================================================
    // Customer Operations
    // ============================================================================

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    public async Task<Result<JsonApiResource<LsCustomerAttributes>>> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        return await GetResourceAsync<LsCustomerAttributes>(
            $"customers/{customerId}", cancellationToken);
    }

    /// <summary>
    /// Lists customers with optional filtering.
    /// </summary>
    public async Task<Result<List<JsonApiResource<LsCustomerAttributes>>>> ListCustomersAsync(
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        var url = "customers";
        if (!string.IsNullOrEmpty(email))
        {
            url += $"?filter[email]={Uri.EscapeDataString(email)}";
        }

        return await GetCollectionAsync<LsCustomerAttributes>(url, cancellationToken);
    }

    // ============================================================================
    // Subscription Operations
    // ============================================================================

    /// <summary>
    /// Gets a subscription by ID.
    /// </summary>
    public async Task<Result<JsonApiResource<LsSubscriptionAttributes>>> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await GetResourceAsync<LsSubscriptionAttributes>(
            $"subscriptions/{subscriptionId}", cancellationToken);
    }

    /// <summary>
    /// Lists subscriptions for a customer.
    /// </summary>
    public async Task<Result<List<JsonApiResource<LsSubscriptionAttributes>>>> ListSubscriptionsAsync(
        string? customerId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(customerId))
        {
            queryParams.Add($"filter[customer_id]={customerId}");
        }

        if (!string.IsNullOrEmpty(status))
        {
            queryParams.Add($"filter[status]={status}");
        }

        if (!string.IsNullOrEmpty(_options.StoreId))
        {
            queryParams.Add($"filter[store_id]={_options.StoreId}");
        }

        var url = "subscriptions";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await GetCollectionAsync<LsSubscriptionAttributes>(url, cancellationToken);
    }

    /// <summary>
    /// Updates a subscription (cancel, pause, resume, change variant).
    /// </summary>
    public async Task<Result<JsonApiResource<LsSubscriptionAttributes>>> UpdateSubscriptionAsync(
        string subscriptionId,
        LsUpdateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PatchResourceAsync<LsUpdateSubscriptionRequest, LsSubscriptionAttributes>(
            $"subscriptions/{subscriptionId}", request, cancellationToken);
    }

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    public async Task<Result> DeleteSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await DeleteAsync($"subscriptions/{subscriptionId}", cancellationToken);
    }

    // ============================================================================
    // Checkout Operations
    // ============================================================================

    /// <summary>
    /// Creates a checkout session.
    /// </summary>
    public async Task<Result<JsonApiResource<LsCheckoutAttributes>>> CreateCheckoutAsync(
        LsCreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostResourceAsync<LsCreateCheckoutRequest, LsCheckoutAttributes>(
            "checkouts", request, cancellationToken);
    }

    /// <summary>
    /// Gets a checkout by ID.
    /// </summary>
    public async Task<Result<JsonApiResource<LsCheckoutAttributes>>> GetCheckoutAsync(
        string checkoutId,
        CancellationToken cancellationToken = default)
    {
        return await GetResourceAsync<LsCheckoutAttributes>(
            $"checkouts/{checkoutId}", cancellationToken);
    }

    // ============================================================================
    // License Key Operations (JSON:API endpoints)
    // ============================================================================

    /// <summary>
    /// Gets a license key by ID.
    /// </summary>
    public async Task<Result<JsonApiResource<LsLicenseKeyAttributes>>> GetLicenseKeyAsync(
        string licenseKeyId,
        CancellationToken cancellationToken = default)
    {
        return await GetResourceAsync<LsLicenseKeyAttributes>(
            $"license-keys/{licenseKeyId}", cancellationToken);
    }

    /// <summary>
    /// Lists license key instances.
    /// </summary>
    public async Task<Result<List<JsonApiResource<LsLicenseKeyInstanceAttributes>>>> ListLicenseKeyInstancesAsync(
        string licenseKeyId,
        CancellationToken cancellationToken = default)
    {
        return await GetCollectionAsync<LsLicenseKeyInstanceAttributes>(
            $"license-key-instances?filter[license_key_id]={licenseKeyId}", cancellationToken);
    }

    // ============================================================================
    // License API Operations (Non-JSON:API endpoints)
    // ============================================================================

    /// <summary>
    /// Validates a license key.
    /// </summary>
    public async Task<Result<LsValidateLicenseResponse>> ValidateLicenseAsync(
        LsValidateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostLicenseApiAsync<LsValidateLicenseRequest, LsValidateLicenseResponse>(
            "licenses/validate", request, cancellationToken);
    }

    /// <summary>
    /// Activates a license key.
    /// </summary>
    public async Task<Result<LsActivateLicenseResponse>> ActivateLicenseAsync(
        LsActivateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostLicenseApiAsync<LsActivateLicenseRequest, LsActivateLicenseResponse>(
            "licenses/activate", request, cancellationToken);
    }

    /// <summary>
    /// Deactivates a license key.
    /// </summary>
    public async Task<Result<LsDeactivateLicenseResponse>> DeactivateLicenseAsync(
        LsDeactivateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostLicenseApiAsync<LsDeactivateLicenseRequest, LsDeactivateLicenseResponse>(
            "licenses/deactivate", request, cancellationToken);
    }

    // ============================================================================
    // HTTP Helper Methods
    // ============================================================================

    private async Task<Result<JsonApiResource<T>>> GetResourceAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<JsonApiResource<T>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<JsonApiResponse<JsonApiResource<T>>>(
                _jsonOptions, cancellationToken);

            if (wrapper?.Data is null)
            {
                return Error.Failure("LemonSqueezy.EmptyResponse", "Empty response from LemonSqueezy API");
            }

            return Result<JsonApiResource<T>>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in LemonSqueezy GetResourceAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
        }
    }

    private async Task<Result<List<JsonApiResource<T>>>> GetCollectionAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<List<JsonApiResource<T>>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<JsonApiCollectionResponse<JsonApiResource<T>>>(
                _jsonOptions, cancellationToken);

            return Result<List<JsonApiResource<T>>>.Success(wrapper?.Data ?? new List<JsonApiResource<T>>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in LemonSqueezy GetCollectionAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
        }
    }

    private async Task<Result<JsonApiResource<TResponse>>> PostResourceAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/vnd.api+json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<JsonApiResource<TResponse>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<JsonApiResponse<JsonApiResource<TResponse>>>(
                _jsonOptions, cancellationToken);

            if (wrapper?.Data is null)
            {
                return Error.Failure("LemonSqueezy.EmptyResponse", "Empty response from LemonSqueezy API");
            }

            return Result<JsonApiResource<TResponse>>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in LemonSqueezy PostResourceAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
        }
    }

    private async Task<Result<JsonApiResource<TResponse>>> PatchResourceAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/vnd.api+json");

            var response = await _httpClient.PatchAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<JsonApiResource<TResponse>>(response, cancellationToken);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<JsonApiResponse<JsonApiResource<TResponse>>>(
                _jsonOptions, cancellationToken);

            if (wrapper?.Data is null)
            {
                return Error.Failure("LemonSqueezy.EmptyResponse", "Empty response from LemonSqueezy API");
            }

            return Result<JsonApiResource<TResponse>>.Success(wrapper.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in LemonSqueezy PatchResourceAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
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
            _logger.LogError(ex, "HTTP error in LemonSqueezy DeleteAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
        }
    }

    private async Task<Result<TResponse>> PostLicenseApiAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // License API uses regular JSON, not JSON:API
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponseAsync<TResponse>(response, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);

            if (result is null)
            {
                return Error.Failure("LemonSqueezy.EmptyResponse", "Empty response from LemonSqueezy API");
            }

            return Result<TResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in LemonSqueezy License PostAsync");
            return Error.Failure("LemonSqueezy.HttpError", ex.Message);
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
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            errorMessage = !string.IsNullOrWhiteSpace(errorBody) ? errorBody : response.ReasonPhrase ?? "Unknown error";
        }
        catch
        {
            errorMessage = response.ReasonPhrase ?? "Unknown error";
        }

        _logger.LogWarning("LemonSqueezy API error: {StatusCode} - {Error}",
            response.StatusCode, errorMessage);

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => Error.NotFound("LemonSqueezy.NotFound", errorMessage),
            HttpStatusCode.BadRequest => Error.Validation("LemonSqueezy.BadRequest", errorMessage),
            HttpStatusCode.Unauthorized => Error.Failure("LemonSqueezy.Unauthorized", "Invalid API key"),
            HttpStatusCode.Forbidden => Error.Failure("LemonSqueezy.Forbidden", "Access denied"),
            HttpStatusCode.Conflict => Error.Conflict("LemonSqueezy.Conflict", errorMessage),
            HttpStatusCode.UnprocessableEntity => Error.Validation("LemonSqueezy.UnprocessableEntity", errorMessage),
            HttpStatusCode.TooManyRequests => BillingErrors.RateLimitExceeded,
            HttpStatusCode.ServiceUnavailable => BillingErrors.ProviderUnavailable,
            _ => Error.Failure("LemonSqueezy.Error", errorMessage)
        };
    }
}
