// -----------------------------------------------------------------------
// <copyright file="CreateOrganizationRequestTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests.Models.Requests;

public class CreateOrganizationRequestTests
{
    [Fact]
    public void CreateOrganizationRequest_WithRequiredName_InitializesSuccessfully()
    {
        // Arrange / Act
        var request = new CreateOrganizationRequest { Name = "Acme" };

        // Assert
        request.Name.Should().Be("Acme");
        request.Domain.Should().BeNull();
        request.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateOrganizationRequest_FullyPopulated_RoundTripsAllProperties()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["plan"] = "pro" };

        // Act
        var request = new CreateOrganizationRequest
        {
            Name = "Acme",
            Domain = "acme.com",
            Metadata = metadata
        };

        // Assert
        request.Name.Should().Be("Acme");
        request.Domain.Should().Be("acme.com");
        request.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void CreateOrganizationRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var first = new CreateOrganizationRequest { Name = "Acme", Domain = "acme.com" };
        var second = new CreateOrganizationRequest { Name = "Acme", Domain = "acme.com" };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void CreateOrganizationRequest_With_ProducesUpdatedCopy()
    {
        // Arrange
        var original = new CreateOrganizationRequest { Name = "Acme" };

        // Act
        var updated = original with { Name = "Globex" };

        // Assert
        original.Name.Should().Be("Acme");
        updated.Name.Should().Be("Globex");
    }
}
