// -----------------------------------------------------------------------
// <copyright file="ZitadelClaimsTransformationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Compendium.Adapters.Zitadel.Authentication;
using Compendium.Adapters.Zitadel.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Zitadel.Tests.Authentication;

/// <summary>
/// Unit tests for <see cref="ZitadelClaimsTransformation"/> covering both the
/// instance <c>TransformAsync</c> mapping logic and the static helpers.
/// </summary>
public class ZitadelClaimsTransformationTests
{
    [Fact]
    public void Ctor_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ZitadelOptions { Authority = "https://x" });

        // Act
        Action a1 = () => new ZitadelClaimsTransformation(null!, NullLogger<ZitadelClaimsTransformation>.Instance);
        Action a2 = () => new ZitadelClaimsTransformation(options, null!);

        // Assert
        a1.Should().Throw<ArgumentNullException>().WithParameterName("options");
        a2.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void TransformAsync_WithOrgIdClaim_AddsTenantIdAndOrgIdClaims()
    {
        // Arrange
        var sut = CreateSut();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelOrgIdClaimType, "o-1")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = sut.TransformAsync(principal);

        // Assert
        result.HasClaim(c => c.Type == ZitadelClaimsTransformation.TenantIdClaimType && c.Value == "o-1")
            .Should().BeTrue();
        result.HasClaim(c => c.Type == ZitadelClaimsTransformation.OrganizationIdClaimType && c.Value == "o-1")
            .Should().BeTrue();
    }

    [Fact]
    public void TransformAsync_WithResourceOwnerOnly_FallsBackForOrgId()
    {
        // Arrange
        var sut = CreateSut();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelResourceOwnerClaimType, "ro-1")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = sut.TransformAsync(principal);

        // Assert
        result.FindFirst(ZitadelClaimsTransformation.TenantIdClaimType)!.Value.Should().Be("ro-1");
    }

    [Fact]
    public void TransformAsync_WhenTenantIdAlreadyPresent_DoesNotDuplicate()
    {
        // Arrange
        var sut = CreateSut();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelOrgIdClaimType, "o-1"),
            new Claim(ZitadelClaimsTransformation.TenantIdClaimType, "preset"),
            new Claim(ZitadelClaimsTransformation.OrganizationIdClaimType, "preset")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = sut.TransformAsync(principal);

        // Assert — preserved, not overwritten.
        result.FindAll(ZitadelClaimsTransformation.TenantIdClaimType).Should().HaveCount(1);
        result.FindFirst(ZitadelClaimsTransformation.TenantIdClaimType)!.Value.Should().Be("preset");
    }

    [Fact]
    public void TransformAsync_WithRolesJson_AddsRoleClaims()
    {
        // Arrange — roles is a JSON object {"role": {"orgId": "x"}, ...}.
        var sut = CreateSut();
        var rolesJson = "{\"admin\":{\"o-1\":\"x\"},\"viewer\":{}}";
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelRolesClaimType, rolesJson)
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = sut.TransformAsync(principal);

        // Assert
        result.HasClaim(ClaimTypes.Role, "admin").Should().BeTrue();
        result.HasClaim(ClaimTypes.Role, "viewer").Should().BeTrue();
    }

    [Fact]
    public void TransformAsync_WithExistingRole_DoesNotDuplicate()
    {
        // Arrange
        var sut = CreateSut();
        var rolesJson = "{\"admin\":{}}";
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelRolesClaimType, rolesJson),
            new Claim(ClaimTypes.Role, "admin")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = sut.TransformAsync(principal);

        // Assert — only one admin role claim total.
        result.FindAll(ClaimTypes.Role).Where(c => c.Value == "admin").Should().HaveCount(1);
    }

    [Fact]
    public void TransformAsync_WithMalformedRolesJson_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelRolesClaimType, "{this-is-not-json")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var act = () => sut.TransformAsync(principal);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TransformAsync_WithNonClaimsIdentity_ReturnsPrincipalUnchanged()
    {
        // Arrange — a principal whose Identity is not a ClaimsIdentity.
        var sut = CreateSut();
        var nonClaimsIdentity = new System.Security.Principal.GenericIdentity("user", "Negotiate");
        var principal = new ClaimsPrincipal(new[] { nonClaimsIdentity }) { /* no-op */ };
        // ClaimsPrincipal wraps GenericIdentity in a ClaimsIdentity — to truly test the
        // non-ClaimsIdentity branch we need a principal whose primary Identity is null.
        var emptyPrincipal = new ClaimsPrincipal();

        // Act
        var result = sut.TransformAsync(emptyPrincipal);

        // Assert — returns same principal without throwing.
        result.Should().BeSameAs(emptyPrincipal);
    }

    [Fact]
    public void TransformAsync_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.TransformAsync(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTenantId_WithNullPrincipal_ReturnsNull()
    {
        // Arrange / Act
        var result = ZitadelClaimsTransformation.GetTenantId(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WithTenantIdClaim_ReturnsValue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.TenantIdClaimType, "tid")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetTenantId(principal);

        // Assert
        result.Should().Be("tid");
    }

    [Fact]
    public void GetTenantId_WithOrgIdClaim_ReturnsValue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelOrgIdClaimType, "o-1")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetTenantId(principal);

        // Assert
        result.Should().Be("o-1");
    }

    [Fact]
    public void GetTenantId_WithResourceOwnerClaim_ReturnsValue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ZitadelClaimsTransformation.ZitadelResourceOwnerClaimType, "ro-1")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetTenantId(principal);

        // Assert
        result.Should().Be("ro-1");
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsValue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u-1")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetUserId(principal);

        // Assert
        result.Should().Be("u-1");
    }

    [Fact]
    public void GetUserId_WithSubClaim_ReturnsValue()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "sub-1")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetUserId(principal);

        // Assert
        result.Should().Be("sub-1");
    }

    [Fact]
    public void GetUserId_WithNullPrincipal_ReturnsNull()
    {
        // Arrange / Act
        var result = ZitadelClaimsTransformation.GetUserId(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRoles_WithRoleClaims_ReturnsList()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "user")
        }));

        // Act
        var result = ZitadelClaimsTransformation.GetRoles(principal);

        // Assert
        result.Should().BeEquivalentTo(new[] { "admin", "user" });
    }

    [Fact]
    public void GetRoles_WithNullPrincipal_ReturnsEmpty()
    {
        // Arrange / Act
        var result = ZitadelClaimsTransformation.GetRoles(null);

        // Assert
        result.Should().BeEmpty();
    }

    private static ZitadelClaimsTransformation CreateSut()
    {
        var options = Options.Create(new ZitadelOptions { Authority = "https://zitadel.invalid" });
        return new ZitadelClaimsTransformation(options, NullLogger<ZitadelClaimsTransformation>.Instance);
    }
}
