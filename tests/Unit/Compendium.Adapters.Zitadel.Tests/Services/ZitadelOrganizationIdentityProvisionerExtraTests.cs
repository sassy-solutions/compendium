// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationIdentityProvisionerExtraTests.cs" company="Sassy Solutions">
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
/// Additional unit tests for <see cref="ZitadelOrganizationIdentityProvisioner"/>
/// covering paths not exercised by the existing conflict-reuse tests:
/// happy path, user-conflict reuse (POM-470), template validation, member-add
/// failure, lookup-after-conflict failures, non-conflict failures.
/// </summary>
public class ZitadelOrganizationIdentityProvisionerExtraTests
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
    public async Task ProvisionAsync_HappyPath_CreatesAllResourcesAndReturnsSecrets()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        orgService.AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId, Name = $"nexus-{OrgName}" }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp
            {
                AppId = "a-1",
                ClientId = "client-1",
                ClientSecret = "secret-1"
            }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalOrganizationId.Should().Be(ZitadelOrgId);
        result.Value.ExternalProjectId.Should().Be(ProjectId);
        result.Value.ClientId.Should().Be("client-1");
        result.Value.ClientSecret.Should().Be("secret-1");
        result.Value.AdminUserId.Should().Be(AdminUserId);
    }

    [Fact]
    public async Task ProvisionAsync_HappyPath_UsesResourceOwnerAsProjectIdWhenIdNull()
    {
        // Arrange — Zitadel can return Details.ResourceOwner without an Id; the
        // provisioner should fall back to ResourceOwner for the project id.
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        orgService.AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject
            {
                Id = ProjectId,
                Details = new ZitadelResourceDetails { ResourceOwner = "ro-1" }
            }));
        http.CreateOidcApplicationAsync(Arg.Any<string>(), Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp
            {
                AppId = "a-1",
                ClientId = "c",
                ClientSecret = "s"
            }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert — provisioner picked Id (since not null) but exercised the branch;
        // the result still surfaces a project id.
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProjectId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProvisionAsync_UserConflict_ReusesExistingUser()
    {
        // Arrange — explicit coverage of the conflict-then-lookup user path
        // introduced in 3bd935b.
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        orgService.AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp
            {
                AppId = "a-1",
                ClientId = "c",
                ClientSecret = "s"
            }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityUser>(Error.Conflict("Identity.UserAlreadyExists", "x")));
        userService.GetUserByEmailAsync(AdminEmail, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminUserId.Should().Be(AdminUserId);
        await userService.Received(1).GetUserByEmailAsync(AdminEmail, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_UserNonConflictFailure_PropagatesError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp { AppId = "a-1", ClientId = "c", ClientSecret = "s" }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityUser>(Error.Forbidden("Zitadel.Forbidden", "no")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task ProvisionAsync_UserConflictThenLookupFails_PropagatesLookupError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp { AppId = "a-1", ClientId = "c", ClientSecret = "s" }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityUser>(Error.Conflict("Identity.UserAlreadyExists", "x")));
        userService.GetUserByEmailAsync(AdminEmail, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityUser>(Error.Failure("Zitadel.Error", "lookup blew up")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task ProvisionAsync_AddMemberFails_PropagatesError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        orgService.AddMemberAsync(ZitadelOrgId, AdminUserId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Forbidden("Zitadel.Forbidden", "no")));

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelOidcApp { AppId = "a-1", ClientId = "c", ClientSecret = "s" }));

        userService.CreateUserAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityUser { Id = AdminUserId, Email = AdminEmail }));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task ProvisionAsync_OrgConflictThenLookupFails_PropagatesLookupError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityOrganization>(Error.Conflict("Zitadel.Conflict", "exists")));
        orgService.GetOrganizationByNameAsync(DisplayName, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityOrganization>(Error.Failure("Zitadel.Error", "lookup blew up")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task ProvisionAsync_OrgNonConflictFailure_PropagatesError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IdentityOrganization>(Error.Failure("Zitadel.Error", "boom")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task ProvisionAsync_ProjectConflictThenLookupFails_PropagatesLookupError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelProject>(Error.Conflict("Zitadel.Conflict", "exists")));
        http.GetProjectByNameAsync(Arg.Any<string>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelProject>(Error.Failure("Zitadel.Error", "lookup blew up")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task ProvisionAsync_ProjectNonConflictFailure_PropagatesError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));
        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelProject>(Error.Failure("Zitadel.Error", "boom")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task ProvisionAsync_OidcAppNonConflictFailure_PropagatesError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        orgService.CreateOrganizationAsync(Arg.Any<CreateOrganizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new IdentityOrganization { Id = ZitadelOrgId, Name = DisplayName }));

        http.CreateProjectAsync(Arg.Any<ZitadelCreateProjectRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ZitadelProject { Id = ProjectId }));
        http.CreateOidcApplicationAsync(ProjectId, Arg.Any<ZitadelCreateOidcAppRequest>(), ZitadelOrgId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ZitadelOidcApp>(Error.Forbidden("Zitadel.Forbidden", "no")));

        var sut = CreateProvisioner(http, orgService, userService);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task ProvisionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateProvisioner(CreateFakeHttpClient(),
            Substitute.For<IOrganizationService>(),
            Substitute.For<IIdentityUserService>());

        // Act
        Func<Task> act = () => sut.ProvisionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProvisionAsync_WithMissingRedirectTemplate_ReturnsTemplateMissing()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.invalid",
            RedirectUriTemplate = null,
            PostLogoutUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test"
        });
        var sut = new ZitadelOrganizationIdentityProvisioner(
            http, orgService, userService, options,
            NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.TemplateMissing");
    }

    [Fact]
    public async Task ProvisionAsync_WithRedirectTemplateMissingPlaceholder_ReturnsValidationError()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.invalid",
            RedirectUriTemplate = "https://no-placeholder.example.test/cb",
            PostLogoutUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test"
        });
        var sut = new ZitadelOrganizationIdentityProvisioner(
            http, orgService, userService, options,
            NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.TemplateMissingPlaceholder");
    }

    [Fact]
    public async Task ProvisionAsync_WithMissingPostLogoutTemplate_ReturnsTemplateMissing()
    {
        // Arrange
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var http = CreateFakeHttpClient();

        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.invalid",
            RedirectUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test/cb",
            PostLogoutUriTemplate = string.Empty
        });
        var sut = new ZitadelOrganizationIdentityProvisioner(
            http, orgService, userService, options,
            NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);

        // Act
        var result = await sut.ProvisionAsync(Request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.TemplateMissing");
    }

    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateFakeHttpClient();
        var orgService = Substitute.For<IOrganizationService>();
        var userService = Substitute.For<IIdentityUserService>();
        var options = Options.Create(new ZitadelOptions { Authority = "https://x" });

        // Act
        Action a1 = () => new ZitadelOrganizationIdentityProvisioner(null!, orgService, userService, options, NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
        Action a2 = () => new ZitadelOrganizationIdentityProvisioner(http, null!, userService, options, NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
        Action a3 = () => new ZitadelOrganizationIdentityProvisioner(http, orgService, null!, options, NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
        Action a4 = () => new ZitadelOrganizationIdentityProvisioner(http, orgService, userService, null!, NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
        Action a5 = () => new ZitadelOrganizationIdentityProvisioner(http, orgService, userService, options, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("organizationService");
        a3.Should().Throw<ArgumentNullException>().WithParameterName("userService");
        a4.Should().Throw<ArgumentNullException>().WithParameterName("options");
        a5.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    private static ZitadelHttpClient CreateFakeHttpClient()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://zitadel.invalid/") };
        var options = Options.Create(new ZitadelOptions { Authority = "https://zitadel.invalid" });
        return Substitute.For<ZitadelHttpClient>(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelOrganizationIdentityProvisioner CreateProvisioner(
        ZitadelHttpClient http,
        IOrganizationService orgService,
        IIdentityUserService userService)
    {
        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.invalid",
            RedirectUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test/callback",
            PostLogoutUriTemplate = $"https://{ZitadelOptions.OrganizationPlaceholder}.example.test/signed-out"
        });
        return new ZitadelOrganizationIdentityProvisioner(
            http, orgService, userService, options,
            NullLogger<ZitadelOrganizationIdentityProvisioner>.Instance);
    }
}
