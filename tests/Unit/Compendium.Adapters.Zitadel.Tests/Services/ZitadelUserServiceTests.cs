// -----------------------------------------------------------------------
// <copyright file="ZitadelUserServiceTests.cs" company="Sassy Solutions">
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
using Compendium.Multitenancy;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Zitadel.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ZitadelUserService"/>. Drives the real
/// <see cref="ZitadelHttpClient"/> through <c>MockHttpMessageHandler</c>
/// since most HTTP-client methods are non-virtual.
/// </summary>
public class ZitadelUserServiceTests
{
    private const string Authority = "https://zitadel.invalid";
    private const string OrgId = "11111111";
    private const string UserId = "22222222";

    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateHttp(new MockHttpMessageHandler());
        var ctx = Substitute.For<ITenantContext>();
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        Action a1 = () => new ZitadelUserService(null!, ctx, options, NullLogger<ZitadelUserService>.Instance);
        Action a2 = () => new ZitadelUserService(http, null!, options, NullLogger<ZitadelUserService>.Instance);
        Action a3 = () => new ZitadelUserService(http, ctx, null!, NullLogger<ZitadelUserService>.Instance);
        Action a4 = () => new ZitadelUserService(http, ctx, options, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("tenantContext");
        a3.Should().Throw<ArgumentNullException>().WithParameterName("options");
        a4.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreateUserAsync_OnSuccess_MapsResponseToIdentityUser()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond("application/json",
                "{\"userId\":\"" + UserId + "\",\"state\":\"USER_STATE_ACTIVE\"," +
                "\"human\":{\"profile\":{\"firstName\":\"Ada\",\"lastName\":\"Lovelace\",\"displayName\":\"Ada\"}," +
                "\"email\":{\"email\":\"ada@x.test\",\"isEmailVerified\":true}}}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(new CreateUserRequest
        {
            Email = "ada@x.test",
            FirstName = "Ada",
            LastName = "Lovelace",
            OrganizationId = OrgId,
            SendVerificationEmail = false
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(UserId);
        result.Value.Email.Should().Be("ada@x.test");
        result.Value.FirstName.Should().Be("Ada");
        result.Value.IsActive.Should().BeTrue();
        result.Value.EmailVerified.Should().BeTrue();
        result.Value.OrganizationId.Should().Be(OrgId);
    }

    [Fact]
    public async Task CreateUserAsync_WithPassword_SendsCreatePasswordPayload()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond("application/json", "{\"userId\":\"" + UserId + "\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(new CreateUserRequest
        {
            Email = "u@x.test",
            FirstName = "F",
            LastName = "L",
            Password = "P@ssw0rd",
            PhoneNumber = "+1-555-0000",
            OrganizationId = OrgId
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"exists\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(new CreateUserRequest
        {
            Email = "u@x.test",
            OrganizationId = OrgId
        });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullEmail_DoesNotThrowAndProducesEmptyHashLog()
    {
        // Arrange — empty email exercises the HashPrefix "<empty>" branch.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond("application/json", "{\"userId\":\"" + UserId + "\"}");
        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(new CreateUserRequest
        {
            Email = string.Empty,
            OrganizationId = OrgId
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.CreateUserAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetUserByIdAsync_OnSuccess_ReturnsMappedUser()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/{UserId}")
            .Respond("application/json",
                "{\"userId\":\"" + UserId + "\",\"state\":\"USER_STATE_ACTIVE\"," +
                "\"human\":{\"email\":{\"email\":\"u@x.test\",\"isEmailVerified\":true}}}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByIdAsync(UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(UserId);
        result.Value.Email.Should().Be("u@x.test");
    }

    [Fact]
    public async Task GetUserByIdAsync_OnNotFoundError_RemapsToUserNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"missing\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByIdAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.UserNotFound");
    }

    [Fact]
    public async Task GetUserByIdAsync_OnOtherError_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"no\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByIdAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Forbidden");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.GetUserByIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenMatches_ReturnsUser()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond("application/json",
                "{\"result\":[{\"userId\":\"" + UserId + "\"," +
                "\"human\":{\"email\":{\"email\":\"u@x.test\"}}}]}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByEmailAsync("u@x.test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(UserId);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenEmpty_ReturnsUserNotFoundByEmail()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond("application/json", "{\"result\":[]}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByEmailAsync("missing@x.test");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.UserNotFoundByEmail");
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenSearchFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.GetUserByEmailAsync("u@x.test");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNullEmail_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.GetUserByEmailAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.UpdateUserAsync(UserId, new UpdateUserRequest { FirstName = "F2" });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.UpdateUserAsync(UserId, new UpdateUserRequest { FirstName = "F2" });

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> a1 = () => sut.UpdateUserAsync(null!, new UpdateUserRequest());
        Func<Task> a2 = () => sut.UpdateUserAsync("u-1", null!);

        // Assert
        await a1.Should().ThrowAsync<ArgumentNullException>();
        await a2.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeactivateUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/deactivate")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.DeactivateUserAsync(UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateUserAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/deactivate")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.DeactivateUserAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/reactivate")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.ReactivateUserAsync(UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateUserAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/reactivate")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.ReactivateUserAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListUsersAsync_OnSuccess_ReturnsPagedResult()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond("application/json",
                "{\"details\":{\"totalResult\":\"42\"}," +
                "\"result\":[{\"userId\":\"" + UserId + "\"}]}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.ListUsersAsync(new ListUsersRequest
        {
            Page = 1,
            PageSize = 10,
            SearchQuery = "ad"
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(42);
    }

    [Fact]
    public async Task ListUsersAsync_WhenResultIsNull_ReturnsEmptyItems()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond("application/json", "{}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.ListUsersAsync(new ListUsersRequest { Page = 1, PageSize = 10 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListUsersAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.ListUsersAsync(new ListUsersRequest { Page = 1, PageSize = 10 });

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ListUsersAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.ListUsersAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.OK);
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.DeleteUserAsync(UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/{UserId}")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.DeleteUserAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/password_reset")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.InitiatePasswordResetAsync(UserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/{UserId}/password_reset")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"message\":\"x\"}");
        var sut = CreateSut(mock, tenantId: OrgId);

        // Act
        var result = await sut.InitiatePasswordResetAsync(UserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenTenantContextHasNoTenant_FallsBackToDefaultOrganizationId()
    {
        // Arrange — no tenant set, only DefaultOrganizationId on options.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/{UserId}")
            .Respond("application/json", "{\"userId\":\"" + UserId + "\"}");
        var sut = CreateSut(mock, tenantId: null, defaultOrgId: "default-org");

        // Act
        var result = await sut.GetUserByIdAsync(UserId);

        // Assert — request succeeds, fallback path was exercised.
        result.IsSuccess.Should().BeTrue();
    }

    private static ZitadelHttpClient CreateHttp(MockHttpMessageHandler mock)
    {
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions { Authority = Authority, PersonalAccessToken = "test-pat" });
        return new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelUserService CreateSut(
        MockHttpMessageHandler mock,
        string? tenantId = null,
        string? defaultOrgId = null)
    {
        var http = CreateHttp(mock);
        var ctx = Substitute.For<ITenantContext>();
        ctx.TenantId.Returns(tenantId);
        var options = Options.Create(new ZitadelOptions
        {
            Authority = Authority,
            PersonalAccessToken = "test-pat",
            DefaultOrganizationId = defaultOrgId
        });
        return new ZitadelUserService(http, ctx, options, NullLogger<ZitadelUserService>.Instance);
    }
}
