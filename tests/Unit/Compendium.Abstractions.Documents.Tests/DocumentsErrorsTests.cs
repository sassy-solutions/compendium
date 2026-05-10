// -----------------------------------------------------------------------
// <copyright file="DocumentsErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests;

public sealed class DocumentsErrorsTests
{
    [Fact]
    public void Prefix_IsDocuments()
    {
        // Assert
        DocumentsErrors.Prefix.Should().Be("Documents");
    }

    [Fact]
    public void UnsupportedFormat_ShouldReturnValidationErrorWithMime()
    {
        // Act
        var error = DocumentsErrors.UnsupportedFormat("application/x-bogus");

        // Assert
        error.Code.Should().Be("Documents.UnsupportedFormat");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("application/x-bogus");
    }

    [Fact]
    public void DocumentTooLarge_ShouldReturnValidationErrorWithSizes()
    {
        // Act
        var error = DocumentsErrors.DocumentTooLarge(20_000_000, 10_485_760);

        // Assert
        error.Code.Should().Be("Documents.DocumentTooLarge");
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("20000000");
        error.Message.Should().Contain("10485760");
    }

    [Fact]
    public void ProviderUnreachable_WithoutReason_ShouldUseGenericMessage()
    {
        // Act
        var error = DocumentsErrors.ProviderUnreachable("azure-document-intelligence");

        // Assert
        error.Code.Should().Be("Documents.ProviderUnreachable");
        error.Type.Should().Be(ErrorType.Failure);
        error.Message.Should().Contain("azure-document-intelligence");
        error.Message.Should().NotContain(":");
    }

    [Fact]
    public void ProviderUnreachable_WithReason_ShouldIncludeReason()
    {
        // Act
        var error = DocumentsErrors.ProviderUnreachable("aws-textract", "DNS lookup failed");

        // Assert
        error.Code.Should().Be("Documents.ProviderUnreachable");
        error.Message.Should().Contain("aws-textract");
        error.Message.Should().Contain("DNS lookup failed");
    }

    [Fact]
    public void RateLimited_WithoutRetryAfter_ShouldReturnGenericMessage()
    {
        // Act
        var error = DocumentsErrors.RateLimited();

        // Assert
        error.Code.Should().Be("Documents.RateLimited");
        error.Type.Should().Be(ErrorType.Failure);
        error.Message.Should().Contain("rate limit");
    }

    [Fact]
    public void RateLimited_WithRetryAfter_ShouldIncludeSeconds()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = DocumentsErrors.RateLimited(retryAfter);

        // Assert
        error.Code.Should().Be("Documents.RateLimited");
        error.Message.Should().Contain("45");
    }
}
