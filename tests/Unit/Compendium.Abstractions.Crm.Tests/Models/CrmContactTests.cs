// -----------------------------------------------------------------------
// <copyright file="CrmContactTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests.Models;

public class CrmContactTests
{
    [Fact]
    public void CrmContact_Construct_AssignsAllProperties()
    {
        // Arrange
        var props = new Dictionary<string, object> { ["plan"] = "pro" };

        // Act
        var contact = new CrmContact(
            ExternalId: "ext-1",
            Email: "user@example.com",
            FirstName: "Ada",
            LastName: "Lovelace",
            Properties: props,
            TenantId: "tenant-1");

        // Assert
        contact.ExternalId.Should().Be("ext-1");
        contact.Email.Should().Be("user@example.com");
        contact.FirstName.Should().Be("Ada");
        contact.LastName.Should().Be("Lovelace");
        contact.Properties.Should().BeSameAs(props);
        contact.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void CrmContact_Construct_AllowsNullOptionalFields()
    {
        // Act
        var contact = new CrmContact("ext-2", "user@example.com", null, null, null, "tenant-1");

        // Assert
        contact.FirstName.Should().BeNull();
        contact.LastName.Should().BeNull();
        contact.Properties.Should().BeNull();
    }

    [Fact]
    public void CrmContact_Equality_IsValueBased()
    {
        // Arrange
        var a = new CrmContact("ext-1", "user@example.com", "Ada", "L", null, "tenant-1");
        var b = new CrmContact("ext-1", "user@example.com", "Ada", "L", null, "tenant-1");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CrmContact_Inequality_DiffersWhenAnyFieldDiffers()
    {
        // Arrange
        var a = new CrmContact("ext-1", "user@example.com", "Ada", "L", null, "tenant-1");
        var b = a with { TenantId = "tenant-2" };

        // Act / Assert
        a.Should().NotBe(b);
    }
}
