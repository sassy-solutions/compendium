// -----------------------------------------------------------------------
// <copyright file="CrmCompanyTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests.Models;

public class CrmCompanyTests
{
    [Fact]
    public void CrmCompany_Construct_AssignsAllProperties()
    {
        // Arrange
        var props = new Dictionary<string, object> { ["industry"] = "saas" };

        // Act
        var company = new CrmCompany(
            ExternalId: "co-1",
            Name: "Acme",
            Domain: "acme.example",
            Properties: props,
            TenantId: "tenant-1");

        // Assert
        company.ExternalId.Should().Be("co-1");
        company.Name.Should().Be("Acme");
        company.Domain.Should().Be("acme.example");
        company.Properties.Should().BeSameAs(props);
        company.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void CrmCompany_Construct_AllowsNullOptionalFields()
    {
        // Act
        var company = new CrmCompany("co-2", "Acme", null, null, "tenant-1");

        // Assert
        company.Domain.Should().BeNull();
        company.Properties.Should().BeNull();
    }

    [Fact]
    public void CrmCompany_Equality_IsValueBased()
    {
        // Arrange
        var a = new CrmCompany("co-1", "Acme", "acme.example", null, "tenant-1");
        var b = new CrmCompany("co-1", "Acme", "acme.example", null, "tenant-1");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CrmCompany_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new CrmCompany("co-1", "Acme", null, null, "tenant-1");

        // Act
        var modified = original with { Name = "Acme Corp" };

        // Assert
        modified.Should().NotBe(original);
        modified.Name.Should().Be("Acme Corp");
        modified.ExternalId.Should().Be("co-1");
    }
}
