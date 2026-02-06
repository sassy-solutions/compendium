// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationIdentityProvisioner.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;
using Nexus.Core.Domain.ValueObjects;
using Nexus.Core.Ports.Platform;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Provisions Zitadel identity resources (organization, project, OIDC app, admin user)
/// for a new Nexus organization.
/// </summary>
internal sealed class ZitadelOrganizationIdentityProvisioner : IOrganizationIdentityProvisioner
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly IOrganizationService _organizationService;
    private readonly IIdentityUserService _userService;
    private readonly ILogger<ZitadelOrganizationIdentityProvisioner> _logger;

    public ZitadelOrganizationIdentityProvisioner(
        ZitadelHttpClient httpClient,
        IOrganizationService organizationService,
        IIdentityUserService userService,
        ILogger<ZitadelOrganizationIdentityProvisioner> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<OrganizationIdentityResult>> ProvisionIdentityAsync(
        OrganizationId organizationId,
        string organizationName,
        string? displayName,
        string planId,
        AdminUserRequest adminUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(organizationName);
        ArgumentNullException.ThrowIfNull(adminUser);

        _logger.LogInformation(
            "Provisioning identity for organization {OrganizationId} ({OrganizationName})",
            organizationId.Value, organizationName);

        // Step 1: Create Zitadel organization
        var orgResult = await _organizationService.CreateOrganizationAsync(
            new CreateOrganizationRequest { Name = displayName ?? organizationName },
            cancellationToken);

        if (orgResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create Zitadel organization for {OrganizationName}: {Error}",
                organizationName, orgResult.Error.Message);
            return orgResult.Error;
        }

        var zitadelOrgId = orgResult.Value.Id;
        _logger.LogInformation("Created Zitadel organization {ZitadelOrgId}", zitadelOrgId);

        // Step 2: Create project
        var projectResult = await _httpClient.CreateProjectAsync(
            new ZitadelCreateProjectRequest
            {
                Name = $"nexus-{organizationName}",
                ProjectRoleAssertion = true,
                ProjectRoleCheck = true,
                HasProjectCheck = true
            },
            zitadelOrgId,
            cancellationToken);

        if (projectResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create project for organization {ZitadelOrgId}: {Error}",
                zitadelOrgId, projectResult.Error.Message);
            return projectResult.Error;
        }

        var projectId = projectResult.Value.Details?.ResourceOwner is not null
            ? projectResult.Value.Id ?? projectResult.Value.Details.ResourceOwner
            : projectResult.Value.Id ?? string.Empty;
        _logger.LogInformation("Created project {ProjectId} for organization {ZitadelOrgId}",
            projectId, zitadelOrgId);

        // Step 3: Create OIDC application
        var oidcResult = await _httpClient.CreateOidcApplicationAsync(
            projectId,
            new ZitadelCreateOidcAppRequest
            {
                Name = $"nexus-{organizationName}-app",
                RedirectUris = [$"https://{organizationName}.admin.sassy.solutions/api/auth/callback/zitadel"],
                PostLogoutRedirectUris = [$"https://{organizationName}.admin.sassy.solutions"],
                ResponseTypes = ["OIDC_RESPONSE_TYPE_CODE"],
                GrantTypes = ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE"],
                AppType = "OIDC_APP_TYPE_WEB",
                AuthMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC",
                AccessTokenType = "OIDC_TOKEN_TYPE_JWT",
                AccessTokenRoleAssertion = true,
                IdTokenRoleAssertion = true,
                IdTokenUserinfoAssertion = true
            },
            zitadelOrgId,
            cancellationToken);

        if (oidcResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create OIDC application for project {ProjectId}: {Error}",
                projectId, oidcResult.Error.Message);
            return oidcResult.Error;
        }

        var clientId = oidcResult.Value.ClientId ?? string.Empty;
        var clientSecret = oidcResult.Value.ClientSecret ?? string.Empty;
        _logger.LogInformation("Created OIDC app with clientId {ClientId} for project {ProjectId}",
            clientId, projectId);

        // Step 4: Create admin user
        var userResult = await _userService.CreateUserAsync(
            new CreateUserRequest
            {
                Email = adminUser.Email,
                FirstName = adminUser.FirstName,
                LastName = adminUser.LastName,
                Password = adminUser.Password,
                OrganizationId = zitadelOrgId,
                SendVerificationEmail = adminUser.Password is null
            },
            cancellationToken);

        if (userResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create admin user {Email} for organization {ZitadelOrgId}: {Error}",
                adminUser.Email, zitadelOrgId, userResult.Error.Message);
            return userResult.Error;
        }

        var adminUserId = userResult.Value.Id;
        _logger.LogInformation("Created admin user {AdminUserId} for organization {ZitadelOrgId}",
            adminUserId, zitadelOrgId);

        // Step 5: Add user as organization member with ORG_OWNER role
        var memberResult = await _organizationService.AddMemberAsync(
            zitadelOrgId,
            adminUserId,
            ["ORG_OWNER"],
            cancellationToken);

        if (memberResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to add admin user {AdminUserId} as member of organization {ZitadelOrgId}: {Error}",
                adminUserId, zitadelOrgId, memberResult.Error.Message);
            return memberResult.Error;
        }

        _logger.LogInformation(
            "Identity provisioning complete for organization {OrganizationId}: ZitadelOrg={ZitadelOrgId}, Project={ProjectId}, AdminUser={AdminUserId}",
            organizationId.Value, zitadelOrgId, projectId, adminUserId);

        return new OrganizationIdentityResult(
            ZitadelOrganizationId: zitadelOrgId,
            ProjectId: projectId,
            ClientId: clientId,
            ClientSecret: clientSecret,
            AdminUserId: adminUserId);
    }
}
