// -----------------------------------------------------------------------
// <copyright file="AuthorizationErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization.Tests;

public class AuthorizationErrorsTests
{
    [Fact]
    public void NotAuthorized_WithSubjectRelationObject_ReturnsForbiddenError()
    {
        // Arrange
        const string subject = "user:1";
        const string relation = "viewer";
        const string @object = "doc:1";

        // Act
        var error = AuthorizationErrors.NotAuthorized(subject, relation, @object);

        // Assert
        error.Code.Should().Be("Authorization.NotAuthorized");
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Contain(subject);
        error.Message.Should().Contain(relation);
        error.Message.Should().Contain(@object);
    }

    [Theory]
    [InlineData("user:1", "viewer", "doc:1")]
    [InlineData("group:eng#member", "editor", "folder:specs")]
    [InlineData("user:*", "owner", "doc:public")]
    public void NotAuthorized_EmbedsAllThreeArgumentsInMessage(string subject, string relation, string @object)
    {
        // Act
        var error = AuthorizationErrors.NotAuthorized(subject, relation, @object);

        // Assert
        error.Code.Should().Be("Authorization.NotAuthorized");
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Contain(subject);
        error.Message.Should().Contain(relation);
        error.Message.Should().Contain(@object);
    }

    [Fact]
    public void InvalidTuple_WithReason_ReturnsValidationError()
    {
        // Arrange
        const string reason = "subject cannot be empty";

        // Act
        var error = AuthorizationErrors.InvalidTuple(reason);

        // Assert
        error.Code.Should().Be("Authorization.InvalidTuple");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void StoreNotFound_WithTenantId_ReturnsNotFoundError()
    {
        // Arrange
        const string tenantId = "tenant-xyz";

        // Act
        var error = AuthorizationErrors.StoreNotFound(tenantId);

        // Assert
        error.Code.Should().Be("Authorization.StoreNotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain($"'{tenantId}'");
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ReturnsUnavailableError()
    {
        // Arrange
        const string reason = "connection timeout";

        // Act
        var error = AuthorizationErrors.ProviderUnreachable(reason);

        // Assert
        error.Code.Should().Be("Authorization.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Unavailable);
        error.Message.Should().Contain(reason);
    }

    [Fact]
    public void AllErrorFactories_ReturnNonNullErrorInstances()
    {
        // Act / Assert
        AuthorizationErrors.NotAuthorized("s", "r", "o").Should().NotBeNull();
        AuthorizationErrors.InvalidTuple("r").Should().NotBeNull();
        AuthorizationErrors.StoreNotFound("t").Should().NotBeNull();
        AuthorizationErrors.ProviderUnreachable("r").Should().NotBeNull();
    }

    [Fact]
    public void AllErrorCodes_StartWithAuthorizationPrefix()
    {
        // Act
        var codes = new[]
        {
            AuthorizationErrors.NotAuthorized("s", "r", "o").Code,
            AuthorizationErrors.InvalidTuple("r").Code,
            AuthorizationErrors.StoreNotFound("t").Code,
            AuthorizationErrors.ProviderUnreachable("r").Code,
        };

        // Assert
        codes.Should().OnlyContain(c => c.StartsWith("Authorization.", StringComparison.Ordinal));
    }
}
