// -----------------------------------------------------------------------
// <copyright file="CrmAssociationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests.Models;

public class CrmAssociationTests
{
    [Fact]
    public void CrmAssociation_Construct_AssignsAllProperties()
    {
        // Act
        var assoc = new CrmAssociation(
            FromType: "contact",
            FromId: "ext-1",
            ToType: "company",
            ToId: "co-1",
            Role: "primary_contact");

        // Assert
        assoc.FromType.Should().Be("contact");
        assoc.FromId.Should().Be("ext-1");
        assoc.ToType.Should().Be("company");
        assoc.ToId.Should().Be("co-1");
        assoc.Role.Should().Be("primary_contact");
    }

    [Fact]
    public void CrmAssociation_Construct_AllowsNullRole()
    {
        // Act
        var assoc = new CrmAssociation("contact", "ext-1", "company", "co-1", null);

        // Assert
        assoc.Role.Should().BeNull();
    }

    [Fact]
    public void CrmAssociation_Equality_IsValueBased()
    {
        // Arrange
        var a = new CrmAssociation("contact", "ext-1", "company", "co-1", "primary_contact");
        var b = new CrmAssociation("contact", "ext-1", "company", "co-1", "primary_contact");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CrmAssociation_Inequality_WhenRoleDiffers()
    {
        // Arrange
        var a = new CrmAssociation("contact", "ext-1", "company", "co-1", "primary_contact");
        var b = a with { Role = "decision_maker" };

        // Act / Assert
        a.Should().NotBe(b);
    }
}
