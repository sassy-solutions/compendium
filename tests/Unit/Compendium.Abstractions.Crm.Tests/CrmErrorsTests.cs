// -----------------------------------------------------------------------
// <copyright file="CrmErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm.Tests;

public class CrmErrorsTests
{
    [Fact]
    public void Prefix_IsCrm()
    {
        // Act / Assert
        CrmErrors.Prefix.Should().Be("Crm");
    }

    [Fact]
    public void ContactNotFound_ShouldReturnNotFoundError()
    {
        // Act
        var error = CrmErrors.ContactNotFound("ext-1");

        // Assert
        error.Code.Should().Be("Crm.ContactNotFound");
        error.Message.Should().Contain("ext-1");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void DuplicateContact_ShouldReturnConflictError()
    {
        // Act
        var error = CrmErrors.DuplicateContact("user@example.com");

        // Assert
        error.Code.Should().Be("Crm.DuplicateContact");
        error.Message.Should().Contain("user@example.com");
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void InvalidEmail_ShouldReturnValidationError()
    {
        // Act
        var error = CrmErrors.InvalidEmail("not-an-email");

        // Assert
        error.Code.Should().Be("Crm.InvalidEmail");
        error.Message.Should().Contain("not-an-email");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void ProviderUnreachable_ShouldReturnFailureError()
    {
        // Act
        var error = CrmErrors.ProviderUnreachable("hubspot");

        // Assert
        error.Code.Should().Be("Crm.ProviderUnreachable");
        error.Message.Should().Contain("hubspot");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ShouldReturnGenericMessage()
    {
        // Act
        var error = CrmErrors.RateLimited();

        // Assert
        error.Code.Should().Be("Crm.RateLimited");
        error.Message.Should().Contain("Rate limit exceeded");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void RateLimited_WithRetryAfter_ShouldIncludeRetryTime()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = CrmErrors.RateLimited(retryAfter);

        // Assert
        error.Code.Should().Be("Crm.RateLimited");
        error.Message.Should().Contain("45");
    }
}
