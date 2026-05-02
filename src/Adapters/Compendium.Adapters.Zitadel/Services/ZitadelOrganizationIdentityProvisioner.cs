// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationIdentityProvisioner.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Provisions Zitadel identity resources (organization, project, OIDC app, admin user)
/// when a new tenant-level organization is created.
/// </summary>
/// <remarks>
/// <para>
/// Redirect URI templates for the OIDC app are <b>not hardcoded</b>. They are read from
/// <see cref="ZitadelOptions.RedirectUriTemplate"/> and
/// <see cref="ZitadelOptions.PostLogoutUriTemplate"/>. The templates must contain the
/// literal <see cref="ZitadelOptions.OrganizationPlaceholder"/> placeholder which is
/// substituted with the provisioned organization name.
/// </para>
/// </remarks>
internal sealed class ZitadelOrganizationIdentityProvisioner : IOrganizationIdentityProvisioner
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly IOrganizationService _organizationService;
    private readonly IIdentityUserService _userService;
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelOrganizationIdentityProvisioner> _logger;

    public ZitadelOrganizationIdentityProvisioner(
        ZitadelHttpClient httpClient,
        IOrganizationService organizationService,
        IIdentityUserService userService,
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelOrganizationIdentityProvisioner> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<OrganizationProvisioningResult>> ProvisionAsync(
        OrganizationProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.AdminUser);

        _logger.LogInformation(
            "Provisioning identity for organization {OrganizationId} ({OrganizationName})",
            request.OrganizationId, request.Name);

        // Step 0: Resolve redirect URI templates from options (no hardcoded URLs)
        var redirectUriResult = ResolveTemplate(
            _options.RedirectUriTemplate, nameof(ZitadelOptions.RedirectUriTemplate), request.Name);
        if (redirectUriResult.IsFailure)
        {
            return redirectUriResult.Error;
        }

        var postLogoutUriResult = ResolveTemplate(
            _options.PostLogoutUriTemplate, nameof(ZitadelOptions.PostLogoutUriTemplate), request.Name);
        if (postLogoutUriResult.IsFailure)
        {
            return postLogoutUriResult.Error;
        }

        // Step 1: Create Zitadel organization (idempotent — reuse on Conflict).
        // Mirrors Step 4's user-conflict handling: if the org already exists in
        // Zitadel (typical when a previous saga run created the org but failed
        // later, e.g. on the project step), look it up by name and reuse the id.
        // Without this, every retry leaks an orphan org because each create
        // generates a new id even when Zitadel rejects the request.
        var displayName = request.DisplayName ?? request.Name;
        var orgResult = await _organizationService.CreateOrganizationAsync(
            new CreateOrganizationRequest { Name = displayName },
            cancellationToken);

        string zitadelOrgId;
        if (orgResult.IsFailure)
        {
            if (orgResult.Error.Type != ErrorType.Conflict)
            {
                _logger.LogWarning(
                    "Failed to create Zitadel organization for {OrganizationName}: {Error}",
                    request.Name, orgResult.Error.Message);
                return orgResult.Error;
            }

            _logger.LogInformation(
                "Zitadel organization already exists for {OrganizationName}; looking up to reuse",
                request.Name);

            var existingOrg = await _organizationService.GetOrganizationByNameAsync(
                displayName, cancellationToken);
            if (existingOrg.IsFailure)
            {
                _logger.LogWarning(
                    "Organization create reported conflict but lookup by name failed: {Error}",
                    existingOrg.Error.Message);
                return existingOrg.Error;
            }

            zitadelOrgId = existingOrg.Value.Id;
            _logger.LogInformation(
                "Reusing existing Zitadel organization {ZitadelOrgId} for {OrganizationName}",
                zitadelOrgId, request.Name);
        }
        else
        {
            zitadelOrgId = orgResult.Value.Id;
            _logger.LogInformation("Created Zitadel organization {ZitadelOrgId}", zitadelOrgId);
        }

        // Step 2: Create project (idempotent — reuse on Conflict).
        // Same shape as Step 1: project names within an org are unique in Zitadel,
        // so a Conflict here means the project survives from a previous saga run.
        var projectName = $"nexus-{request.Name}";
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

        string projectId;
        if (projectResult.IsFailure)
        {
            if (projectResult.Error.Type != ErrorType.Conflict)
            {
                _logger.LogWarning(
                    "Failed to create project for organization {ZitadelOrgId}: {Error}",
                    zitadelOrgId, projectResult.Error.Message);
                return projectResult.Error;
            }

            _logger.LogInformation(
                "Project {ProjectName} already exists in Zitadel organization {ZitadelOrgId}; looking up to reuse",
                projectName, zitadelOrgId);

            var existingProject = await _httpClient.GetProjectByNameAsync(
                projectName, zitadelOrgId, cancellationToken);
            if (existingProject.IsFailure)
            {
                _logger.LogWarning(
                    "Project create reported conflict but lookup by name failed: {Error}",
                    existingProject.Error.Message);
                return existingProject.Error;
            }

            projectId = existingProject.Value.Id ?? string.Empty;
            _logger.LogInformation(
                "Reusing existing project {ProjectId} in organization {ZitadelOrgId}",
                projectId, zitadelOrgId);
        }
        else
        {
            projectId = projectResult.Value.Details?.ResourceOwner is not null
                ? projectResult.Value.Id ?? projectResult.Value.Details.ResourceOwner
                : projectResult.Value.Id ?? string.Empty;
            _logger.LogInformation("Created project {ProjectId} for organization {ZitadelOrgId}",
                projectId, zitadelOrgId);
        }

        // Step 3: Create OIDC application using URIs resolved in Step 0.
        // Conflict here is NOT recoverable the same way as Steps 1, 2, 4.
        // Zitadel returns the OIDC client_secret only once, at creation time. If
        // we silently reused a found app, the saga's downstream secrets step
        // would persist an empty/wrong secret, leaving the OIDC app permanently
        // broken from the platform's point of view. Fail fast with a clearly
        // labelled error so an operator knows to rotate the secret in Zitadel
        // and re-run provisioning. Cleanup of the org/project created above is
        // the saga's responsibility — the provisioner does not roll back.
        var appName = $"nexus-{request.Name}-app";
        var oidcResult = await _httpClient.CreateOidcApplicationAsync(
            projectId,
            new ZitadelCreateOidcAppRequest
            {
                Name = appName,
                RedirectUris = [redirectUriResult.Value],
                PostLogoutRedirectUris = [postLogoutUriResult.Value],
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
            if (oidcResult.Error.Type == ErrorType.Conflict)
            {
                _logger.LogWarning(
                    "OIDC application {AppName} already exists in project {ProjectId}: client_secret " +
                    "is no longer recoverable. Operator must rotate the OIDC secret in Zitadel and retry.",
                    appName, projectId);
                return Error.Conflict(
                    "Zitadel.OidcAppExistsButSecretLost",
                    $"OIDC application '{appName}' already exists in project '{projectId}'. " +
                    "Zitadel only returns the client_secret at creation time, so it cannot be " +
                    "recovered by lookup. Rotate the OIDC client secret for this application in " +
                    "Zitadel (or delete the application) and re-run provisioning.");
            }

            _logger.LogWarning(
                "Failed to create OIDC application for project {ProjectId}: {Error}",
                projectId, oidcResult.Error.Message);
            return oidcResult.Error;
        }

        var clientId = oidcResult.Value.ClientId ?? string.Empty;
        var clientSecret = oidcResult.Value.ClientSecret ?? string.Empty;
        _logger.LogInformation("Created OIDC app with clientId {ClientId} for project {ProjectId}",
            clientId, projectId);

        // Step 4: Create admin user (idempotent — reuse existing user if present)
        // Zitadel usernames are unique across the entire instance; the same human can
        // legitimately be admin of multiple orgs. When CreateUserAsync returns a
        // conflict, fall back to GetUserByEmailAsync and reuse the existing user id
        // for the membership step. Without this, every subsequent Org provisioning
        // for the same admin email leaves an orphan Zitadel org behind and gets
        // stuck in `Provisioning` on the Nexus side.
        var userResult = await _userService.CreateUserAsync(
            new CreateUserRequest
            {
                Email = request.AdminUser.Email,
                FirstName = request.AdminUser.FirstName,
                LastName = request.AdminUser.LastName,
                Password = request.AdminUser.Password,
                OrganizationId = zitadelOrgId,
                SendVerificationEmail = request.AdminUser.Password is null
            },
            cancellationToken);

        string adminUserId;
        if (userResult.IsFailure)
        {
            // Conflict on user creation → look up the existing user by email and reuse it.
            // Any other failure (auth, network, validation) propagates as before.
            if (userResult.Error.Type != ErrorType.Conflict)
            {
                _logger.LogWarning(
                    "Failed to create admin user for organization {ZitadelOrgId}: {Error}",
                    zitadelOrgId, userResult.Error.Message);
                return userResult.Error;
            }

            _logger.LogInformation(
                "Admin user already exists in Zitadel for organization {ZitadelOrgId}; looking up to reuse",
                zitadelOrgId);

            var existing = await _userService.GetUserByEmailAsync(request.AdminUser.Email, cancellationToken);
            if (existing.IsFailure)
            {
                _logger.LogWarning(
                    "Admin user creation reported conflict but lookup by email failed: {Error}",
                    existing.Error.Message);
                return existing.Error;
            }

            adminUserId = existing.Value.Id;
            _logger.LogInformation(
                "Reusing existing admin user {AdminUserId} for organization {ZitadelOrgId}",
                adminUserId, zitadelOrgId);
        }
        else
        {
            adminUserId = userResult.Value.Id;
            _logger.LogInformation("Created admin user {AdminUserId} for organization {ZitadelOrgId}",
                adminUserId, zitadelOrgId);
        }

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
            "Identity provisioning complete for organization {OrganizationId}: ExternalOrg={ZitadelOrgId}, Project={ProjectId}, AdminUser={AdminUserId}",
            request.OrganizationId, zitadelOrgId, projectId, adminUserId);

        return new OrganizationProvisioningResult(
            ExternalOrganizationId: zitadelOrgId,
            ExternalProjectId: projectId,
            ClientId: clientId,
            ClientSecret: clientSecret,
            AdminUserId: adminUserId);
    }

    /// <summary>
    /// Validates and substitutes the <see cref="ZitadelOptions.OrganizationPlaceholder"/>
    /// in a configured URI template.
    /// </summary>
    private static Result<string> ResolveTemplate(
        string? template,
        string settingName,
        string organizationName)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return Error.Validation(
                "Zitadel.TemplateMissing",
                $"Zitadel option '{settingName}' is not configured. Set 'Zitadel:{settingName}' " +
                "in configuration (e.g. appsettings.json or environment variable " +
                $"'Zitadel__{settingName}').");
        }

        if (!template.Contains(ZitadelOptions.OrganizationPlaceholder, StringComparison.Ordinal))
        {
            return Error.Validation(
                "Zitadel.TemplateMissingPlaceholder",
                $"Zitadel option '{settingName}' must contain the placeholder " +
                $"'{ZitadelOptions.OrganizationPlaceholder}'.");
        }

        return template.Replace(
            ZitadelOptions.OrganizationPlaceholder,
            organizationName,
            StringComparison.Ordinal);
    }
}
