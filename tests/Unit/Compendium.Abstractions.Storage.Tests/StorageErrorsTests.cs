// -----------------------------------------------------------------------
// <copyright file="StorageErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests;

public class StorageErrorsTests
{
    [Fact]
    public void Prefix_IsStorage()
    {
        // Act / Assert
        StorageErrors.Prefix.Should().Be("Storage");
    }

    [Fact]
    public void NotFound_ReturnsNotFoundErrorWithKey()
    {
        // Act
        var error = StorageErrors.NotFound("tenants/t-1/file.png");

        // Assert
        error.Code.Should().Be("Storage.NotFound");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain("tenants/t-1/file.png");
    }

    [Fact]
    public void AccessDenied_ReturnsForbiddenErrorWithKey()
    {
        // Act
        var error = StorageErrors.AccessDenied("private/secret.txt");

        // Assert
        error.Code.Should().Be("Storage.AccessDenied");
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Contain("private/secret.txt");
    }

    [Fact]
    public void InvalidBucket_ReturnsValidationErrorWithBucket()
    {
        // Act
        var error = StorageErrors.InvalidBucket("BadBucketName");

        // Assert
        error.Code.Should().Be("Storage.InvalidBucket");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("BadBucketName");
    }

    [Fact]
    public void Throttled_WithoutRetryAfter_ReturnsTooManyRequestsGenericMessage()
    {
        // Act
        var error = StorageErrors.Throttled();

        // Assert
        error.Code.Should().Be("Storage.Throttled");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("throttled");
    }

    [Fact]
    public void Throttled_WithRetryAfter_IncludesSeconds()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = StorageErrors.Throttled(retryAfter);

        // Assert
        error.Code.Should().Be("Storage.Throttled");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("45");
    }

    [Fact]
    public void ContentTooLarge_ReturnsValidationErrorWithSizes()
    {
        // Act
        var error = StorageErrors.ContentTooLarge(size: 5_000_000, maximum: 1_000_000);

        // Assert
        error.Code.Should().Be("Storage.ContentTooLarge");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("5000000");
        error.Message.Should().Contain("1000000");
    }

    [Fact]
    public void ConflictExists_ReturnsConflictErrorWithKey()
    {
        // Act
        var error = StorageErrors.ConflictExists("tenants/t-1/file.png");

        // Assert
        error.Code.Should().Be("Storage.ConflictExists");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Contain("tenants/t-1/file.png");
    }
}
