// -----------------------------------------------------------------------
// <copyright file="ZitadelHttpClient.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http.Models;

namespace Compendium.Adapters.Zitadel.Http;

/// <summary>
/// HTTP client for communicating with Zitadel REST API.
/// Handles authentication, token management, and API calls.
/// </summary>
internal sealed class ZitadelHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelHttpClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ZitadelHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="options">The Zitadel configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public ZitadelHttpClient(
        HttpClient httpClient,
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri(_options.Authority.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Creates a human user in Zitadel.
    /// </summary>
    public async Task<Result<ZitadelUser>> CreateUserAsync(
        ZitadelCreateUserRequest request,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = "v2/users/human";

        return await PostAsync<ZitadelUser>(url, request, orgId, cancellationToken);
    }

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    public async Task<Result<ZitadelUser>> GetUserByIdAsync(
        string userId,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}";

        return await GetAsync<ZitadelUser>(url, orgId, cancellationToken);
    }

    /// <summary>
    /// Searches for users.
    /// </summary>
    public async Task<Result<ZitadelUserSearchResponse>> SearchUsersAsync(
        ZitadelUserSearchRequest request,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = "v2/users";

        return await PostAsync<ZitadelUserSearchResponse>(url, request, orgId, cancellationToken);
    }

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    public async Task<Result> UpdateUserProfileAsync(
        string userId,
        ZitadelCreateProfile profile,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}";

        var request = new { profile };
        var result = await PutAsync<object>(url, request, orgId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Deactivates a user.
    /// </summary>
    public async Task<Result> DeactivateUserAsync(
        string userId,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}/deactivate";

        var result = await PostAsync<object>(url, new { }, orgId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Reactivates a user.
    /// </summary>
    public async Task<Result> ReactivateUserAsync(
        string userId,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}/reactivate";

        var result = await PostAsync<object>(url, new { }, orgId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    public async Task<Result> DeleteUserAsync(
        string userId,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}";

        return await DeleteAsync(url, orgId, cancellationToken);
    }

    /// <summary>
    /// Sends a password reset email to a user.
    /// </summary>
    public async Task<Result> InitiatePasswordResetAsync(
        string userId,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        var orgId = organizationId ?? _options.DefaultOrganizationId;
        var url = $"v2/users/{userId}/password_reset";

        var result = await PostAsync<object>(url, new { sendLink = new { } }, orgId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Introspects a token.
    /// </summary>
    public async Task<Result<ZitadelIntrospectionResponse>> IntrospectTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
            ["client_id"] = _options.ClientId ?? string.Empty,
            ["client_secret"] = _options.ClientSecret ?? string.Empty
        });

        try
        {
            var response = await _httpClient.PostAsync(_options.IntrospectionEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Token introspection failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return IdentityErrors.InvalidToken;
            }

            var result = await response.Content.ReadFromJsonAsync<ZitadelIntrospectionResponse>(JsonOptions, cancellationToken);
            return result ?? new ZitadelIntrospectionResponse { Active = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error introspecting token");
            return IdentityErrors.ProviderUnavailable;
        }
    }

    /// <summary>
    /// Creates an organization.
    /// </summary>
    public async Task<Result<ZitadelOrganization>> CreateOrganizationAsync(
        ZitadelCreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = "management/v1/orgs";
        return await PostAsync<ZitadelOrganization>(url, request, null, cancellationToken);
    }

    /// <summary>
    /// Gets an organization by ID.
    /// </summary>
    public async Task<Result<ZitadelOrganization>> GetOrganizationAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = $"management/v1/orgs/{organizationId}";
        return await GetAsync<ZitadelOrganization>(url, organizationId, cancellationToken);
    }

    /// <summary>
    /// Adds a member to an organization.
    /// </summary>
    public async Task<Result> AddOrganizationMemberAsync(
        string organizationId,
        ZitadelAddMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = $"management/v1/orgs/{organizationId}/members";

        var result = await PostAsync<object>(url, request, organizationId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Removes a member from an organization.
    /// </summary>
    public async Task<Result> RemoveOrganizationMemberAsync(
        string organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        var url = $"management/v1/orgs/{organizationId}/members/{userId}";

        return await DeleteAsync(url, organizationId, cancellationToken);
    }

    /// <summary>
    /// Updates organization member roles.
    /// </summary>
    public async Task<Result> UpdateOrganizationMemberRolesAsync(
        string organizationId,
        string userId,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        var url = $"management/v1/orgs/{organizationId}/members/{userId}";

        var result = await PutAsync<object>(url, new { roles }, organizationId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Lists organization members.
    /// </summary>
    public async Task<Result<ZitadelMemberListResponse>> ListOrganizationMembersAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = $"management/v1/orgs/{organizationId}/members/_search";

        return await PostAsync<ZitadelMemberListResponse>(url, new { }, organizationId, cancellationToken);
    }

    /// <summary>
    /// Deactivates an organization.
    /// </summary>
    public async Task<Result> DeactivateOrganizationAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = $"management/v1/orgs/{organizationId}/_deactivate";

        var result = await PostAsync<object>(url, new { }, organizationId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    /// <summary>
    /// Creates a project within an organization.
    /// </summary>
    public async Task<Result<ZitadelProject>> CreateProjectAsync(
        ZitadelCreateProjectRequest request,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = "management/v1/projects";

        return await PostAsync<ZitadelProject>(url, request, organizationId, cancellationToken);
    }

    /// <summary>
    /// Creates an OIDC application within a project.
    /// </summary>
    public async Task<Result<ZitadelOidcApp>> CreateOidcApplicationAsync(
        string projectId,
        ZitadelCreateOidcAppRequest request,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(organizationId);
        var url = $"management/v1/projects/{projectId}/apps/oidc";

        return await PostAsync<ZitadelOidcApp>(url, request, organizationId, cancellationToken);
    }

    /// <summary>
    /// Updates an OIDC application's settings.
    /// </summary>
    public async Task<Result> UpdateOidcApplicationAsync(
        string projectId,
        string appId,
        ZitadelUpdateOidcAppRequest request,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(appId);
        ArgumentNullException.ThrowIfNull(request);
        var url = $"management/v1/projects/{projectId}/apps/{appId}/oidc";

        var result = await PutAsync<object>(url, request, organizationId, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    private async Task<Result<T>> GetAsync<T>(
        string url,
        string? organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Get, url, organizationId, cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during GET request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during GET request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
    }

    private async Task<Result<T>> PostAsync<T>(
        string url,
        object? body,
        string? organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Post, url, organizationId, cancellationToken);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during POST request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during POST request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
    }

    private async Task<Result<T>> PutAsync<T>(
        string url,
        object? body,
        string? organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Put, url, organizationId, cancellationToken);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ProcessResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during PUT request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during PUT request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
    }

    private async Task<Result> DeleteAsync(
        string url,
        string? organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await CreateRequestAsync(HttpMethod.Delete, url, organizationId, cancellationToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var error = await ParseErrorAsync(response, cancellationToken);
            return error;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during DELETE request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during DELETE request to {Url}", url);
            return IdentityErrors.ProviderUnavailable;
        }
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string url,
        string? organizationId,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        if (!string.IsNullOrEmpty(organizationId))
        {
            request.Headers.Add("x-zitadel-orgid", organizationId);
        }

        return request;
    }

    private async Task<Result<T>> ProcessResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object))
            {
                return Result.Success(default(T)!);
            }

            var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return content is not null ? Result.Success(content) : Result.Failure<T>(Error.Unexpected("Zitadel.EmptyResponse", "Empty response from Zitadel"));
        }

        var error = await ParseErrorAsync(response, cancellationToken);
        return Result.Failure<T>(error);
    }

    private async Task<Error> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var errorResponse = JsonSerializer.Deserialize<ZitadelErrorResponse>(content, JsonOptions);

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound =>
                    Error.NotFound("Zitadel.NotFound", errorResponse?.Message ?? "Resource not found"),
                System.Net.HttpStatusCode.Conflict =>
                    Error.Conflict("Zitadel.Conflict", errorResponse?.Message ?? "Resource conflict"),
                System.Net.HttpStatusCode.BadRequest =>
                    Error.Validation("Zitadel.BadRequest", errorResponse?.Message ?? "Bad request"),
                System.Net.HttpStatusCode.Unauthorized =>
                    Error.Unauthorized("Zitadel.Unauthorized", errorResponse?.Message ?? "Unauthorized"),
                System.Net.HttpStatusCode.Forbidden =>
                    Error.Forbidden("Zitadel.Forbidden", errorResponse?.Message ?? "Forbidden"),
                System.Net.HttpStatusCode.TooManyRequests =>
                    IdentityErrors.RateLimitExceeded,
                _ => Error.Failure("Zitadel.Error", errorResponse?.Message ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}")
            };
        }
        catch
        {
            return Error.Failure("Zitadel.Error", $"HTTP {(int)response.StatusCode}: {content}");
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // PAT configured → use directly (no token exchange needed)
        if (!string.IsNullOrEmpty(_options.PersonalAccessToken))
        {
            return _options.PersonalAccessToken;
        }

        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId ?? string.Empty,
                ["client_secret"] = _options.ClientSecret ?? string.Empty,
                ["scope"] = "openid urn:zitadel:iam:org:project:id:zitadel:aud"
            });

            var response = await _httpClient.PostAsync(_options.TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<ZitadelTokenResponse>(JsonOptions, cancellationToken);
            _accessToken = tokenResponse?.AccessToken ?? throw new InvalidOperationException("No access token received");
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds((tokenResponse?.ExpiresIn ?? 3600) - 60);

            _logger.LogDebug("Obtained new Zitadel access token, expires at {Expiry}", _tokenExpiry);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tokenLock.Dispose();
    }
}
