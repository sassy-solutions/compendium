// -----------------------------------------------------------------------
// <copyright file="ZitadelProjectIdentityProvisioner.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;
using Compendium.Core.Results;
using Microsoft.Extensions.Logging;

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

    /// <inheritdoc />
    public async Task<Result<ProjectProvisioningResult>> ProvisionProjectAsync(
        ProjectProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Creating Zitadel project '{ProjectName}' for org {ExternalOrganizationId}",
            request.ProjectName, request.ExternalOrganizationId);

        var projectResult = await _httpClient.CreateProjectAsync(
            new ZitadelCreateProjectRequest
            {
                Name = request.ProjectName,
                ProjectRoleAssertion = true,
                ProjectRoleCheck = true,
                HasProjectCheck = true
            },
            request.ExternalOrganizationId,
            cancellationToken);

        if (projectResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create Zitadel project for {ProjectName}: {Error}",
                request.ProjectName, projectResult.Error.Message);
            return projectResult.Error;
        }

        var externalProjectId = projectResult.Value.Id ?? string.Empty;
        _logger.LogInformation(
            "Created Zitadel project {ExternalProjectId} for project {ProjectId}",
            externalProjectId, request.ProjectId);

        return Result.Success(new ProjectProvisioningResult(ExternalProjectId: externalProjectId));
    }

    /// <inheritdoc />
    public async Task<Result<OidcAppProvisioningResult>> CreateOidcAppAsync(
        OidcAppProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Creating OIDC app '{AppName}' in Zitadel project {ExternalProjectId}",
            request.AppName, request.ExternalProjectId);

        var oidcResult = await _httpClient.CreateOidcApplicationAsync(
            request.ExternalProjectId,
            new ZitadelCreateOidcAppRequest
            {
                Name = request.AppName,
                RedirectUris = [.. request.RedirectUris],
                PostLogoutRedirectUris = [.. request.PostLogoutRedirectUris],
                ResponseTypes = ["OIDC_RESPONSE_TYPE_CODE"],
                GrantTypes = ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE"],
                AppType = "OIDC_APP_TYPE_WEB",
                AuthMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC",
                AccessTokenType = "OIDC_TOKEN_TYPE_JWT",
                AccessTokenRoleAssertion = true,
                IdTokenRoleAssertion = true,
                IdTokenUserinfoAssertion = true
            },
            request.ExternalOrganizationId,
            cancellationToken);

        if (oidcResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create OIDC app for project {ExternalProjectId}: {Error}",
                request.ExternalProjectId, oidcResult.Error.Message);
            return oidcResult.Error;
        }

        var clientId = oidcResult.Value.ClientId ?? string.Empty;
        var clientSecret = oidcResult.Value.ClientSecret ?? string.Empty;
        _logger.LogInformation(
            "Created OIDC app with clientId {ClientId} in project {ExternalProjectId}",
            clientId, request.ExternalProjectId);

        var appId = oidcResult.Value.AppId;
        return Result.Success(new OidcAppProvisioningResult(ClientId: clientId, ClientSecret: clientSecret, ExternalAppId: appId));
    }

    /// <inheritdoc />
    public async Task<Result> UpdateOidcAppAsync(
        OidcAppUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Updating OIDC app {ExternalAppId} in project {ExternalProjectId}",
            request.ExternalAppId, request.ExternalProjectId);

        return await _httpClient.UpdateOidcApplicationAsync(
            request.ExternalProjectId,
            request.ExternalAppId,
            new ZitadelUpdateOidcAppRequest
            {
                RedirectUris = [.. request.RedirectUris],
                PostLogoutRedirectUris = [.. request.PostLogoutRedirectUris],
                ResponseTypes = ["OIDC_RESPONSE_TYPE_CODE"],
                GrantTypes = ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE"],
                AppType = "OIDC_APP_TYPE_WEB",
                AuthMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC",
                AccessTokenType = "OIDC_TOKEN_TYPE_JWT",
                AccessTokenRoleAssertion = true,
                IdTokenRoleAssertion = true,
                IdTokenUserinfoAssertion = true
            },
            request.ExternalOrganizationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteOidcAppAsync(
        OidcAppDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Deleting OIDC app {ExternalAppId} from project {ExternalProjectId}",
            request.ExternalAppId, request.ExternalProjectId);

        return await _httpClient.DeleteApplicationAsync(
            request.ExternalProjectId,
            request.ExternalAppId,
            request.ExternalOrganizationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<OidcAppSecretRotationResult>> RotateOidcAppSecretAsync(
        OidcAppSecretRotationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Rotating secret for OIDC app {ExternalAppId} in project {ExternalProjectId}",
            request.ExternalAppId, request.ExternalProjectId);

        var result = await _httpClient.RegenerateOidcClientSecretAsync(
            request.ExternalProjectId,
            request.ExternalAppId,
            request.ExternalOrganizationId,
            cancellationToken);

        if (result.IsFailure)
            return result.Error;

        return Result.Success(new OidcAppSecretRotationResult(ClientSecret: result.Value.ClientSecret ?? string.Empty));
    }
}
