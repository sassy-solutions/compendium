// -----------------------------------------------------------------------
// <copyright file="ZitadelTokenValidatorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
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
/// Unit tests for <see cref="ZitadelTokenValidator"/> covering both
/// <c>ValidateTokenAsync</c> (active/expired/inactive paths) and
/// <c>IntrospectTokenAsync</c> (full introspection result projection).
/// </summary>
public class ZitadelTokenValidatorTests
{
    private const string Authority = "https://zitadel.invalid";

    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var http = CreateHttp(new MockHttpMessageHandler());
        var options = Options.Create(new ZitadelOptions { Authority = Authority });

        // Act
        Action a1 = () => new ZitadelTokenValidator(null!, options, NullLogger<ZitadelTokenValidator>.Instance);
        Action a2 = () => new ZitadelTokenValidator(http, null!, NullLogger<ZitadelTokenValidator>.Instance);
        Action a3 = () => new ZitadelTokenValidator(http, options, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("options");
        a3.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenActive_ReturnsTokenInfo()
    {
        // Arrange — issued an hour ago, expires in an hour.
        var now = DateTimeOffset.UtcNow;
        var iat = now.AddMinutes(-30).ToUnixTimeSeconds();
        var exp = now.AddMinutes(30).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u-1\",\"iss\":\"https://zitadel.invalid\"," +
                   $"\"iat\":{iat},\"exp\":{exp},\"email\":\"u@x.test\",\"email_verified\":true," +
                   $"\"name\":\"Ada\",\"urn:zitadel:iam:org:id\":\"o-1\",\"scope\":\"openid profile\"," +
                   $"\"aud\":[\"a-1\",\"a-2\"]," +
                   $"\"urn:zitadel:iam:org:project:roles\":{{\"admin\":{{\"o-1\":\"x\"}},\"user\":{{}}}}}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("the-token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("u-1");
        result.Value.Email.Should().Be("u@x.test");
        result.Value.EmailVerified.Should().BeTrue();
        result.Value.Audience.Should().Contain("a-1");
        result.Value.Roles.Should().Contain("admin");
        result.Value.Scopes.Should().Contain("openid");
        result.Value.OrganizationId.Should().Be("o-1");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenInactive_ReturnsInvalidToken()
    {
        // Arrange
        var sut = CreateSut(WithIntrospect("{\"active\":false}"));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.InvalidToken");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenExpired_ReturnsTokenExpired()
    {
        // Arrange — exp in the past.
        var exp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var sut = CreateSut(WithIntrospect($"{{\"active\":true,\"sub\":\"u\",\"exp\":{exp}}}"));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.TokenExpired");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenIntrospectFails_PropagatesError()
    {
        // Arrange — introspect endpoint returns 4xx → InvalidToken from HttpClient.
        var sut = CreateSut(_ =>
            _.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
                .Respond(HttpStatusCode.BadRequest, "application/json", "x"));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.InvalidToken");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(_ => { });

        // Act
        Func<Task> act = () => sut.ValidateTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenActive_ReturnsResultWithTokenInfo()
    {
        // Arrange
        var exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"client_id\":\"cid\",\"token_type\":\"Bearer\",\"exp\":{exp}}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeTrue();
        result.Value.TokenInfo.Should().NotBeNull();
        result.Value.ClientId.Should().Be("cid");
        result.Value.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenInactiveAndExpired_ReturnsExpiredReason()
    {
        // Arrange
        var exp = DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds();
        var sut = CreateSut(WithIntrospect($"{{\"active\":false,\"exp\":{exp}}}"));

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeFalse();
        result.Value.InactiveReason.Should().Be("Token has expired");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenInactiveAndNotExpired_ReturnsRevokedReason()
    {
        // Arrange — no exp claim, so the "invalid or revoked" branch fires.
        var sut = CreateSut(WithIntrospect("{\"active\":false}"));

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Active.Should().BeFalse();
        result.Value.InactiveReason.Should().Contain("invalid");
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenIntrospectFails_PropagatesError()
    {
        // Arrange
        var sut = CreateSut(_ =>
            _.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
                .Respond(HttpStatusCode.BadRequest, "application/json", "x"));

        // Act
        var result = await sut.IntrospectTokenAsync("any");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(_ => { });

        // Act
        Func<Task> act = () => sut.IntrospectTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenAudIsString_ParsesAudienceAsSingleItem()
    {
        // Arrange — aud as string, not array. Triggers the audString branch.
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"iat\":{iat},\"exp\":{exp},\"aud\":\"only-one\"}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Audience.Should().ContainSingle().Which.Should().Be("only-one");
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenAudIsMissing_AudienceIsNull()
    {
        // Arrange
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"iat\":{iat},\"exp\":{exp}}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Audience.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenScopeIsEmpty_ScopesIsNull()
    {
        // Arrange
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"iat\":{iat},\"exp\":{exp}}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scopes.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenRolesEmpty_RolesIsNull()
    {
        // Arrange
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"iat\":{iat},\"exp\":{exp}," +
                   $"\"urn:zitadel:iam:org:project:roles\":{{}}}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenResourceOwnerOnly_OrgIdFallsBackToResourceOwner()
    {
        // Arrange — no urn:zitadel:iam:org:id, only resourceownerid.
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var json = $"{{\"active\":true,\"sub\":\"u\",\"iat\":{iat},\"exp\":{exp}," +
                   $"\"urn:zitadel:iam:user:resourceowner:id\":\"ro-1\"}}";
        var sut = CreateSut(WithIntrospect(json));

        // Act
        var result = await sut.ValidateTokenAsync("any");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizationId.Should().Be("ro-1");
    }

    private static Action<MockHttpMessageHandler> WithIntrospect(string json) =>
        m => m.When(HttpMethod.Post, $"{Authority}/oauth/v2/introspect")
              .Respond("application/json", json);

    private static ZitadelHttpClient CreateHttp(MockHttpMessageHandler mock)
    {
        var http = mock.ToHttpClient();
        var options = Options.Create(new ZitadelOptions
        {
            Authority = Authority,
            PersonalAccessToken = "test-pat",
            ClientId = "cid",
            ClientSecret = "csec"
        });
        return new ZitadelHttpClient(http, options, NullLogger<ZitadelHttpClient>.Instance);
    }

    private static ZitadelTokenValidator CreateSut(Action<MockHttpMessageHandler> configure)
    {
        var mock = new MockHttpMessageHandler();
        configure(mock);
        var http = CreateHttp(mock);
        var options = Options.Create(new ZitadelOptions { Authority = Authority });
        return new ZitadelTokenValidator(http, options, NullLogger<ZitadelTokenValidator>.Instance);
    }
}
