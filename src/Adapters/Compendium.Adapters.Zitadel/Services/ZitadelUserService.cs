// -----------------------------------------------------------------------
// <copyright file="ZitadelUserService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Implements identity user service using Zitadel REST API.
/// </summary>
internal sealed class ZitadelUserService : IIdentityUserService
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly ITenantContext _tenantContext;
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelUserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZitadelUserService"/> class.
    /// </summary>
    public ZitadelUserService(
        ZitadelHttpClient httpClient,
        ITenantContext tenantContext,
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelUserService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<IdentityUser>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = GetOrganizationId(request.OrganizationId);

        _logger.LogInformation("Creating user with email {Email} in organization {OrgId}",
            request.Email, orgId);

        var zitadelRequest = new ZitadelCreateUserRequest
        {
            UserName = request.Username ?? request.Email,
            Profile = new ZitadelCreateProfile
            {
                FirstName = request.FirstName ?? "User",
                LastName = request.LastName ?? "User",
                DisplayName = request.DisplayName,
                PreferredLanguage = request.PreferredLanguage
            },
            Email = new ZitadelCreateEmail
            {
                Email = request.Email,
                IsEmailVerified = !request.SendVerificationEmail
            },
            Phone = string.IsNullOrEmpty(request.PhoneNumber) ? null : new ZitadelCreatePhone
            {
                Phone = request.PhoneNumber
            },
            Password = string.IsNullOrEmpty(request.Password) ? null : new ZitadelCreatePassword
            {
                Password = request.Password,
                ChangeRequired = false
            }
        };

        var result = await _httpClient.CreateUserAsync(zitadelRequest, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create user {Email}: {Error}", request.Email, result.Error.Message);
            return result.Error;
        }

        var user = MapToIdentityUser(result.Value, orgId);
        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

        return user;
    }

    /// <inheritdoc />
    public async Task<Result<IdentityUser>> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var orgId = GetOrganizationId();

        _logger.LogDebug("Getting user by ID {UserId}", userId);

        var result = await _httpClient.GetUserByIdAsync(userId, orgId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound"))
            {
                return IdentityErrors.UserNotFound(userId);
            }
            return result.Error;
        }

        return MapToIdentityUser(result.Value, orgId);
    }

    /// <inheritdoc />
    public async Task<Result<IdentityUser>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var orgId = GetOrganizationId();

        _logger.LogDebug("Getting user by email {Email}", email);

        var searchRequest = new ZitadelUserSearchRequest
        {
            Query = new ZitadelSearchQuery { Limit = 1 },
            Queries = new List<ZitadelUserQuery>
            {
                new() { EmailQuery = new ZitadelEmailQuery { EmailAddress = email, Method = "TEXT_QUERY_METHOD_EQUALS" } }
            }
        };

        var result = await _httpClient.SearchUsersAsync(searchRequest, orgId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var user = result.Value.Result?.FirstOrDefault();
        if (user is null)
        {
            return IdentityErrors.UserNotFoundByEmail(email);
        }

        return MapToIdentityUser(user, orgId);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(request);

        var orgId = GetOrganizationId();

        _logger.LogInformation("Updating user {UserId}", userId);

        var profile = new ZitadelCreateProfile
        {
            FirstName = request.FirstName ?? "User",
            LastName = request.LastName ?? "User",
            DisplayName = request.DisplayName,
            PreferredLanguage = request.PreferredLanguage
        };

        var result = await _httpClient.UpdateUserProfileAsync(userId, profile, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update user {UserId}: {Error}", userId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Updated user {UserId}", userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var orgId = GetOrganizationId();

        _logger.LogInformation("Deactivating user {UserId}", userId);

        var result = await _httpClient.DeactivateUserAsync(userId, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to deactivate user {UserId}: {Error}", userId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Deactivated user {UserId}", userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> ReactivateUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var orgId = GetOrganizationId();

        _logger.LogInformation("Reactivating user {UserId}", userId);

        var result = await _httpClient.ReactivateUserAsync(userId, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to reactivate user {UserId}: {Error}", userId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Reactivated user {UserId}", userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<IdentityUser>>> ListUsersAsync(
        ListUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = GetOrganizationId(request.OrganizationId);

        _logger.LogDebug("Listing users, page {Page}, size {PageSize}", request.Page, request.PageSize);

        var queries = new List<ZitadelUserQuery>();

        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            queries.Add(new ZitadelUserQuery
            {
                DisplayNameQuery = new ZitadelDisplayNameQuery
                {
                    DisplayName = request.SearchQuery,
                    Method = "TEXT_QUERY_METHOD_CONTAINS_IGNORE_CASE"
                }
            });
        }

        var searchRequest = new ZitadelUserSearchRequest
        {
            Query = new ZitadelSearchQuery
            {
                Offset = (request.Page - 1) * request.PageSize,
                Limit = request.PageSize,
                Asc = !request.SortDescending
            },
            Queries = queries.Count > 0 ? queries : null
        };

        var result = await _httpClient.SearchUsersAsync(searchRequest, orgId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var users = result.Value.Result?
            .Select(u => MapToIdentityUser(u, orgId))
            .ToList() ?? new List<IdentityUser>();

        return new PagedResult<IdentityUser>
        {
            Items = users,
            TotalCount = (int)(result.Value.Details?.TotalResult ?? users.Count),
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var orgId = GetOrganizationId();

        _logger.LogWarning("Deleting user {UserId}", userId);

        var result = await _httpClient.DeleteUserAsync(userId, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to delete user {UserId}: {Error}", userId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Deleted user {UserId}", userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> InitiatePasswordResetAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var orgId = GetOrganizationId();

        _logger.LogInformation("Initiating password reset for user {UserId}", userId);

        var result = await _httpClient.InitiatePasswordResetAsync(userId, orgId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to initiate password reset for user {UserId}: {Error}",
                userId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Password reset initiated for user {UserId}", userId);
        }

        return result;
    }

    private string? GetOrganizationId(string? requestOrgId = null)
    {
        return requestOrgId ?? _tenantContext.TenantId ?? _options.DefaultOrganizationId;
    }

    private static IdentityUser MapToIdentityUser(ZitadelUser zitadelUser, string? organizationId)
    {
        var userId = zitadelUser.UserId ?? zitadelUser.Id ?? string.Empty;
        var email = zitadelUser.Human?.Email?.Email ?? zitadelUser.PreferredLoginName ?? string.Empty;

        return new IdentityUser
        {
            Id = userId,
            Email = email,
            Username = zitadelUser.UserName,
            FirstName = zitadelUser.Human?.Profile?.FirstName,
            LastName = zitadelUser.Human?.Profile?.LastName,
            DisplayName = zitadelUser.Human?.Profile?.DisplayName,
            PhoneNumber = zitadelUser.Human?.Phone?.Phone,
            EmailVerified = zitadelUser.Human?.Email?.IsEmailVerified ?? false,
            PhoneVerified = zitadelUser.Human?.Phone?.IsPhoneVerified ?? false,
            IsActive = zitadelUser.State == "USER_STATE_ACTIVE",
            PreferredLanguage = zitadelUser.Human?.Profile?.PreferredLanguage,
            ProfilePictureUrl = zitadelUser.Human?.Profile?.AvatarUrl,
            CreatedAt = zitadelUser.Details?.CreationDate ?? DateTimeOffset.MinValue,
            UpdatedAt = zitadelUser.Details?.ChangeDate,
            OrganizationId = organizationId ?? zitadelUser.Details?.ResourceOwner
        };
    }
}
