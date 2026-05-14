// -----------------------------------------------------------------------
// <copyright file="UpdateUserRequestTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models.Requests;

public class UpdateUserRequestTests
{
    [Fact]
    public void UpdateUserRequest_DefaultConstruction_AllPropertiesAreNull()
    {
        // Arrange / Act
        var request = new UpdateUserRequest();

        // Assert
        request.Username.Should().BeNull();
        request.FirstName.Should().BeNull();
        request.LastName.Should().BeNull();
        request.DisplayName.Should().BeNull();
        request.PhoneNumber.Should().BeNull();
        request.PreferredLanguage.Should().BeNull();
        request.Timezone.Should().BeNull();
        request.ProfilePictureUrl.Should().BeNull();
        request.Roles.Should().BeNull();
        request.Metadata.Should().BeNull();
    }

    [Fact]
    public void UpdateUserRequest_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var roles = new[] { "user" };
        var metadata = new Dictionary<string, object> { ["k"] = "v" };

        // Act
        var request = new UpdateUserRequest
        {
            Username = "u",
            FirstName = "F",
            LastName = "L",
            DisplayName = "D",
            PhoneNumber = "+15551112222",
            PreferredLanguage = "fr",
            Timezone = "Europe/Paris",
            ProfilePictureUrl = "https://x/pic.png",
            Roles = roles,
            Metadata = metadata
        };

        // Assert
        request.Username.Should().Be("u");
        request.FirstName.Should().Be("F");
        request.LastName.Should().Be("L");
        request.DisplayName.Should().Be("D");
        request.PhoneNumber.Should().Be("+15551112222");
        request.PreferredLanguage.Should().Be("fr");
        request.Timezone.Should().Be("Europe/Paris");
        request.ProfilePictureUrl.Should().Be("https://x/pic.png");
        request.Roles.Should().BeEquivalentTo(roles);
        request.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void UpdateUserRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new UpdateUserRequest { Username = "u", FirstName = "F" };
        var second = new UpdateUserRequest { Username = "u", FirstName = "F" };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void UpdateUserRequest_With_ProducesUpdatedCopy()
    {
        // Arrange
        var original = new UpdateUserRequest { FirstName = "Old" };

        // Act
        var updated = original with { FirstName = "New" };

        // Assert
        original.FirstName.Should().Be("Old");
        updated.FirstName.Should().Be("New");
    }
}
