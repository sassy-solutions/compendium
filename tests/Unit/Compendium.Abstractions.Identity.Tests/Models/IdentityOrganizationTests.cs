// -----------------------------------------------------------------------
// <copyright file="IdentityOrganizationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class IdentityOrganizationTests
{
    [Fact]
    public void IdentityOrganization_WithRequiredProperties_InitializesSuccessfully()
    {
        // Arrange / Act
        var org = new IdentityOrganization
        {
            Id = "org-1",
            Name = "Acme"
        };

        // Assert
        org.Id.Should().Be("org-1");
        org.Name.Should().Be("Acme");
    }

    [Fact]
    public void IdentityOrganization_DefaultIsActive_IsTrue()
    {
        // Arrange / Act
        var org = new IdentityOrganization
        {
            Id = "org-1",
            Name = "Acme"
        };

        // Assert
        org.IsActive.Should().BeTrue();
        org.Domain.Should().BeNull();
        org.Metadata.Should().BeNull();
        org.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void IdentityOrganization_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);
        var metadata = new Dictionary<string, object> { ["industry"] = "tech" };

        // Act
        var org = new IdentityOrganization
        {
            Id = "org-1",
            Name = "Acme",
            Domain = "acme.com",
            IsActive = false,
            Metadata = metadata,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        org.Id.Should().Be("org-1");
        org.Name.Should().Be("Acme");
        org.Domain.Should().Be("acme.com");
        org.IsActive.Should().BeFalse();
        org.Metadata.Should().BeSameAs(metadata);
        org.CreatedAt.Should().Be(createdAt);
        org.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void IdentityOrganization_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var first = new IdentityOrganization { Id = "org-1", Name = "Acme", CreatedAt = createdAt };
        var second = new IdentityOrganization { Id = "org-1", Name = "Acme", CreatedAt = createdAt };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void IdentityOrganization_TwoInstancesWithDifferentNames_AreNotEqual()
    {
        // Arrange
        var first = new IdentityOrganization { Id = "org-1", Name = "Acme" };
        var second = new IdentityOrganization { Id = "org-1", Name = "Globex" };

        // Act / Assert
        first.Should().NotBe(second);
    }
}

public class OrganizationMemberTests
{
    [Fact]
    public void OrganizationMember_WithRequiredProperties_InitializesSuccessfully()
    {
        // Arrange / Act
        var member = new OrganizationMember
        {
            UserId = "user-1",
            Email = "u@example.com",
            Roles = new[] { "owner" }
        };

        // Assert
        member.UserId.Should().Be("user-1");
        member.Email.Should().Be("u@example.com");
        member.Roles.Should().ContainSingle().Which.Should().Be("owner");
    }

    [Fact]
    public void OrganizationMember_DefaultIsActive_IsTrue()
    {
        // Arrange / Act
        var member = new OrganizationMember
        {
            UserId = "u-1",
            Email = "u@example.com",
            Roles = Array.Empty<string>()
        };

        // Assert
        member.IsActive.Should().BeTrue();
        member.DisplayName.Should().BeNull();
    }

    [Fact]
    public void OrganizationMember_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var joinedAt = DateTimeOffset.UtcNow;
        var roles = new[] { "admin", "billing" };

        // Act
        var member = new OrganizationMember
        {
            UserId = "u-1",
            Email = "u@example.com",
            DisplayName = "Test User",
            Roles = roles,
            JoinedAt = joinedAt,
            IsActive = false
        };

        // Assert
        member.UserId.Should().Be("u-1");
        member.Email.Should().Be("u@example.com");
        member.DisplayName.Should().Be("Test User");
        member.Roles.Should().BeEquivalentTo(roles);
        member.JoinedAt.Should().Be(joinedAt);
        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void OrganizationMember_RecordEquality_IgnoresReferenceIdentity()
    {
        // Arrange
        var joinedAt = DateTimeOffset.UtcNow;
        var first = new OrganizationMember
        {
            UserId = "u-1",
            Email = "u@example.com",
            Roles = new[] { "admin" },
            JoinedAt = joinedAt
        };
        var second = first with { };

        // Act / Assert
        first.Should().Be(second);
        ReferenceEquals(first, second).Should().BeFalse();
    }
}
