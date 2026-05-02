// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Implements organization service using Zitadel REST API.
/// </summary>
internal sealed class ZitadelOrganizationService : IOrganizationService
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelOrganizationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZitadelOrganizationService"/> class.
    /// </summary>
    public ZitadelOrganizationService(
        ZitadelHttpClient httpClient,
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelOrganizationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<IdentityOrganization>> CreateOrganizationAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Creating organization {Name}", request.Name);

        var zitadelRequest = new ZitadelCreateOrganizationRequest
        {
            Name = request.Name
        };

        var result = await _httpClient.CreateOrganizationAsync(zitadelRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create organization {Name}: {Error}",
                request.Name, result.Error.Message);
            return result.Error;
        }

        var organization = MapToIdentityOrganization(result.Value);
        _logger.LogInformation("Created organization {OrgId} with name {Name}",
            organization.Id, organization.Name);

        return organization;
    }

    /// <inheritdoc />
    public async Task<Result<IdentityOrganization>> GetOrganizationAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);

        _logger.LogDebug("Getting organization {OrgId}", organizationId);

        var result = await _httpClient.GetOrganizationAsync(organizationId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound"))
            {
                return IdentityErrors.OrganizationNotFound(organizationId);
            }
            return result.Error;
        }

        return MapToIdentityOrganization(result.Value);
    }

    /// <inheritdoc />
    public async Task<Result<IdentityOrganization>> GetOrganizationByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        _logger.LogDebug("Getting organization by name {Name}", name);

        var result = await _httpClient.SearchOrganizationsByNameAsync(name, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var match = result.Value.Result?.FirstOrDefault();
        if (match is null)
        {
            return IdentityErrors.OrganizationNotFoundByName(name);
        }

        return MapToIdentityOrganization(match);
    }

    /// <inheritdoc />
    public async Task<Result> AddMemberAsync(
        string organizationId,
        string userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roles);

        var rolesList = roles.ToList();
        _logger.LogInformation("Adding user {UserId} to organization {OrgId} with roles {Roles}",
            userId, organizationId, string.Join(", ", rolesList));

        var request = new ZitadelAddMemberRequest
        {
            UserId = userId,
            Roles = rolesList
        };

        var result = await _httpClient.AddOrganizationMemberAsync(organizationId, request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to add user {UserId} to organization {OrgId}: {Error}",
                userId, organizationId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Added user {UserId} to organization {OrgId}", userId, organizationId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> RemoveMemberAsync(
        string organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);

        _logger.LogInformation("Removing user {UserId} from organization {OrgId}", userId, organizationId);

        var result = await _httpClient.RemoveOrganizationMemberAsync(organizationId, userId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to remove user {UserId} from organization {OrgId}: {Error}",
                userId, organizationId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Removed user {UserId} from organization {OrgId}", userId, organizationId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> UpdateMemberRolesAsync(
        string organizationId,
        string userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roles);

        var rolesList = roles.ToList();
        _logger.LogInformation("Updating roles for user {UserId} in organization {OrgId} to {Roles}",
            userId, organizationId, string.Join(", ", rolesList));

        var result = await _httpClient.UpdateOrganizationMemberRolesAsync(
            organizationId, userId, rolesList, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update roles for user {UserId} in organization {OrgId}: {Error}",
                userId, organizationId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Updated roles for user {UserId} in organization {OrgId}",
                userId, organizationId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrganizationMember>>> ListMembersAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);

        _logger.LogDebug("Listing members of organization {OrgId}", organizationId);

        var result = await _httpClient.ListOrganizationMembersAsync(organizationId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var members = result.Value.Result?
            .Select(MapToOrganizationMember)
            .ToList() ?? new List<OrganizationMember>();

        return members;
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateOrganizationAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);

        _logger.LogWarning("Deactivating organization {OrgId}", organizationId);

        var result = await _httpClient.DeactivateOrganizationAsync(organizationId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to deactivate organization {OrgId}: {Error}",
                organizationId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Deactivated organization {OrgId}", organizationId);
        }

        return result;
    }

    private static IdentityOrganization MapToIdentityOrganization(ZitadelOrganization zitadelOrg)
    {
        return new IdentityOrganization
        {
            Id = zitadelOrg.Id ?? string.Empty,
            Name = zitadelOrg.Name ?? string.Empty,
            Domain = zitadelOrg.PrimaryDomain,
            IsActive = zitadelOrg.State == "ORG_STATE_ACTIVE",
            CreatedAt = zitadelOrg.Details?.CreationDate ?? DateTimeOffset.MinValue,
            UpdatedAt = zitadelOrg.Details?.ChangeDate
        };
    }

    private static OrganizationMember MapToOrganizationMember(ZitadelOrgMember zitadelMember)
    {
        return new OrganizationMember
        {
            UserId = zitadelMember.UserId ?? string.Empty,
            Email = string.Empty, // Zitadel doesn't return email in member listing
            Roles = zitadelMember.Roles?.AsReadOnly() ?? new List<string>().AsReadOnly(),
            JoinedAt = zitadelMember.Details?.CreationDate ?? DateTimeOffset.MinValue,
            IsActive = true
        };
    }
}
