// -----------------------------------------------------------------------
// <copyright file="VectorStoreErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Tests;

public class VectorStoreErrorsTests
{
    [Fact]
    public void Prefix_IsVectorStore()
    {
        // Assert
        VectorStoreErrors.Prefix.Should().Be("VectorStore");
    }

    [Fact]
    public void CollectionNotFound_ReturnsNotFoundError()
    {
        // Act
        var error = VectorStoreErrors.CollectionNotFound("docs");

        // Assert
        error.Code.Should().Be("VectorStore.CollectionNotFound");
        error.Message.Should().Contain("docs");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void DimensionMismatch_ReturnsValidationError()
    {
        // Act
        var error = VectorStoreErrors.DimensionMismatch(1536, 768);

        // Assert
        error.Code.Should().Be("VectorStore.DimensionMismatch");
        error.Message.Should().Contain("1536");
        error.Message.Should().Contain("768");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void IdNotFound_ReturnsNotFoundError()
    {
        // Act
        var error = VectorStoreErrors.IdNotFound("doc-42");

        // Assert
        error.Code.Should().Be("VectorStore.IdNotFound");
        error.Message.Should().Contain("doc-42");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void InvalidFilter_ReturnsValidationError()
    {
        // Act
        var error = VectorStoreErrors.InvalidFilter("unknown operator");

        // Assert
        error.Code.Should().Be("VectorStore.InvalidFilter");
        error.Message.Should().Contain("unknown operator");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Throttled_WithoutRetryAfter_ReturnsTooManyRequestsErrorWithGenericMessage()
    {
        // Act
        var error = VectorStoreErrors.Throttled();

        // Assert
        error.Code.Should().Be("VectorStore.Throttled");
        error.Type.Should().Be(ErrorType.TooManyRequests);
        error.Message.Should().Contain("throttled");
    }

    [Fact]
    public void Throttled_WithRetryAfter_IncludesRetrySeconds()
    {
        // Act
        var error = VectorStoreErrors.Throttled(TimeSpan.FromSeconds(45));

        // Assert
        error.Code.Should().Be("VectorStore.Throttled");
        error.Message.Should().Contain("45");
    }
}
