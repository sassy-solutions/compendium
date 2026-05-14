// -----------------------------------------------------------------------
// <copyright file="CircuitBreakerRegistryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="CircuitBreakerRegistry"/> covering caching and DI failure modes.
/// </summary>
public sealed class CircuitBreakerRegistryTests
{
    [Fact]
    public void Ctor_NullProvider_Throws()
    {
        // Arrange / Act
        var act = () => new CircuitBreakerRegistry(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("serviceProvider");
    }

    [Fact]
    public void GetOrCreate_FirstCall_CreatesNewBreaker()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var breaker = sut.GetOrCreate("redis");

        // Assert
        breaker.Should().NotBeNull();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void GetOrCreate_SecondCall_ReturnsCachedInstance()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var first = sut.GetOrCreate("redis");
        var second = sut.GetOrCreate("redis");

        // Assert
        second.Should().BeSameAs(first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetOrCreate_EmptyName_Throws(string invalid)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.GetOrCreate(invalid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void GetOrCreate_DifferentNames_ReturnDifferentInstances()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var redis = sut.GetOrCreate("redis");
        var postgres = sut.GetOrCreate("postgres");

        // Assert
        redis.Should().NotBeSameAs(postgres);
    }

    [Fact]
    public void GetOrCreate_WithCustomOptions_UsesOptions()
    {
        // Arrange
        var sut = CreateSut();
        var options = new CircuitBreakerOptions { FailureThreshold = 99 };

        // Act
        var breaker = sut.GetOrCreate("custom", options);

        // Assert
        breaker.Should().NotBeNull();
    }

    [Fact]
    public void GetOrCreate_LoggerNotRegistered_ThrowsInvalidOperation()
    {
        // Arrange
        var emptyProvider = new ServiceCollection().BuildServiceProvider();
        var sut = new CircuitBreakerRegistry(emptyProvider);

        // Act
        var act = () => sut.GetOrCreate("any");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Logger<CircuitBreaker>*");
    }

    [Fact]
    public void CircuitBreakerOptions_Defaults_AreSensible()
    {
        // Arrange / Act
        var options = new CircuitBreakerOptions();

        // Assert
        options.FailureThreshold.Should().Be(5);
        options.OpenTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.ShouldTripOnError.Should().NotBeNull();
        options.ShouldTripOnException.Should().NotBeNull();
    }

    [Fact]
    public void CircuitBreakerOptions_DefaultShouldTripOnError_TripsOnFailure()
    {
        // Arrange
        var options = new CircuitBreakerOptions();

        // Act / Assert
        options.ShouldTripOnError(Error.Failure("x", "y")).Should().BeTrue();
        options.ShouldTripOnError(Error.Validation("v", "v")).Should().BeFalse();
        options.ShouldTripOnError(Error.NotFound("n", "n")).Should().BeFalse();
    }

    [Fact]
    public void CircuitBreakerOptions_DefaultShouldTripOnException_DoesNotTripOnArgument()
    {
        // Arrange
        var options = new CircuitBreakerOptions();

        // Act / Assert
        options.ShouldTripOnException(new ArgumentException("x")).Should().BeFalse();
        options.ShouldTripOnException(new ArgumentNullException("x")).Should().BeFalse();
        options.ShouldTripOnException(new InvalidOperationException("x")).Should().BeFalse();
        options.ShouldTripOnException(new TimeoutException("x")).Should().BeTrue();
    }

    private static CircuitBreakerRegistry CreateSut()
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLogger<CircuitBreaker>.Instance);
        services.AddSingleton<ILogger<CircuitBreaker>>(sp => NullLogger<CircuitBreaker>.Instance);
        var provider = services.BuildServiceProvider();
        return new CircuitBreakerRegistry(provider);
    }
}
