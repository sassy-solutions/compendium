// -----------------------------------------------------------------------
// <copyright file="ValidationBehaviorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using Compendium.Application.CQRS.Behaviors;

namespace Compendium.Application.Tests.CQRS.Behaviors;

/// <summary>
/// Unit tests for the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class ValidationBehaviorTests
{
    public sealed class ValidatableRequest
    {
        [Required]
        public string? Name { get; init; }

        [Range(1, 100)]
        public int Quantity { get; init; }
    }

    public sealed class AnyResponse
    {
    }

    [Fact]
    public async Task HandleAsync_WhenValid_CallsNextAndReturnsResponse()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidatableRequest, AnyResponse>();
        var expected = new AnyResponse();

        // Act
        var actual = await behavior.HandleAsync(
            new ValidatableRequest { Name = "ok", Quantity = 5 },
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidAndResponseIsResult_ReturnsValidationFailure()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidatableRequest, Result>();
        var nextCalled = false;

        // Act
        var actual = await behavior.HandleAsync(
            new ValidatableRequest { Name = null, Quantity = 0 },
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        // Assert
        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("Validation.Failed");
        actual.Error.Type.Should().Be(ErrorType.Validation);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidAndResponseIsGenericResult_ReturnsFailureAndShortCircuits()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidatableRequest, Result<int>>();
        var nextCalled = false;

        // Act
        var actual = await behavior.HandleAsync(
            new ValidatableRequest { Name = null, Quantity = 0 },
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success(42));
            },
            CancellationToken.None);

        // Assert
        actual.Should().NotBeNull();
        actual.IsFailure.Should().BeTrue();
        actual.Error.Code.Should().Be("Validation.Failed");
        actual.Error.Type.Should().Be(ErrorType.Validation);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidAndResponseIsArbitraryClass_StillCallsNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidatableRequest, AnyResponse>();
        var expected = new AnyResponse();

        // Act
        var actual = await behavior.HandleAsync(
            new ValidatableRequest { Name = null, Quantity = 0 },
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        // Validation only short-circuits for Result / Result<T>; for arbitrary
        // responses the validation result is dropped and the pipeline continues.
        actual.Should().Be(expected);
    }
}
