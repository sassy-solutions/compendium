// -----------------------------------------------------------------------
// <copyright file="CreateUserRequestTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models.Requests;

public class CreateUserRequestTests
{
    [Fact]
    public void CreateUserRequest_WithRequiredEmail_InitializesSuccessfully()
    {
        // Arrange / Act
        var request = new CreateUserRequest { Email = "u@example.com" };

        // Assert
        request.Email.Should().Be("u@example.com");
    }

    [Fact]
    public void CreateUserRequest_DefaultSendVerificationEmail_IsTrue()
    {
        // Arrange / Act
        var request = new CreateUserRequest { Email = "u@example.com" };

        // Assert
        request.SendVerificationEmail.Should().BeTrue();
    }

    [Fact]
    public void CreateUserRequest_OptionalProperties_AreNullByDefault()
    {
        // Arrange / Act
        var request = new CreateUserRequest { Email = "u@example.com" };

        // Assert
        request.Username.Should().BeNull();
        request.FirstName.Should().BeNull();
        request.LastName.Should().BeNull();
        request.DisplayName.Should().BeNull();
        request.PhoneNumber.Should().BeNull();
        request.Password.Should().BeNull();
        request.PreferredLanguage.Should().BeNull();
        request.Timezone.Should().BeNull();
        request.OrganizationId.Should().BeNull();
        request.Roles.Should().BeNull();
        request.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateUserRequest_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var roles = new[] { "admin" };
        var metadata = new Dictionary<string, object> { ["k"] = "v" };

        // Act
        var request = new CreateUserRequest
        {
            Email = "u@example.com",
            Username = "uname",
            FirstName = "First",
            LastName = "Last",
            DisplayName = "Display",
            PhoneNumber = "+15551234567",
            Password = "p@ss",
            PreferredLanguage = "en",
            Timezone = "UTC",
            OrganizationId = "org-1",
            Roles = roles,
            Metadata = metadata,
            SendVerificationEmail = false
        };

        // Assert
        request.Email.Should().Be("u@example.com");
        request.Username.Should().Be("uname");
        request.FirstName.Should().Be("First");
        request.LastName.Should().Be("Last");
        request.DisplayName.Should().Be("Display");
        request.PhoneNumber.Should().Be("+15551234567");
        request.Password.Should().Be("p@ss");
        request.PreferredLanguage.Should().Be("en");
        request.Timezone.Should().Be("UTC");
        request.OrganizationId.Should().Be("org-1");
        request.Roles.Should().BeEquivalentTo(roles);
        request.Metadata.Should().BeSameAs(metadata);
        request.SendVerificationEmail.Should().BeFalse();
    }

    [Fact]
    public void CreateUserRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new CreateUserRequest { Email = "u@example.com" };
        var second = new CreateUserRequest { Email = "u@example.com" };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void CreateUserRequest_With_ProducesUpdatedCopy()
    {
        // Arrange
        var original = new CreateUserRequest { Email = "u@example.com", Username = "u" };

        // Act
        var updated = original with { Username = "v" };

        // Assert
        original.Username.Should().Be("u");
        updated.Username.Should().Be("v");
        updated.Email.Should().Be("u@example.com");
    }
}
