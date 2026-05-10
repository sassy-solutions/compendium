// -----------------------------------------------------------------------
// <copyright file="ResultGenericTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Results;

/// <summary>
/// Coverage for the generic <see cref="Result{TValue}"/> branches not exercised by ResultTests:
/// implicit operators (value → success, error → failure) and the override <c>ToString</c> path.
/// </summary>
public class ResultGenericTests
{
    [Fact]
    public void ImplicitOperator_FromValue_ReturnsSuccessResult()
    {
        // Arrange
        const string value = "hello";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitOperator_FromError_ReturnsFailureResult()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation("ERR.X", "boom");

        // Act
        Result<string> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ToString_OnSuccess_IncludesValue()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().StartWith("Success:");
        str.Should().Contain("42");
    }

    [Fact]
    public void ToString_OnFailure_IncludesErrorMessage()
    {
        // Arrange
        var error = Error.Validation("VAL.42", "bad input");
        var result = Result.Failure<int>(error);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().StartWith("Failure:");
        str.Should().Contain("bad input");
    }

    [Fact]
    public void ToString_OnSuccess_WithNullableNullValue_DoesNotThrow()
    {
        // Arrange
        var result = Result.Success<string?>(null);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().StartWith("Success:");
    }
}
