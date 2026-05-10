// -----------------------------------------------------------------------
// <copyright file="ZitadelOrganizationServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Abstractions.Identity.Models.Requests;
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
/// Unit tests for <see cref="ZitadelOrganizationService"/> driving a real
/// <see cref="ZitadelHttpClient"/> backed by <c>MockHttpMessageHandler</c>.
/// Most <see cref="ZitadelHttpClient"/> methods are non-virtual, so partial
/// substitution is not possible — going through the HTTP layer covers the
/// service AND the HTTP plumbing in one shot.
/// </summary>
public class ZitadelOrganizationServiceTests
{
    private const string Authority = "https://zitadel.invalid";
    private const string OrgId = "11111111";
    private const string UserId = "22222222";

    [Fact]
    public void Ctor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        var act = () => new ZitadelOrganizationService(null!, options, NullLogger<ZitadelOrganizationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Ctor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateHttp(new MockHttpMessageHandler());

        // Act
        var act = () => new ZitadelOrganizationService(http, null!, NullLogger<ZitadelOrganizationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Ctor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateHttp(new MockHttpMessageHandler());
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        var act = () => new ZitadelOrganizationService(http, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateOrganizationAsync_OnSuccess_MapsToIdentityOrganization()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs")
            .Respond("application/json",
                "{\"id\":\"" + OrgId + "\",\"name\":\"Acme\",\"state\":\"ORG_STATE_ACTIVE\",\"primaryDomain\":\"acme.test\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOrganizationAsync(new CreateOrganizationRequest { Name = "Acme" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(OrgId);
        result.Value.Name.Should().Be("Acme");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Domain.Should().Be("acme.test");
    }

    [Fact]
    public async Task CreateOrganizationAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"exists\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOrganizationAsync(new CreateOrganizationRequest { Name = "Acme" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.CreateOrganizationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetOrganizationAsync_OnSuccess_MapsToIdentityOrganization()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/management/v1/orgs/me")
            .Respond("application/json",
                "{\"id\":\"" + OrgId + "\",\"name\":\"Acme\",\"state\":\"ORG_STATE_ACTIVE\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(OrgId);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizationAsync_WhenNotFoundError_ReturnsOrganizationNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/management/v1/orgs/me")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"missing\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationAsync(OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.OrganizationNotFound");
    }

    [Fact]
    public async Task GetOrganizationAsync_WhenOtherError_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/management/v1/orgs/me")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"no\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationAsync(OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Forbidden");
    }

    [Fact]
    public async Task GetOrganizationAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.GetOrganizationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetOrganizationByNameAsync_WhenMatches_ReturnsOrganization()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/organizations/_search")
            .Respond("application/json",
                "{\"result\":[{\"id\":\"" + OrgId + "\",\"name\":\"Acme\",\"state\":\"ORG_STATE_ACTIVE\"}]}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationByNameAsync("Acme");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(OrgId);
    }

    [Fact]
    public async Task GetOrganizationByNameAsync_WhenEmpty_ReturnsNotFoundByName()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/organizations/_search")
            .Respond("application/json", "{\"result\":[]}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationByNameAsync("Acme");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.OrganizationNotFoundByName");
    }

    [Fact]
    public async Task GetOrganizationByNameAsync_WhenSearchFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/organizations/_search")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationByNameAsync("Acme");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
    }

    [Fact]
    public async Task GetOrganizationByNameAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.GetOrganizationByNameAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddMemberAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.AddMemberAsync(OrgId, UserId, new[] { "ORG_OWNER" });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddMemberAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"dup\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.AddMemberAsync(OrgId, UserId, new[] { "ORG_OWNER" });

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AddMemberAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> a1 = () => sut.AddMemberAsync(null!, UserId, new[] { "X" });
        Func<Task> a2 = () => sut.AddMemberAsync(OrgId, null!, new[] { "X" });
        Func<Task> a3 = () => sut.AddMemberAsync(OrgId, UserId, null!);

        // Assert
        await a1.Should().ThrowAsync<ArgumentNullException>();
        await a2.Should().ThrowAsync<ArgumentNullException>();
        await a3.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RemoveMemberAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/management/v1/orgs/me/members/{UserId}")
            .Respond(HttpStatusCode.OK);
        var sut = CreateSut(mock);

        // Act
        var result = await sut.RemoveMemberAsync(OrgId, UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMemberAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/management/v1/orgs/me/members/{UserId}")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.RemoveMemberAsync(OrgId, UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMemberRolesAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/orgs/me/members/{UserId}")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateMemberRolesAsync(OrgId, UserId, new[] { "ORG_OWNER" });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMemberRolesAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/orgs/me/members/{UserId}")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateMemberRolesAsync(OrgId, UserId, new[] { "X" });

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListMembersAsync_OnSuccess_MapsMembers()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members/_search")
            .Respond("application/json",
                "{\"result\":[{\"userId\":\"" + UserId + "\",\"roles\":[\"ORG_OWNER\"]}]}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.ListMembersAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].UserId.Should().Be(UserId);
        result.Value[0].Roles.Should().Contain("ORG_OWNER");
    }

    [Fact]
    public async Task ListMembersAsync_WhenResultIsNull_ReturnsEmptyList()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members/_search")
            .Respond("application/json", "{}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.ListMembersAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMembersAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members/_search")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.ListMembersAsync(OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateOrganizationAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/_deactivate")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeactivateOrganizationAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateOrganizationAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/_deactivate")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeactivateOrganizationAsync(OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    private static ZitadelHttpClient CreateHttp(MockHttpMessageHandler mock)
    {
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions
        {
            Authority = Authority,
            PersonalAccessToken = "test-pat"
        });
        return new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelOrganizationService CreateSut(MockHttpMessageHandler mock)
    {
        var http = CreateHttp(mock);
        var options = Options.Create(new ZitadelOptions
        {
            Authority = Authority,
            PersonalAccessToken = "test-pat"
        });
        return new ZitadelOrganizationService(http, options, NullLogger<ZitadelOrganizationService>.Instance);
    }
}
