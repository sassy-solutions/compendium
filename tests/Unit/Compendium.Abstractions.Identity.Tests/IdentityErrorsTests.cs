// -----------------------------------------------------------------------
// <copyright file="IdentityErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Tests;

public class IdentityErrorsTests
{
    [Fact]
    public void UserNotFound_WithUserId_ReturnsNotFoundErrorWithCodeAndMessage()
    {
        // Arrange
        const string userId = "user-42";

        // Act
        var error = IdentityErrors.UserNotFound(userId);

        // Assert
        error.Code.Should().Be("Identity.UserNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(userId);
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("")]
    [InlineData("a-very-long-user-id-1234567890")]
    public void UserNotFound_AnyUserId_AlwaysReturnsNotFoundErrorType(string userId)
    {
        // Act
        var error = IdentityErrors.UserNotFound(userId);

        // Assert
        error.Type.Should().Be(ErrorType.NotFound);
        error.Code.Should().Be("Identity.UserNotFound");
    }

    [Fact]
    public void UserNotFoundByEmail_WithEmail_ReturnsNotFoundErrorWithCodeAndMessage()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        var error = IdentityErrors.UserNotFoundByEmail(email);

        // Assert
        error.Code.Should().Be("Identity.UserNotFoundByEmail");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(email);
    }

    [Fact]
    public void UserAlreadyExists_WithEmail_ReturnsConflictErrorWithCodeAndMessage()
    {
        // Arrange
        const string email = "duplicate@example.com";

        // Act
        var error = IdentityErrors.UserAlreadyExists(email);

        // Assert
        error.Code.Should().Be("Identity.UserAlreadyExists");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(email);
    }

    [Fact]
    public void OrganizationNotFound_WithOrganizationId_ReturnsNotFoundErrorWithCodeAndMessage()
    {
        // Arrange
        const string organizationId = "org-77";

        // Act
        var error = IdentityErrors.OrganizationNotFound(organizationId);

        // Assert
        error.Code.Should().Be("Identity.OrganizationNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(organizationId);
    }

    [Fact]
    public void OrganizationNotFoundByName_WithName_ReturnsNotFoundErrorWithCodeAndMessage()
    {
        // Arrange
        const string name = "Acme Inc.";

        // Act
        var error = IdentityErrors.OrganizationNotFoundByName(name);

        // Assert
        error.Code.Should().Be("Identity.OrganizationNotFoundByName");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(name);
    }

    [Fact]
    public void OrganizationAlreadyExists_WithName_ReturnsConflictErrorWithCodeAndMessage()
    {
        // Arrange
        const string name = "Globex";

        // Act
        var error = IdentityErrors.OrganizationAlreadyExists(name);

        // Assert
        error.Code.Should().Be("Identity.OrganizationAlreadyExists");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(name);
    }

    [Fact]
    public void UserAlreadyMember_WithUserAndOrganization_ReturnsConflictErrorWithBothIds()
    {
        // Arrange
        const string userId = "user-1";
        const string organizationId = "org-1";

        // Act
        var error = IdentityErrors.UserAlreadyMember(userId, organizationId);

        // Assert
        error.Code.Should().Be("Identity.UserAlreadyMember");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain(userId);
        error.Message.Should().Contain(organizationId);
    }

    [Fact]
    public void UserNotMember_WithUserAndOrganization_ReturnsNotFoundErrorWithBothIds()
    {
        // Arrange
        const string userId = "user-1";
        const string organizationId = "org-1";

        // Act
        var error = IdentityErrors.UserNotMember(userId, organizationId);

        // Assert
        error.Code.Should().Be("Identity.UserNotMember");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain(userId);
        error.Message.Should().Contain(organizationId);
    }

    [Fact]
    public void InvalidToken_IsUnauthorizedErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.InvalidToken;

        // Assert
        error.Code.Should().Be("Identity.InvalidToken");
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TokenExpired_IsUnauthorizedErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.TokenExpired;

        // Assert
        error.Code.Should().Be("Identity.TokenExpired");
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Contain("expired");
    }

    [Fact]
    public void TokenRevoked_IsUnauthorizedErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.TokenRevoked;

        // Assert
        error.Code.Should().Be("Identity.TokenRevoked");
        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Contain("revoked");
    }

    [Fact]
    public void ProviderUnavailable_IsUnavailableErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.ProviderUnavailable;

        // Assert
        error.Code.Should().Be("Identity.ProviderUnavailable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RateLimitExceeded_IsTooManyRequestsErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.RateLimitExceeded;

        // Assert
        error.Code.Should().Be("Identity.RateLimitExceeded");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InvalidEmail_WithEmail_ReturnsValidationErrorWithCodeAndMessage()
    {
        // Arrange
        const string email = "not-an-email";

        // Act
        var error = IdentityErrors.InvalidEmail(email);

        // Assert
        error.Code.Should().Be("Identity.InvalidEmail");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(email);
    }

    [Fact]
    public void TenantContextRequired_IsValidationErrorWithExpectedCodeAndMessage()
    {
        // Act
        var error = IdentityErrors.TenantContextRequired;

        // Assert
        error.Code.Should().Be("Identity.TenantContextRequired");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("Tenant");
    }

    [Fact]
    public void StaticErrors_AreSingletonInstancesAcrossCalls()
    {
        // Act
        var first = IdentityErrors.InvalidToken;
        var second = IdentityErrors.InvalidToken;

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void AllFactoryErrors_HaveDistinctCodes()
    {
        // Arrange / Act
        var codes = new[]
        {
            IdentityErrors.UserNotFound("u").Code,
            IdentityErrors.UserNotFoundByEmail("e").Code,
            IdentityErrors.UserAlreadyExists("e").Code,
            IdentityErrors.OrganizationNotFound("o").Code,
            IdentityErrors.OrganizationNotFoundByName("o").Code,
            IdentityErrors.OrganizationAlreadyExists("o").Code,
            IdentityErrors.UserAlreadyMember("u", "o").Code,
            IdentityErrors.UserNotMember("u", "o").Code,
            IdentityErrors.InvalidToken.Code,
            IdentityErrors.TokenExpired.Code,
            IdentityErrors.TokenRevoked.Code,
            IdentityErrors.ProviderUnavailable.Code,
            IdentityErrors.RateLimitExceeded.Code,
            IdentityErrors.InvalidEmail("e").Code,
            IdentityErrors.TenantContextRequired.Code,
        };

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Errors_CanBeWrappedInResultFailure_PreservingErrorIdentity()
    {
        // Arrange
        var error = IdentityErrors.UserNotFound("u-1");

        // Act
        var result = Result.Failure<IdentityUser>(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Error.Code.Should().Be("Identity.UserNotFound");
    }
}
