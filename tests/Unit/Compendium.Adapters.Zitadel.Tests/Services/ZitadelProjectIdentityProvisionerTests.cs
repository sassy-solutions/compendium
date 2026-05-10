// -----------------------------------------------------------------------
// <copyright file="ZitadelProjectIdentityProvisionerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Abstractions.Identity;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Services;
using Compendium.Core.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Zitadel.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ZitadelProjectIdentityProvisioner"/>.
/// </summary>
public class ZitadelProjectIdentityProvisionerTests
{
    private const string Authority = "https://zitadel.invalid";
    private const string OrgId = "11111111";
    private const string ProjectId = "22222222";
    private const string AppId = "33333333";

    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateHttp(new MockHttpMessageHandler());

        // Act
        Action a1 = () => new ZitadelProjectIdentityProvisioner(null!, NullLogger<ZitadelProjectIdentityProvisioner>.Instance);
        Action a2 = () => new ZitadelProjectIdentityProvisioner(http, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ProvisionProjectAsync_OnSuccess_ReturnsResult()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects")
            .Respond("application/json", $"{{\"id\":\"{ProjectId}\"}}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.ProvisionProjectAsync(new ProjectProvisioningRequest(
            ProjectId: "p-aggregate",
            OrganizationId: "o-aggregate",
            ExternalOrganizationId: OrgId,
            ProjectName: "nexus-acme"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProjectId.Should().Be(ProjectId);
    }

    [Fact]
    public async Task ProvisionProjectAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.ProvisionProjectAsync(new ProjectProvisioningRequest(
            ProjectId: "p", OrganizationId: "o", ExternalOrganizationId: OrgId, ProjectName: "n"));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionProjectAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.ProvisionProjectAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOidcAppAsync_OnSuccess_ReturnsClientCredentials()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/oidc")
            .Respond("application/json",
                $"{{\"appId\":\"{AppId}\",\"clientId\":\"cid\",\"clientSecret\":\"sec\"}}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOidcAppAsync(new OidcAppProvisioningRequest(
            ExternalProjectId: ProjectId,
            ExternalOrganizationId: OrgId,
            AppName: "app",
            RedirectUris: new[] { "https://x" },
            PostLogoutRedirectUris: new[] { "https://y" }));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClientId.Should().Be("cid");
        result.Value.ClientSecret.Should().Be("sec");
        result.Value.ExternalAppId.Should().Be(AppId);
    }

    [Fact]
    public async Task CreateOidcAppAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/oidc")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOidcAppAsync(new OidcAppProvisioningRequest(
            ExternalProjectId: ProjectId,
            ExternalOrganizationId: OrgId,
            AppName: "app",
            RedirectUris: new[] { "https://x" },
            PostLogoutRedirectUris: new[] { "https://y" }));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOidcAppAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.CreateOidcAppAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateOidcAppAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOidcAppAsync(new OidcAppUpdateRequest(
            ExternalProjectId: ProjectId,
            ExternalAppId: AppId,
            ExternalOrganizationId: OrgId,
            RedirectUris: new[] { "https://x" },
            PostLogoutRedirectUris: new[] { "https://y" }));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOidcAppAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOidcAppAsync(new OidcAppUpdateRequest(
            ExternalProjectId: ProjectId,
            ExternalAppId: AppId,
            ExternalOrganizationId: OrgId,
            RedirectUris: new[] { "https://x" },
            PostLogoutRedirectUris: new[] { "https://y" }));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOidcAppAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.UpdateOidcAppAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteOidcAppAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}")
            .Respond(HttpStatusCode.OK);
        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteOidcAppAsync(new OidcAppDeleteRequest(
            ExternalProjectId: ProjectId,
            ExternalAppId: AppId,
            ExternalOrganizationId: OrgId));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteOidcAppAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.DeleteOidcAppAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RotateOidcAppSecretAsync_OnSuccess_ReturnsNewSecret()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post,
                $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc_config/_generate_client_secret")
            .Respond("application/json", "{\"clientSecret\":\"rotated-sec\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.RotateOidcAppSecretAsync(new OidcAppSecretRotationRequest(
            ExternalProjectId: ProjectId,
            ExternalAppId: AppId,
            ExternalOrganizationId: OrgId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClientSecret.Should().Be("rotated-sec");
    }

    [Fact]
    public async Task RotateOidcAppSecretAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post,
                $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc_config/_generate_client_secret")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.RotateOidcAppSecretAsync(new OidcAppSecretRotationRequest(
            ExternalProjectId: ProjectId,
            ExternalAppId: AppId,
            ExternalOrganizationId: OrgId));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RotateOidcAppSecretAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.RotateOidcAppSecretAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static ZitadelHttpClient CreateHttp(MockHttpMessageHandler mock)
    {
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions { Authority = Authority, PersonalAccessToken = "test-pat" });
        return new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelProjectIdentityProvisioner CreateSut(MockHttpMessageHandler mock)
    {
        var http = CreateHttp(mock);
        return new ZitadelProjectIdentityProvisioner(http, NullLogger<ZitadelProjectIdentityProvisioner>.Instance);
    }
}
