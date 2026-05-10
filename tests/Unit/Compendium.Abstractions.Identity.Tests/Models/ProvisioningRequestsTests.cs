// -----------------------------------------------------------------------
// <copyright file="ProvisioningRequestsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class OrganizationProvisioningRequestTests
{
    [Fact]
    public void OrganizationProvisioningRequest_Constructor_SetsAllProperties()
    {
        // Arrange
        var admin = new AdminUserProvisioningRequest("admin@acme.com", "First", "Last", "p@ss", "en");

        // Act
        var request = new OrganizationProvisioningRequest(
            OrganizationId: "org-1",
            Name: "Acme",
            DisplayName: "Acme Corp",
            PlanId: "pro",
            AdminUser: admin);

        // Assert
        request.OrganizationId.Should().Be("org-1");
        request.Name.Should().Be("Acme");
        request.DisplayName.Should().Be("Acme Corp");
        request.PlanId.Should().Be("pro");
        request.AdminUser.Should().BeSameAs(admin);
    }

    [Fact]
    public void OrganizationProvisioningRequest_AcceptsNullDisplayName()
    {
        // Arrange
        var admin = new AdminUserProvisioningRequest("admin@acme.com", "F", "L", null, null);

        // Act
        var request = new OrganizationProvisioningRequest("org-1", "Acme", null, "free", admin);

        // Assert
        request.DisplayName.Should().BeNull();
    }

    [Fact]
    public void OrganizationProvisioningRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var admin = new AdminUserProvisioningRequest("a@b.com", "F", "L", null, null);
        var first = new OrganizationProvisioningRequest("org-1", "Acme", "Acme Corp", "pro", admin);
        var second = new OrganizationProvisioningRequest("org-1", "Acme", "Acme Corp", "pro", admin);

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void OrganizationProvisioningRequest_Deconstruct_YieldsAllArguments()
    {
        // Arrange
        var admin = new AdminUserProvisioningRequest("a@b.com", "F", "L", null, null);
        var request = new OrganizationProvisioningRequest("org-1", "Acme", "Acme Corp", "pro", admin);

        // Act
        var (orgId, name, displayName, planId, adminUser) = request;

        // Assert
        orgId.Should().Be("org-1");
        name.Should().Be("Acme");
        displayName.Should().Be("Acme Corp");
        planId.Should().Be("pro");
        adminUser.Should().BeSameAs(admin);
    }
}

public class AdminUserProvisioningRequestTests
{
    [Fact]
    public void AdminUserProvisioningRequest_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var admin = new AdminUserProvisioningRequest(
            Email: "admin@acme.com",
            FirstName: "Jane",
            LastName: "Doe",
            Password: "secret",
            PreferredLanguage: "en");

        // Assert
        admin.Email.Should().Be("admin@acme.com");
        admin.FirstName.Should().Be("Jane");
        admin.LastName.Should().Be("Doe");
        admin.Password.Should().Be("secret");
        admin.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public void AdminUserProvisioningRequest_AcceptsNullPasswordAndLanguage()
    {
        // Arrange / Act
        var admin = new AdminUserProvisioningRequest("admin@acme.com", "Jane", "Doe", null, null);

        // Assert
        admin.Password.Should().BeNull();
        admin.PreferredLanguage.Should().BeNull();
    }

    [Fact]
    public void AdminUserProvisioningRequest_With_ProducesUpdatedCopy()
    {
        // Arrange
        var original = new AdminUserProvisioningRequest("a@b.com", "F", "L", "p", "en");

        // Act
        var updated = original with { Password = "new-pass" };

        // Assert
        original.Password.Should().Be("p");
        updated.Password.Should().Be("new-pass");
        updated.Email.Should().Be("a@b.com");
    }
}

public class ProjectProvisioningRequestTests
{
    [Fact]
    public void ProjectProvisioningRequest_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var request = new ProjectProvisioningRequest(
            ProjectId: "p-1",
            OrganizationId: "org-1",
            ExternalOrganizationId: "ext-org-1",
            ProjectName: "Project One");

        // Assert
        request.ProjectId.Should().Be("p-1");
        request.OrganizationId.Should().Be("org-1");
        request.ExternalOrganizationId.Should().Be("ext-org-1");
        request.ProjectName.Should().Be("Project One");
    }

    [Fact]
    public void ProjectProvisioningRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new ProjectProvisioningRequest("p-1", "o", "ext", "name");
        var second = new ProjectProvisioningRequest("p-1", "o", "ext", "name");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}

public class OidcAppProvisioningRequestTests
{
    [Fact]
    public void OidcAppProvisioningRequest_Constructor_SetsAllProperties()
    {
        // Arrange
        var redirects = new[] { "https://app.example.com/callback" };
        var postLogout = new[] { "https://app.example.com/" };

        // Act
        var request = new OidcAppProvisioningRequest(
            ExternalProjectId: "ext-p-1",
            ExternalOrganizationId: "ext-o-1",
            AppName: "Web",
            RedirectUris: redirects,
            PostLogoutRedirectUris: postLogout);

        // Assert
        request.ExternalProjectId.Should().Be("ext-p-1");
        request.ExternalOrganizationId.Should().Be("ext-o-1");
        request.AppName.Should().Be("Web");
        request.RedirectUris.Should().BeSameAs(redirects);
        request.PostLogoutRedirectUris.Should().BeSameAs(postLogout);
    }

    [Fact]
    public void OidcAppProvisioningRequest_AcceptsEmptyUriLists()
    {
        // Arrange / Act
        var request = new OidcAppProvisioningRequest(
            "ext-p-1",
            "ext-o-1",
            "Web",
            Array.Empty<string>(),
            Array.Empty<string>());

        // Assert
        request.RedirectUris.Should().BeEmpty();
        request.PostLogoutRedirectUris.Should().BeEmpty();
    }
}

public class OidcAppUpdateRequestTests
{
    [Fact]
    public void OidcAppUpdateRequest_Constructor_SetsAllProperties()
    {
        // Arrange
        var redirects = new[] { "https://x" };
        var postLogout = new[] { "https://y" };

        // Act
        var request = new OidcAppUpdateRequest(
            ExternalProjectId: "ext-p",
            ExternalAppId: "ext-app",
            ExternalOrganizationId: "ext-o",
            RedirectUris: redirects,
            PostLogoutRedirectUris: postLogout);

        // Assert
        request.ExternalProjectId.Should().Be("ext-p");
        request.ExternalAppId.Should().Be("ext-app");
        request.ExternalOrganizationId.Should().Be("ext-o");
        request.RedirectUris.Should().BeSameAs(redirects);
        request.PostLogoutRedirectUris.Should().BeSameAs(postLogout);
    }
}

public class OidcAppDeleteRequestTests
{
    [Fact]
    public void OidcAppDeleteRequest_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var request = new OidcAppDeleteRequest(
            ExternalProjectId: "ext-p",
            ExternalAppId: "ext-app",
            ExternalOrganizationId: "ext-o");

        // Assert
        request.ExternalProjectId.Should().Be("ext-p");
        request.ExternalAppId.Should().Be("ext-app");
        request.ExternalOrganizationId.Should().Be("ext-o");
    }

    [Fact]
    public void OidcAppDeleteRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new OidcAppDeleteRequest("p", "a", "o");
        var second = new OidcAppDeleteRequest("p", "a", "o");

        // Act / Assert
        first.Should().Be(second);
    }
}

public class OidcAppSecretRotationRequestTests
{
    [Fact]
    public void OidcAppSecretRotationRequest_Constructor_SetsAllProperties()
    {
        // Arrange / Act
        var request = new OidcAppSecretRotationRequest(
            ExternalProjectId: "ext-p",
            ExternalAppId: "ext-app",
            ExternalOrganizationId: "ext-o");

        // Assert
        request.ExternalProjectId.Should().Be("ext-p");
        request.ExternalAppId.Should().Be("ext-app");
        request.ExternalOrganizationId.Should().Be("ext-o");
    }
}
