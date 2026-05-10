// -----------------------------------------------------------------------
// <copyright file="SearchErrorsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests;

public class SearchErrorsTests
{
    [Fact]
    public void Prefix_IsSearch()
    {
        // Assert
        SearchErrors.Prefix.Should().Be("Search");
    }

    [Fact]
    public void IndexNotFound_ReturnsNotFoundErrorWithIndexName()
    {
        // Act
        var error = SearchErrors.IndexNotFound("products");

        // Assert
        error.Code.Should().Be("Search.IndexNotFound");
        error.Message.Should().Contain("products");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void InvalidQuery_ReturnsValidationErrorWithReason()
    {
        // Act
        var error = SearchErrors.InvalidQuery("missing closing brace");

        // Assert
        error.Code.Should().Be("Search.InvalidQuery");
        error.Message.Should().Contain("missing closing brace");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void AttributeNotFilterable_ReturnsValidationErrorWithAttribute()
    {
        // Act
        var error = SearchErrors.AttributeNotFilterable("price");

        // Assert
        error.Code.Should().Be("Search.AttributeNotFilterable");
        error.Message.Should().Contain("price");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void AttributeNotSortable_ReturnsValidationErrorWithAttribute()
    {
        // Act
        var error = SearchErrors.AttributeNotSortable("created_at");

        // Assert
        error.Code.Should().Be("Search.AttributeNotSortable");
        error.Message.Should().Contain("created_at");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Throttled_WithoutRetryAfter_ReturnsTooManyRequestsWithGenericMessage()
    {
        // Act
        var error = SearchErrors.Throttled();

        // Assert
        error.Code.Should().Be("Search.Throttled");
        error.Message.Should().Contain("throttled");
        error.Type.Should().Be(ErrorType.TooManyRequests);
    }

    [Fact]
    public void Throttled_WithRetryAfter_IncludesRetrySeconds()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var error = SearchErrors.Throttled(retryAfter);

        // Assert
        error.Code.Should().Be("Search.Throttled");
        error.Message.Should().Contain("45");
        error.Type.Should().Be(ErrorType.TooManyRequests);
    }
}
