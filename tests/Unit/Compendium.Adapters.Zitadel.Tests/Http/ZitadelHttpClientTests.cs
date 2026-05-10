// -----------------------------------------------------------------------
// <copyright file="ZitadelHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;
using Compendium.Core.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Compendium.Adapters.Zitadel.Tests.Http;

/// <summary>
/// Unit tests for <see cref="ZitadelHttpClient"/> covering the HTTP-level
/// path: request shaping, response parsing, error mapping, token acquisition
/// (PAT vs client_credentials), and the conflict-recovery search endpoints
/// added for the org/project/OIDC idempotency story.
/// </summary>
public class ZitadelHttpClientTests
{
    private const string Authority = "https://zitadel.invalid";
    private const string OrgId = "11111111";
    private const string ProjectId = "22222222";
    private const string AppId = "33333333";

    [Fact]
    public void Ctor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        var act = () => new ZitadelHttpClient(null!, options, NullLogger<ZitadelHttpClient>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Ctor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var http = new HttpClient();

        // Act
        var act = () => new ZitadelHttpClient(http, null!, NullLogger<ZitadelHttpClient>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Ctor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var http = new HttpClient();
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        var act = () => new ZitadelHttpClient(http, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Ctor_WithInternalBaseUrl_SetsHostHeaderToAuthority()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions
        {
            Authority = "https://zitadel.example.com",
            InternalBaseUrl = "http://zitadel.svc.cluster.local"
        });

        // Act
        using var sut = new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);

        // Assert
        http.BaseAddress!.AbsoluteUri.Should().StartWith("http://zitadel.svc.cluster.local");
        http.DefaultRequestHeaders.Host.Should().Be("zitadel.example.com");
    }

    [Fact]
    public async Task CreateUserAsync_OnSuccess_DeserializesResponseAndAddsBearerTokenAndOrgIdHeader()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .WithHeaders("Authorization", "Bearer my-pat")
            .WithHeaders("x-zitadel-orgid", OrgId)
            .Respond("application/json", "{\"userId\":\"u-1\",\"state\":\"USER_STATE_ACTIVE\"}");

        var sut = CreateSut(mock, pat: "my-pat");

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "ada@example.test",
                Profile = new ZitadelCreateProfile { FirstName = "Ada", LastName = "Lovelace" },
                Email = new ZitadelCreateEmail { Email = "ada@example.test" }
            },
            OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("u-1");
        result.Value.State.Should().Be("USER_STATE_ACTIVE");
        mock.GetMatchCount(mock.Expect(HttpMethod.Post, "*")).Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task CreateUserAsync_OnConflict_ReturnsConflictError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"code\":6,\"message\":\"already exists\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "ada@example.test",
                Profile = new ZitadelCreateProfile { FirstName = "A", LastName = "B" },
                Email = new ZitadelCreateEmail { Email = "ada@example.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Zitadel.Conflict");
        result.Error.Message.Should().Contain("already exists");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Zitadel.NotFound", ErrorType.NotFound)]
    [InlineData(HttpStatusCode.BadRequest, "Zitadel.BadRequest", ErrorType.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, "Zitadel.Unauthorized", ErrorType.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, "Zitadel.Forbidden", ErrorType.Forbidden)]
    public async Task CreateUserAsync_OnHttpError_MapsToExpectedError(
        HttpStatusCode status, string expectedCode, ErrorType expectedType)
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond(status, "application/json", "{\"code\":1,\"message\":\"oops\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
        result.Error.Type.Should().Be(expectedType);
    }

    [Fact]
    public async Task CreateUserAsync_OnTooManyRequests_ReturnsRateLimitError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{\"code\":8,\"message\":\"slow down\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.RateLimitExceeded");
    }

    [Fact]
    public async Task CreateUserAsync_OnServerErrorWithUnparseableBody_ReturnsGenericFailure()
    {
        // Arrange — body is not valid JSON to force the catch path of ParseErrorAsync.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond(HttpStatusCode.InternalServerError, "text/plain", "<html>boom</html>");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.Error");
        result.Error.Message.Should().Contain("500");
    }

    [Fact]
    public async Task CreateUserAsync_OnHttpRequestException_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Throw(new HttpRequestException("network down"));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task CreateUserAsync_OnTimeout_ReturnsProviderUnavailable()
    {
        // Arrange — TaskCanceledException with TimeoutException inner triggers the timeout branch.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Throw(new TaskCanceledException("timeout", new TimeoutException("inner")));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task CreateUserAsync_OnEmpty200Body_ReturnsEmptyResponseError()
    {
        // Arrange — null content body causes ReadFromJsonAsync to return null.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/human")
            .Respond("application/json", "null");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateUserAsync(
            new ZitadelCreateUserRequest
            {
                UserName = "u",
                Profile = new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
                Email = new ZitadelCreateEmail { Email = "u@e.test" }
            },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.EmptyResponse");
    }

    [Fact]
    public async Task GetUserByIdAsync_BuildsCorrectUrlAndReturnsUser()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/abc")
            .Respond("application/json", "{\"userId\":\"abc\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetUserByIdAsync("abc", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("abc");
    }

    [Fact]
    public async Task GetUserByIdAsync_OnHttpError_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/abc")
            .Throw(new HttpRequestException("network down"));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetUserByIdAsync("abc", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task GetUserByIdAsync_OnTimeout_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/abc")
            .Throw(new TaskCanceledException("t", new TimeoutException("inner")));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetUserByIdAsync("abc", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
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
    public async Task UpdateUserProfileAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/u-1")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateUserProfileAsync("u-1",
            new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
            OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/u-1")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"nope\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateUserProfileAsync("u-1",
            new ZitadelCreateProfile { FirstName = "F", LastName = "L" },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_OnHttpException_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/u-1")
            .Throw(new HttpRequestException("boom"));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateUserProfileAsync("u-1",
            new ZitadelCreateProfile { FirstName = "F", LastName = "L" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_OnTimeout_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/v2/users/u-1")
            .Throw(new TaskCanceledException("t", new TimeoutException("i")));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateUserProfileAsync("u-1",
            new ZitadelCreateProfile { FirstName = "F", LastName = "L" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task DeactivateUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/u-1/deactivate")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeactivateUserAsync("u-1", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/u-1/reactivate")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.ReactivateUserAsync("u-1", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/u-1")
            .Respond(HttpStatusCode.OK);

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteUserAsync("u-1", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_OnError_ParsesErrorBody()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/u-1")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"gone\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteUserAsync("u-1", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.NotFound");
    }

    [Fact]
    public async Task DeleteUserAsync_OnHttpException_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/u-1")
            .Throw(new HttpRequestException("boom"));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteUserAsync("u-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task DeleteUserAsync_OnTimeout_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/v2/users/u-1")
            .Throw(new TaskCanceledException("t", new TimeoutException("i")));

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteUserAsync("u-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users/u-1/password_reset")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.InitiatePasswordResetAsync("u-1", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IntrospectTokenAsync_OnSuccess_ReturnsParsedResponse()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
            .Respond("application/json", "{\"active\":true,\"sub\":\"user-1\"}");

        var sut = CreateSut(mock, clientId: "id", clientSecret: "sec");

        // Act
        var result = await sut.IntrospectTokenAsync("the-token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeTrue();
        result.Value.Sub.Should().Be("user-1");
    }

    [Fact]
    public async Task IntrospectTokenAsync_OnNonSuccessStatus_ReturnsInvalidToken()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
            .Respond(HttpStatusCode.BadRequest, "application/json", "bad");

        var sut = CreateSut(mock, clientId: "id", clientSecret: "sec");

        // Act
        var result = await sut.IntrospectTokenAsync("bad-token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.InvalidToken");
    }

    [Fact]
    public async Task IntrospectTokenAsync_OnException_ReturnsProviderUnavailable()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
            .Throw(new HttpRequestException("net"));

        var sut = CreateSut(mock, clientId: "id", clientSecret: "sec");

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.ProviderUnavailable");
    }

    [Fact]
    public async Task IntrospectTokenAsync_OnEmptyJson_ReturnsInactiveResponse()
    {
        // Arrange — server returns "null" causing ReadFromJsonAsync to yield null.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
            .Respond("application/json", "null");

        var sut = CreateSut(mock, clientId: "id", clientSecret: "sec");

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeFalse();
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        Func<Task> act = () => sut.IntrospectTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrganizationAsync_OnSuccess_ReturnsOrganization()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs")
            .Respond("application/json", "{\"id\":\"o-1\",\"name\":\"Acme\",\"state\":\"ORG_STATE_ACTIVE\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOrganizationAsync(new ZitadelCreateOrganizationRequest { Name = "Acme" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("o-1");
    }

    [Fact]
    public async Task GetOrganizationAsync_OnSuccess_ReturnsOrganization()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, $"{Authority}/management/v1/orgs/me")
            .Respond("application/json", "{\"id\":\"o-1\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOrganizationAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("o-1");
    }

    [Fact]
    public async Task AddOrganizationMemberAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.AddOrganizationMemberAsync(OrgId,
            new ZitadelAddMemberRequest { UserId = "u-1", Roles = ["ORG_OWNER"] });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddOrganizationMemberAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"dup\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.AddOrganizationMemberAsync(OrgId,
            new ZitadelAddMemberRequest { UserId = "u-1", Roles = ["ORG_OWNER"] });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task RemoveOrganizationMemberAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/management/v1/orgs/me/members/u-1")
            .Respond(HttpStatusCode.OK);

        var sut = CreateSut(mock);

        // Act
        var result = await sut.RemoveOrganizationMemberAsync(OrgId, "u-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrganizationMemberRolesAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/orgs/me/members/u-1")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOrganizationMemberRolesAsync(OrgId, "u-1", new List<string> { "ORG_OWNER" });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrganizationMemberRolesAsync_OnFailure_ReturnsFailure()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/orgs/me/members/u-1")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"message\":\"bad\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOrganizationMemberRolesAsync(OrgId, "u-1", new List<string> { "x" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.BadRequest");
    }

    [Fact]
    public async Task ListOrganizationMembersAsync_OnSuccess_ReturnsMembers()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/orgs/me/members/_search")
            .Respond("application/json", "{\"result\":[{\"userId\":\"u-1\",\"roles\":[\"ORG_OWNER\"]}]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.ListOrganizationMembersAsync(OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Result.Should().NotBeNull();
        result.Value.Result!.Should().HaveCount(1);
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
    public async Task CreateProjectAsync_OnSuccess_ReturnsProject()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects")
            .Respond("application/json", "{\"id\":\"p-1\",\"name\":\"nexus-acme\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateProjectAsync(
            new ZitadelCreateProjectRequest { Name = "nexus-acme", ProjectRoleAssertion = true, ProjectRoleCheck = true, HasProjectCheck = true },
            OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("p-1");
    }

    [Fact]
    public async Task CreateProjectAsync_OnConflict_ReturnsConflict()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects")
            .Respond(HttpStatusCode.Conflict, "application/json", "{\"message\":\"dup\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateProjectAsync(
            new ZitadelCreateProjectRequest { Name = "nexus-acme", ProjectRoleAssertion = true, ProjectRoleCheck = true, HasProjectCheck = true },
            OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task GetProjectByNameAsync_WhenMatchExists_ReturnsProject()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/_search")
            .Respond("application/json", "{\"result\":[{\"id\":\"p-1\",\"name\":\"nexus-acme\"}]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetProjectByNameAsync("nexus-acme", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("p-1");
    }

    [Fact]
    public async Task GetProjectByNameAsync_WhenNoMatch_ReturnsNotFound()
    {
        // Arrange — empty result list maps to ProjectNotFound.
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/_search")
            .Respond("application/json", "{\"result\":[]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetProjectByNameAsync("nope", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.ProjectNotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetProjectByNameAsync_WhenSearchFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/_search")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"message\":\"no\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetProjectByNameAsync("nexus-acme", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CreateOidcApplicationAsync_OnSuccess_ReturnsAppWithSecret()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/oidc")
            .Respond("application/json", "{\"appId\":\"a-1\",\"clientId\":\"cid\",\"clientSecret\":\"sec\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.CreateOidcApplicationAsync(
            ProjectId,
            new ZitadelCreateOidcAppRequest
            {
                Name = "app",
                RedirectUris = ["https://x"],
                PostLogoutRedirectUris = ["https://y"],
                ResponseTypes = ["OIDC_RESPONSE_TYPE_CODE"],
                GrantTypes = ["OIDC_GRANT_TYPE_AUTHORIZATION_CODE"],
                AppType = "OIDC_APP_TYPE_WEB",
                AuthMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC"
            },
            OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AppId.Should().Be("a-1");
        result.Value.ClientSecret.Should().Be("sec");
    }

    [Fact]
    public async Task GetOidcApplicationByNameAsync_WhenMatchExists_ReturnsApp()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/_search")
            .Respond("application/json", "{\"result\":[{\"id\":\"a-1\",\"name\":\"app\",\"oidcConfig\":{\"clientId\":\"cid\"}}]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOidcApplicationByNameAsync(ProjectId, "app", OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("a-1");
        result.Value.OidcConfig!.ClientId.Should().Be("cid");
    }

    [Fact]
    public async Task GetOidcApplicationByNameAsync_WhenEmpty_ReturnsNotFound()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/_search")
            .Respond("application/json", "{\"result\":[]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOidcApplicationByNameAsync(ProjectId, "missing", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.AppNotFound");
    }

    [Fact]
    public async Task GetOidcApplicationByNameAsync_WhenSearchFails_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/_search")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"no\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.GetOidcApplicationByNameAsync(ProjectId, "app", OrgId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task SearchOrganizationsByNameAsync_OnSuccess_ReturnsResponse()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/organizations/_search")
            .Respond("application/json", "{\"result\":[{\"id\":\"o-1\",\"name\":\"Acme\"}]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.SearchOrganizationsByNameAsync("Acme");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Result!.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateOidcApplicationAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOidcApplicationAsync(
            ProjectId, AppId,
            new ZitadelUpdateOidcAppRequest { RedirectUris = ["https://x"] },
            OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOidcApplicationAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Put, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"message\":\"x\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.UpdateOidcApplicationAsync(
            ProjectId, AppId,
            new ZitadelUpdateOidcAppRequest { RedirectUris = ["https://x"] });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Zitadel.NotFound");
    }

    [Fact]
    public async Task DeleteApplicationAsync_OnSuccess_ReturnsSuccess()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Delete, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}")
            .Respond(HttpStatusCode.OK);

        var sut = CreateSut(mock);

        // Act
        var result = await sut.DeleteApplicationAsync(ProjectId, AppId, OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RegenerateOidcClientSecretAsync_OnSuccess_ReturnsSecret()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/management/v1/projects/{ProjectId}/apps/{AppId}/oidc_config/_generate_client_secret")
            .Respond("application/json", "{\"clientSecret\":\"new-sec\"}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.RegenerateOidcClientSecretAsync(ProjectId, AppId, OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClientSecret.Should().Be("new-sec");
    }

    [Fact]
    public async Task SearchUsersAsync_OnSuccess_ReturnsResponse()
    {
        // Arrange
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, $"{Authority}/v2/users")
            .Respond("application/json", "{\"result\":[{\"userId\":\"u-1\"}]}");

        var sut = CreateSut(mock);

        // Act
        var result = await sut.SearchUsersAsync(new ZitadelUserSearchRequest(), OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Result!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAccessToken_WhenClientCredentialsConfigured_FetchesTokenAndCachesIt()
    {
        // Arrange — no PAT, so the client must hit oauth/v2/token. We register the
        // token endpoint and a downstream API call; only one token call should happen
        // even though we make two API calls (caching path).
        var mock = new MockHttpMessageHandler();
        var tokenCalls = 0;
        mock.When(HttpMethod.Post, $"{Authority}/oauth/v2/token")
            .Respond(_ =>
            {
                tokenCalls++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"access_token\":\"the-token\",\"token_type\":\"Bearer\",\"expires_in\":3600}",
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            });
        mock.When(HttpMethod.Get, $"{Authority}/v2/users/u-1")
            .Respond("application/json", "{\"userId\":\"u-1\"}");

        var sut = CreateSut(mock, pat: null, clientId: "id", clientSecret: "sec");

        // Act
        var first = await sut.GetUserByIdAsync("u-1", OrgId);
        var second = await sut.GetUserByIdAsync("u-1", OrgId);

        // Assert — both calls succeed; the OAuth token endpoint is hit only once.
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        tokenCalls.Should().Be(1);
    }

    [Fact]
    public async Task Dispose_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut(new MockHttpMessageHandler());

        // Act
        var act = () => sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    private static ZitadelHttpClient CreateSut(
        MockHttpMessageHandler mock,
        string? pat = "test-pat",
        string? clientId = null,
        string? clientSecret = null)
    {
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions
        {
            Authority = Authority,
            PersonalAccessToken = pat,
            ClientId = clientId,
            ClientSecret = clientSecret,
            DefaultOrganizationId = OrgId
        });
        return new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }
}
