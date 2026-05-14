// -----------------------------------------------------------------------
// <copyright file="ProvisioningResultsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class OrganizationProvisioningResultTests
{
    [Fact]
    public void OrganizationProvisioningResult_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var result = new OrganizationProvisioningResult(
            ExternalOrganizationId: "ext-org-1",
            ExternalProjectId: "ext-proj-1",
            ClientId: "client-1",
            ClientSecret: "secret",
            AdminUserId: "admin-1");

        // Assert
        result.ExternalOrganizationId.Should().Be("ext-org-1");
        result.ExternalProjectId.Should().Be("ext-proj-1");
        result.ClientId.Should().Be("client-1");
        result.ClientSecret.Should().Be("secret");
        result.AdminUserId.Should().Be("admin-1");
    }

    [Fact]
    public void OrganizationProvisioningResult_Deconstruct_YieldsAllArguments()
    {
        // Arrange
        var result = new OrganizationProvisioningResult("o", "p", "c", "s", "a");

        // Act
        var (extOrg, extProj, clientId, clientSecret, adminId) = result;

        // Assert
        extOrg.Should().Be("o");
        extProj.Should().Be("p");
        clientId.Should().Be("c");
        clientSecret.Should().Be("s");
        adminId.Should().Be("a");
    }

    [Fact]
    public void OrganizationProvisioningResult_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new OrganizationProvisioningResult("o", "p", "c", "s", "a");
        var second = new OrganizationProvisioningResult("o", "p", "c", "s", "a");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}

public class ProjectProvisioningResultTests
{
    [Fact]
    public void ProjectProvisioningResult_Constructor_SetsExternalProjectId()
    {
        // Arrange / Act
        var result = new ProjectProvisioningResult(ExternalProjectId: "ext-p-1");

        // Assert
        result.ExternalProjectId.Should().Be("ext-p-1");
    }

    [Fact]
    public void ProjectProvisioningResult_RecordEquality_IsValueBased()
    {
        // Arrange / Act
        var first = new ProjectProvisioningResult("p");
        var second = new ProjectProvisioningResult("p");

        // Assert
        first.Should().Be(second);
    }
}

public class OidcAppProvisioningResultTests
{
    [Fact]
    public void OidcAppProvisioningResult_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var result = new OidcAppProvisioningResult(
            ClientId: "client-1",
            ClientSecret: "secret",
            ExternalAppId: "ext-app-1");

        // Assert
        result.ClientId.Should().Be("client-1");
        result.ClientSecret.Should().Be("secret");
        result.ExternalAppId.Should().Be("ext-app-1");
    }

    [Fact]
    public void OidcAppProvisioningResult_AcceptsNullExternalAppId()
    {
        // Arrange / Act
        var result = new OidcAppProvisioningResult("client-1", "secret", null);

        // Assert
        result.ExternalAppId.Should().BeNull();
    }

    [Fact]
    public void OidcAppProvisioningResult_With_ProducesUpdatedCopy()
    {
        // Arrange
        var original = new OidcAppProvisioningResult("c", "s", "a");

        // Act
        var rotated = original with { ClientSecret = "new-secret" };

        // Assert
        original.ClientSecret.Should().Be("s");
        rotated.ClientSecret.Should().Be("new-secret");
        rotated.ClientId.Should().Be("c");
    }
}

public class OidcAppSecretRotationResultTests
{
    [Fact]
    public void OidcAppSecretRotationResult_Constructor_SetsClientSecret()
    {
        // Arrange / Act
        var result = new OidcAppSecretRotationResult(ClientSecret: "rotated-secret");

        // Assert
        result.ClientSecret.Should().Be("rotated-secret");
    }

    [Fact]
    public void OidcAppSecretRotationResult_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new OidcAppSecretRotationResult("rotated");
        var second = new OidcAppSecretRotationResult("rotated");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
