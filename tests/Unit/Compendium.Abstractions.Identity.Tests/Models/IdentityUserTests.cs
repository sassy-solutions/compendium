// -----------------------------------------------------------------------
// <copyright file="IdentityUserTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models;

public class IdentityUserTests
{
    [Fact]
    public void IdentityUser_WithRequiredProperties_InitializesSuccessfully()
    {
        // Arrange / Act
        var user = new IdentityUser
        {
            Id = "user-1",
            Email = "user@example.com"
        };

        // Assert
        user.Id.Should().Be("user-1");
        user.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void IdentityUser_DefaultIsActive_IsTrue()
    {
        // Arrange / Act
        var user = new IdentityUser
        {
            Id = "user-1",
            Email = "user@example.com"
        };

        // Assert
        user.IsActive.Should().BeTrue();
        user.EmailVerified.Should().BeFalse();
        user.PhoneVerified.Should().BeFalse();
    }

    [Fact]
    public void IdentityUser_OptionalProperties_AreNullByDefault()
    {
        // Arrange / Act
        var user = new IdentityUser
        {
            Id = "user-1",
            Email = "user@example.com"
        };

        // Assert
        user.Username.Should().BeNull();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.DisplayName.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.PreferredLanguage.Should().BeNull();
        user.Timezone.Should().BeNull();
        user.ProfilePictureUrl.Should().BeNull();
        user.Metadata.Should().BeNull();
        user.UpdatedAt.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
        user.OrganizationId.Should().BeNull();
        user.Roles.Should().BeNull();
    }

    [Fact]
    public void IdentityUser_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);
        var lastLoginAt = createdAt.AddMinutes(10);
        var metadata = new Dictionary<string, object> { ["plan"] = "pro" };
        var roles = new[] { "admin", "user" };

        // Act
        var user = new IdentityUser
        {
            Id = "u-1",
            Email = "u@example.com",
            Username = "uname",
            FirstName = "First",
            LastName = "Last",
            DisplayName = "First Last",
            PhoneNumber = "+15551234567",
            EmailVerified = true,
            PhoneVerified = true,
            IsActive = false,
            PreferredLanguage = "en",
            Timezone = "UTC",
            ProfilePictureUrl = "https://example.com/me.png",
            Metadata = metadata,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastLoginAt = lastLoginAt,
            OrganizationId = "org-1",
            Roles = roles
        };

        // Assert
        user.Id.Should().Be("u-1");
        user.Email.Should().Be("u@example.com");
        user.Username.Should().Be("uname");
        user.FirstName.Should().Be("First");
        user.LastName.Should().Be("Last");
        user.DisplayName.Should().Be("First Last");
        user.PhoneNumber.Should().Be("+15551234567");
        user.EmailVerified.Should().BeTrue();
        user.PhoneVerified.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.PreferredLanguage.Should().Be("en");
        user.Timezone.Should().Be("UTC");
        user.ProfilePictureUrl.Should().Be("https://example.com/me.png");
        user.Metadata.Should().BeSameAs(metadata);
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(updatedAt);
        user.LastLoginAt.Should().Be(lastLoginAt);
        user.OrganizationId.Should().Be("org-1");
        user.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void IdentityUser_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var first = new IdentityUser
        {
            Id = "user-1",
            Email = "user@example.com",
            CreatedAt = createdAt
        };
        var second = new IdentityUser
        {
            Id = "user-1",
            Email = "user@example.com",
            CreatedAt = createdAt
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void IdentityUser_TwoInstancesWithDifferentIds_AreNotEqual()
    {
        // Arrange
        var first = new IdentityUser { Id = "u-1", Email = "u@example.com" };
        var second = new IdentityUser { Id = "u-2", Email = "u@example.com" };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void IdentityUser_With_ReturnsCopyWithUpdatedProperty()
    {
        // Arrange
        var original = new IdentityUser
        {
            Id = "u-1",
            Email = "u@example.com",
            FirstName = "Old"
        };

        // Act
        var updated = original with { FirstName = "New" };

        // Assert
        original.FirstName.Should().Be("Old");
        updated.FirstName.Should().Be("New");
        updated.Id.Should().Be("u-1");
        updated.Email.Should().Be("u@example.com");
    }
}
