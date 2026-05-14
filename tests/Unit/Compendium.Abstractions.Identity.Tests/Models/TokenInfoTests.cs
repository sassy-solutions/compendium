// -----------------------------------------------------------------------
// <copyright file="TokenInfoTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class TokenInfoTests
{
    [Fact]
    public void TokenInfo_WithRequiredProperties_InitializesSuccessfully()
    {
        // Arrange / Act
        var token = new TokenInfo
        {
            Subject = "user-1",
            Issuer = "https://issuer.example.com"
        };

        // Assert
        token.Subject.Should().Be("user-1");
        token.Issuer.Should().Be("https://issuer.example.com");
    }

    [Fact]
    public void TokenInfo_OptionalProperties_AreNullByDefault()
    {
        // Arrange / Act
        var token = new TokenInfo
        {
            Subject = "user-1",
            Issuer = "https://issuer.example.com"
        };

        // Assert
        token.Audience.Should().BeNull();
        token.Email.Should().BeNull();
        token.EmailVerified.Should().BeNull();
        token.Name.Should().BeNull();
        token.OrganizationId.Should().BeNull();
        token.Roles.Should().BeNull();
        token.Scopes.Should().BeNull();
        token.Claims.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        // Arrange
        var token = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        // Act / Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        // Arrange
        var token = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act / Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsDefault_ReturnsTrue()
    {
        // Arrange
        var token = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss"
        };

        // Act / Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void TokenInfo_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddHours(1);
        var audience = new[] { "api", "web" };
        var roles = new[] { "admin" };
        var scopes = new[] { "openid", "profile" };
        var claims = new Dictionary<string, object> { ["custom"] = "val" };

        // Act
        var token = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "https://iss",
            Audience = audience,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Email = "u@example.com",
            EmailVerified = true,
            Name = "Test",
            OrganizationId = "org-1",
            Roles = roles,
            Scopes = scopes,
            Claims = claims
        };

        // Assert
        token.Subject.Should().Be("u-1");
        token.Issuer.Should().Be("https://iss");
        token.Audience.Should().BeEquivalentTo(audience);
        token.IssuedAt.Should().Be(issuedAt);
        token.ExpiresAt.Should().Be(expiresAt);
        token.Email.Should().Be("u@example.com");
        token.EmailVerified.Should().BeTrue();
        token.Name.Should().Be("Test");
        token.OrganizationId.Should().Be("org-1");
        token.Roles.Should().BeEquivalentTo(roles);
        token.Scopes.Should().BeEquivalentTo(scopes);
        token.Claims.Should().BeSameAs(claims);
    }

    [Fact]
    public void TokenInfo_RecordEquality_OnSameValues_IsTrue()
    {
        // Arrange
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddHours(1);
        var first = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss",
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };
        var second = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss",
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}

public class TokenIntrospectionResultTests
{
    [Fact]
    public void TokenIntrospectionResult_WithRequiredActiveProperty_InitializesSuccessfully()
    {
        // Arrange / Act
        var result = new TokenIntrospectionResult { Active = true };

        // Assert
        result.Active.Should().BeTrue();
        result.TokenInfo.Should().BeNull();
        result.TokenType.Should().BeNull();
        result.ClientId.Should().BeNull();
        result.InactiveReason.Should().BeNull();
    }

    [Fact]
    public void TokenIntrospectionResult_Active_WithTokenInfo_RoundTripsAllProperties()
    {
        // Arrange
        var token = new TokenInfo
        {
            Subject = "u-1",
            Issuer = "iss",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        // Act
        var result = new TokenIntrospectionResult
        {
            Active = true,
            TokenInfo = token,
            TokenType = "Bearer",
            ClientId = "client-1"
        };

        // Assert
        result.Active.Should().BeTrue();
        result.TokenInfo.Should().BeSameAs(token);
        result.TokenType.Should().Be("Bearer");
        result.ClientId.Should().Be("client-1");
    }

    [Fact]
    public void TokenIntrospectionResult_Inactive_CarriesInactiveReason()
    {
        // Arrange / Act
        var result = new TokenIntrospectionResult
        {
            Active = false,
            InactiveReason = "Token revoked"
        };

        // Assert
        result.Active.Should().BeFalse();
        result.InactiveReason.Should().Be("Token revoked");
    }

    [Fact]
    public void TokenIntrospectionResult_With_ReturnsCopyWithUpdatedProperty()
    {
        // Arrange
        var original = new TokenIntrospectionResult { Active = true, TokenType = "Bearer" };

        // Act
        var updated = original with { Active = false, InactiveReason = "expired" };

        // Assert
        original.Active.Should().BeTrue();
        updated.Active.Should().BeFalse();
        updated.InactiveReason.Should().Be("expired");
        updated.TokenType.Should().Be("Bearer");
    }
}
