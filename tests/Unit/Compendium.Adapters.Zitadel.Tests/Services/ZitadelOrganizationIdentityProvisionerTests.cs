// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationIdentityProvisionerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity;
using Compendium.Abstractions.Identity.Models;
using Compendium.Abstractions.Identity.Models.Requests;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;
using Compendium.Adapters.Zitadel.Services;
using Compendium.Core.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.Zitadel.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ZitadelOrganizationIdentityProvisioner"/>'s
/// idempotent-create behaviour.
/// </summary>
/// <remarks>
/// Mirrors the user-conflict tests already covered by PR #41. We verify three
/// additional Conflict paths:
/// <list type="bullet">
/// <item>Org Conflict → reuse existing org id.</item>
/// <item>Project Conflict → reuse existing project id.</item>
/// <item>OIDC app Conflict → fail fast with <c>Zitadel.OidcAppExistsButSecretLost</c>
/// (cannot reuse: client_secret is only returned at creation time).</item>
/// </list>
/// </remarks>
public class ZitadelOrganizationIdentityProvisionerTests
{
    private const string OrgName = "acme";
    private const string DisplayName = "ACME";
    private const string ZitadelOrgId = "11111111";
    private const string ProjectId = "22222222";
    private const string AdminUserId = "33333333";
    private const string AdminEmail = "admin@acme.test";

    private static readonly OrganizationProvisioningRequest Request = new(
        OrganizationId: "org-aggregate-id",
        Name: OrgName,
        DisplayName: DisplayName,
        PlanId: "free",
        AdminUser: new AdminUserProvisioningRequest(
            Email: AdminEmail,
            FirstName: "Ada",
            LastName: "Lovelace",
            Password: null,
            PreferredLanguage: "en"));

    [Fact]
    public async Task ProvisionAsync_OrganizationConflict_ReusesExistingOrg()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var httpClient = CreateFakeHttpClient();

        orgService
            .CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityOrganization>(
                Error.Conflict("Zitadel.Conflict", "Organization already exists")));
        orgService
            .GetOrganizationByNameAsync(DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization
            {
                Id = ZitadelOrgId,
                Name = DisplayName,
                IsActive = true
            }));
        orgService
            .AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        httpClient
            .CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId, Name = $"nexus-{OrgName}" }));
        httpClient
            .CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp
            {
                AppId = "app-1",
                ClientId = "client-1",
                ClientSecret = "secret-1"
            }));

        userService
            .CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var provisioner = CreateProvisioner(httpClient, orgService, userService);

        // Act
        var result = await provisioner.ProvisionAsync(Request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalOrganizationId.Should().Be(ZitadelOrgId);
        result.Value.ExternalProjectId.Should().Be(ProjectId);
        result.Value.ClientSecret.Should().Be("secret-1");

        await orgService.Received(1)
            .GetOrganizationByNameAsync(DisplayName, Arg.Any<CancellationToken>());
        await orgService.Received(1)
            .AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_ProjectConflict_ReusesExistingProject()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var httpClient = CreateFakeHttpClient();

        orgService
            .CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        orgService
            .AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        httpClient
            .CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelProject>(
                Error.Conflict("Zitadel.Conflict", "Project name already taken in this org")));
        httpClient
            .GetProjectByNameAsync($"nexus-{OrgName}", ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId, Name = $"nexus-{OrgName}" }));
        httpClient
            .CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp
            {
                AppId = "app-1",
                ClientId = "client-1",
                ClientSecret = "secret-1"
            }));

        userService
            .CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var provisioner = CreateProvisioner(httpClient, orgService, userService);

        // Act
        var result = await provisioner.ProvisionAsync(Request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProjectId.Should().Be(ProjectId);
        result.Value.ExternalOrganizationId.Should().Be(ZitadelOrgId);
        result.Value.ClientSecret.Should().Be("secret-1");

        await httpClient.Received(1)
            .GetProjectByNameAsync($"nexus-{OrgName}", ZitadelOrgId, Arg.Any<CancellationToken>());
        await orgService.Received(1)
            .AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_OidcAppConflict_ReturnsClearError_AndDoesNotCleanUpEarlierResources()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var httpClient = CreateFakeHttpClient();

        orgService
            .CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));

        httpClient
            .CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId, Name = $"nexus-{OrgName}" }));
        httpClient
            .CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelOidcApp>(
                Error.Conflict("Zitadel.Conflict", "OIDC app name already taken in project")));

        var provisioner = CreateProvisioner(httpClient, orgService, userService);

        // Act
        var result = await provisioner.ProvisionAsync(Request);

        // Assert: caller gets a clearly-named conflict error.
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Zitadel.OidcAppExistsButSecretLost");
        result.Error.Message.Should().Contain($"nexus-{OrgName}-app");
        result.Error.Message.Should().Contain("client_secret");

        // Provisioner does NOT compensate. Cleanup is the saga's job.
        await orgService.DidNotReceiveWithAnyArgs()
            .DeactivateOrganizationAsync(default!, default);
        await orgService.DidNotReceiveWithAnyArgs()
            .RemoveMemberAsync(default!, default!, default);

        // The user step should not have run because we failed earlier in step 3.
        await userService.DidNotReceiveWithAnyArgs()
            .CreateUserAsync(default!, default);
    }

    private static ZitadelHttpClient CreateFakeHttpClient()
    {
        // ZitadelHttpClient is non-sealed with virtual methods; NSubstitute partial-mocks
        // it so we can stub only the methods the provisioner calls. The base ctor still
        // wants a real HttpClient and options, so we pass minimal stubs.
        var httpClient = new HttpClient { BaseAddress = new Uri("https://zitadel.invalid/") };
        var options = Options.Create(new ZitadelOptions { Authority = "https://zitadel.invalid" });
        return Substitute.For<ZitadelHttpClient>(
            httpClient,
            options,
            NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelOrganizationIdentityProvisioner CreateProvisioner(
        ZitadelHttpClient httpClient,
        IOrganizationService orgService,
        IIdentityUserService userService)
    {
        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.invalid",
            // Provisioner reads these to build the redirect URIs.
            RedirectUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test/callback",
            PostLogoutUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test/signed-out"
        });
        return new ZitadelOrganizationIdentityProvisioner(
            httpClient,
            orgService,
            userService,
            options,
            NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
    }
}
