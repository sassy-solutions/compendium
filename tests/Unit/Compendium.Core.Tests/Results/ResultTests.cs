// -----------------------------------------------------------------------
// <copyright file="ResultTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Success_WithValue_CreatesSuccessResult()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithValueType_CreatesFailureResult()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_OnSuccessResult_ReturnsValue()
    {
        // Arrange
        var expectedValue = "test value";
        var result = Result.Success(expectedValue);

        // Act
        var actualValue = result.Value;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void Value_OnFailureResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var result = Result.Failure<string>(error);

        // Act
        var act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot access the value of a failed result.");
    }

    [Fact]
    public void Create_WithTrueCondition_ReturnsSuccess()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(true, error);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFalseCondition_ReturnsFailure()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(false, error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Create_WithNonNullValue_ReturnsSuccess()
    {
        // Arrange
        var value = "test value";
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(value, error);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNullValue_ReturnsFailure()
    {
        // Arrange
        string? value = null;
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(value, error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Create_WithNullableValueType_HasValue_ReturnsSuccess()
    {
        // Arrange
        int? value = 42;
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(value, error);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Create_WithNullableValueType_NoValue_ReturnsFailure()
    {
        // Arrange
        int? value = null;
        var error = TestData.Errors.CreateValidation();

        // Act
        var result = Result.Create(value, error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Combine_AllSuccessResults_ReturnsSuccess()
    {
        // Arrange
        var results = new[]
        {
            Result.Success(),
            Result.Success(),
            Result.Success()
        };

        // Act
        var combined = Result.Combine(results);

        // Assert
        combined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_WithFailureResult_ReturnsFirstFailure()
    {
        // Arrange
        var error1 = TestData.Errors.CreateValidation("ERR001", "First error");
        var error2 = TestData.Errors.CreateValidation("ERR002", "Second error");

        var results = new[]
        {
            Result.Success(),
            Result.Failure(error1),
            Result.Failure(error2)
        };

        // Act
        var combined = Result.Combine(results);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Error.Should().Be(error1);
    }

    [Fact]
    public void Combine_EmptyResults_ReturnsSuccess()
    {
        // Arrange
        var results = Array.Empty<Result>();

        // Act
        var combined = Result.Combine(results);

        // Assert
        combined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_WithEnumerable_AllSuccess_ReturnsSuccess()
    {
        // Arrange
        var results = Enumerable.Range(0, 5).Select(_ => Result.Success());

        // Act
        var combined = Result.Combine(results);

        // Assert
        combined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_WithEnumerable_WithFailure_ReturnsFirstFailure()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var results = new List<Result>
        {
            Result.Success(),
            Result.Success(),
            Result.Failure(error),
            Result.Success()
        };

        // Act
        var combined = Result.Combine(results);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Error.Should().Be(error);
    }

    [Fact]
    public void Combine_WithNullResults_ThrowsArgumentNullException()
    {
        // Act
        var act = () => Result.Combine((Result[])null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Combine_WithNullEnumerable_ThrowsArgumentNullException()
    {
        // Act
        var act = () => Result.Combine((IEnumerable<Result>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImplicitOperator_FromError_CreatesFailureResult()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act
        Result result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ToString_SuccessResult_ReturnsSuccessString()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().Be("Success");
    }

    [Fact]
    public void ToString_FailureResult_ReturnsFailureString()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation("ERR001", "Test error");
        var result = Result.Failure(error);

        // Act
        var stringResult = result.ToString();

        // Assert
        stringResult.Should().StartWith("Failure:");
        stringResult.Should().Contain("Test error");
    }

    [Fact]
    public void Constructor_SuccessWithError_ThrowsInvalidOperationException()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();

        // Act & Assert
        var act = () => new TestableResult(true, error);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("A successful result cannot have an error.");
    }

    [Fact]
    public void Constructor_FailureWithoutError_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var act = () => new TestableResult(false, Error.None);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("A failed result must have an error.");
    }

    [Theory]
    [InlineData(1000)]
    public void Result_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = Result.Success();
            _ = Result.Success("value");
            _ = Result.Failure(error);
            _ = Result.Failure<string>(error);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Result creation should be fast");
    }

    [Fact]
    public void ConcurrentAccess_ResultCreation_ThreadSafe()
    {
        // Arrange
        var error = TestData.Errors.CreateValidation();
        var results = new List<Result>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var result = i % 2 == 0 ? Result.Success() : Result.Failure(error);
            lock (lockObject)
            {
                results.Add(result);
            }
        });

        // Assert
        results.Should().HaveCount(100);
        results.Count(r => r.IsSuccess).Should().Be(50);
        results.Count(r => r.IsFailure).Should().Be(50);
    }

    // Helper class to test protected constructor
    private class TestableResult : Result
    {
        public TestableResult(bool isSuccess, Error error) : base(isSuccess, error)
        {
        }
    }
}
