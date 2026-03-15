// -----------------------------------------------------------------------
// <copyright file="ZitadelProjectIdentityProvisioner.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;
using Compendium.Core.Results;
using Microsoft.Extensions.Logging;
using Nexus.Core.Domain.ValueObjects;
using Nexus.Core.Ports.Platform;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Zitadel implementation of <see cref="IProjectIdentityProvisioner"/>.
/// Creates Zitadel Projects and OIDC applications within an existing Zitadel organization.
/// </summary>
internal sealed class ZitadelProjectIdentityProvisioner : IProjectIdentityProvisioner
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly ILogger<ZitadelProjectIdentityProvisioner> _logger;

    public ZitadelProjectIdentityProvisioner(
        ZitadelHttpClient httpClient,
        ILogger<ZitadelProjectIdentityProvisioner> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ProjectIdentityResult>> ProvisionProjectAsync(
        ProjectId projectId,
        OrganizationId organizationId,
        string zitadelOrgId,
        string projectName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating Zitadel project '{ProjectName}' for org {ZitadelOrgId}",
            projectName, zitadelOrgId);

        var projectResult = await _httpClient.CreateProjectAsync(
            new ZitadelCreateProjectRequest
            {
                Name = projectName,
                ProjectRoleAssertion = true,
                ProjectRoleCheck = true,
                HasProjectCheck = true
            },
            zitadelOrgId,
            cancellationToken);

        if (projectResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create Zitadel project for {ProjectName}: {Error}",
                projectName, projectResult.Error.Message);
            return projectResult.Error;
        }

        var zitadelProjectId = projectResult.Value.Id ?? string.Empty;
        _logger.LogInformation(
            "Created Zitadel project {ZitadelProjectId} for Nexus project {ProjectId}",
            zitadelProjectId, projectId.Value);

        return Result.Success(new ProjectIdentityResult(ZitadelProjectId: zitadelProjectId));
    }

    public async Task<Result<OidcAppResult>> CreateOidcAppAsync(
        string zitadelProjectId,
        string zitadelOrgId,
        string appName,
        List<string> redirectUris,
        List<string> postLogoutRedirectUris,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating OIDC app '{AppName}' in Zitadel project {ZitadelProjectId}",
            appName, zitadelProjectId);

        var oidcResult = await _httpClient.CreateOidcApplicationAsync(
            zitadelProjectId,
            new ZitadelCreateOidcAppRequest
            {
                Name = appName,
                RedirectUris = redirectUris,
                PostLogoutRedirectUris = postLogoutRedirectUris,
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
                "Failed to create OIDC app for project {ZitadelProjectId}: {Error}",
                zitadelProjectId, oidcResult.Error.Message);
            return oidcResult.Error;
        }

        var clientId = oidcResult.Value.ClientId ?? string.Empty;
        var clientSecret = oidcResult.Value.ClientSecret ?? string.Empty;
        _logger.LogInformation(
            "Created OIDC app with clientId {ClientId} in project {ZitadelProjectId}",
            clientId, zitadelProjectId);

        return Result.Success(new OidcAppResult(ClientId: clientId, ClientSecret: clientSecret));
    }
}
