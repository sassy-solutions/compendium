// -----------------------------------------------------------------------
// <copyright file="DeduplicationStrategyTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Application.Idempotency;

namespace Compendium.Application.Tests.Idempotency;

/// <summary>
/// Unit tests for <see cref="HashBasedDeduplicationStrategy"/> and
/// <see cref="PropertyBasedDeduplicationStrategy"/>.
/// </summary>
public class DeduplicationStrategyTests
{
    public sealed class FakeRequest
    {
        public string? Name { get; init; }

        public int Quantity { get; init; }
    }

    [Fact]
    public void HashBased_GenerateKey_WithIdenticalInputs_ProducesIdenticalKeys()
    {
        // Arrange
        var strategy = new HashBasedDeduplicationStrategy();
        var a = new FakeRequest { Name = "a", Quantity = 1 };
        var b = new FakeRequest { Name = "a", Quantity = 1 };

        // Act
        var keyA = strategy.GenerateKey(a);
        var keyB = strategy.GenerateKey(b);

        // Assert
        keyA.Should().Be(keyB);
        keyA.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashBased_GenerateKey_WithDifferentInputs_ProducesDifferentKeys()
    {
        // Arrange
        var strategy = new HashBasedDeduplicationStrategy();

        // Act
        var keyA = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });
        var keyB = strategy.GenerateKey(new FakeRequest { Name = "b", Quantity = 2 });

        // Assert
        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public void HashBased_GenerateKey_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new HashBasedDeduplicationStrategy();

        // Act
        var act = () => strategy.GenerateKey<FakeRequest>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void HashBased_GenerateKey_WithCustomJsonOptions_UsesProvidedOptions()
    {
        // Arrange
        var custom = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var strategy = new HashBasedDeduplicationStrategy(custom);

        // Act
        var key = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });

        // Assert — Default options use camelCase; custom uses Pascal. Different keys.
        var defaultKey = new HashBasedDeduplicationStrategy().GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });
        key.Should().NotBe(defaultKey);
    }

    [Fact]
    public void PropertyBased_Constructor_WhenPropertyNamesIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new PropertyBasedDeduplicationStrategy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("propertyNames");
    }

    [Fact]
    public void PropertyBased_Constructor_WhenPropertyNamesIsEmpty_ThrowsArgumentException()
    {
        // Arrange / Act
        var act = () => new PropertyBasedDeduplicationStrategy(Array.Empty<string>());

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("propertyNames");
    }

    [Fact]
    public void PropertyBased_GenerateKey_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new PropertyBasedDeduplicationStrategy("Name");

        // Act
        var act = () => strategy.GenerateKey<FakeRequest>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void PropertyBased_GenerateKey_WhenPropertyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = new PropertyBasedDeduplicationStrategy("DoesNotExist");

        // Act
        var act = () => strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*DoesNotExist*");
    }

    [Fact]
    public void PropertyBased_GenerateKey_WithSameValues_ProducesIdenticalKeys()
    {
        // Arrange
        var strategy = new PropertyBasedDeduplicationStrategy("Name", "Quantity");

        // Act
        var keyA = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });
        var keyB = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });

        // Assert
        keyA.Should().Be(keyB);
    }

    [Fact]
    public void PropertyBased_GenerateKey_OnlyConsidersSelectedProperties()
    {
        // Arrange — only Name is part of the key; Quantity differing should not change the result.
        var strategy = new PropertyBasedDeduplicationStrategy("Name");

        // Act
        var keyA = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 1 });
        var keyB = strategy.GenerateKey(new FakeRequest { Name = "a", Quantity = 99 });

        // Assert
        keyA.Should().Be(keyB);
    }

    [Fact]
    public void PropertyBased_GenerateKey_WithNullPropertyValue_ProducesStableKey()
    {
        // Arrange
        var strategy = new PropertyBasedDeduplicationStrategy("Name");

        // Act
        var key = strategy.GenerateKey(new FakeRequest { Name = null, Quantity = 0 });

        // Assert — should not throw and should be deterministic.
        key.Should().NotBeNullOrEmpty();
        var keyAgain = strategy.GenerateKey(new FakeRequest { Name = null, Quantity = 0 });
        key.Should().Be(keyAgain);
    }
}
