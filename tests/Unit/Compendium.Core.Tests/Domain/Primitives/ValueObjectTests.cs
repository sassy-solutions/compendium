// -----------------------------------------------------------------------
// <copyright file="ValueObjectTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Primitives;

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act
        var result = vo1.Equals(vo2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStringComponent_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Value1", 123);
        var vo2 = new TestValueObject("Value2", 123);

        // Act
        var result = vo1.Equals(vo2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentIntComponent_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 456);

        // Act
        var result = vo1.Equals(vo2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullValueObject_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Value", 123);

        // Act
        var result = vo.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var vo = new TestValueObject("Value", 123);

        // Act
        var result = vo.Equals(vo);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var vo = new TestValueObject("Value", 123);
        var other = new object();

        // Act
        var result = vo.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNullComponent_HandlesCorrectly()
    {
        // Arrange
        var vo1 = new TestValueObject(null, 123);
        var vo2 = new TestValueObject(null, 123);

        // Act
        var result = vo1.Equals(vo2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_OneNullOneNotNull_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject(null, 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act
        var result = vo1.Equals(vo2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameComponents_ReturnsSameHash()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act
        var hash1 = vo1.GetHashCode();
        var hash2 = vo2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentComponents_ReturnsDifferentHash()
    {
        // Arrange
        var vo1 = new TestValueObject("Value1", 123);
        var vo2 = new TestValueObject("Value2", 123);

        // Act
        var hash1 = vo1.GetHashCode();
        var hash2 = vo2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_WithNullComponent_DoesNotThrow()
    {
        // Arrange
        var vo = new TestValueObject(null, 123);

        // Act
        var act = () => vo.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var vo = new TestValueObject("Value", 123);

        // Act
        var hash1 = vo.GetHashCode();
        var hash2 = vo.GetHashCode();
        var hash3 = vo.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    [Fact]
    public void EqualityOperator_SameComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act
        var result = vo1 == vo2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentComponents_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Value1", 123);
        var vo2 = new TestValueObject("Value2", 123);

        // Act
        var result = vo1 == vo2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        TestValueObject? vo1 = null;
        TestValueObject? vo2 = null;

        // Act
        var result = vo1 == vo2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        TestValueObject? vo2 = null;

        // Act
        var result = vo1 == vo2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentComponents_ReturnsTrue()
    {
        // Arrange
        var vo1 = new TestValueObject("Value1", 123);
        var vo2 = new TestValueObject("Value2", 123);

        // Act
        var result = vo1 != vo2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_SameComponents_ReturnsFalse()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act
        var result = vo1 != vo2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var vo = new TestValueObject("TestValue", 42);

        // Act
        var result = vo.ToString();

        // Assert
        result.Should().Contain("TestValueObject");
        result.Should().Contain("TestValue");
        result.Should().Contain("42");
    }

    [Fact]
    public void ToString_WithNullComponent_HandlesGracefully()
    {
        // Arrange
        var vo = new TestValueObject(null, 42);

        // Act
        var result = vo.ToString();

        // Assert
        result.Should().Contain("TestValueObject");
        result.Should().Contain("42");
        result.Should().NotContain("null"); // Null components should be filtered out
    }

    [Fact]
    public void ToString_WithEmptyComponents_ReturnsTypeNameOnly()
    {
        // Arrange
        var vo = new EmptyValueObject();

        // Act
        var result = vo.ToString();

        // Assert
        result.Should().Be("EmptyValueObject()");
    }

    [Theory]
    [InlineData("Value1", 1)]
    [InlineData("Value2", 2)]
    [InlineData("", 0)]
    [InlineData("LongValue", int.MaxValue)]
    public void Equals_VariousValues_WorksCorrectly(string stringValue, int intValue)
    {
        // Arrange
        var vo1 = new TestValueObject(stringValue, intValue);
        var vo2 = new TestValueObject(stringValue, intValue);
        var vo3 = new TestValueObject(stringValue + "Different", intValue);

        // Act & Assert
        vo1.Should().Be(vo2);
        vo1.Should().NotBe(vo3);
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
    }

    [Fact]
    public void ValueObjectEquality_IsReflexive()
    {
        // Arrange
        var vo = new TestValueObject("Value", 123);

        // Act & Assert
        vo.Should().Be(vo); // Reflexive: x.Equals(x) should be true
    }

    [Fact]
    public void ValueObjectEquality_IsSymmetric()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);

        // Act & Assert
        vo1.Should().Be(vo2); // Symmetric: if x.Equals(y), then y.Equals(x)
        vo2.Should().Be(vo1);
    }

    [Fact]
    public void ValueObjectEquality_IsTransitive()
    {
        // Arrange
        var vo1 = new TestValueObject("Value", 123);
        var vo2 = new TestValueObject("Value", 123);
        var vo3 = new TestValueObject("Value", 123);

        // Act & Assert
        vo1.Should().Be(vo2);
        vo2.Should().Be(vo3);
        vo1.Should().Be(vo3); // Transitive: if x.Equals(y) and y.Equals(z), then x.Equals(z)
    }

    [Theory]
    [InlineData(1000)]
    public void GetHashCode_PerformanceTest_CompletesQuickly(int iterations)
    {
        // Arrange
        var valueObjects = Enumerable.Range(0, 100)
            .Select(i => new TestValueObject($"Value{i}", i))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            foreach (var vo in valueObjects)
            {
                _ = vo.GetHashCode();
            }
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "GetHashCode should be fast (relaxed for CI)");
    }

    [Fact]
    public void ConcurrentAccess_GetHashCode_ThreadSafe()
    {
        // Arrange
        var vo = new TestValueObject("ConcurrentValue", 42);
        var hashCodes = new List<int>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, _ =>
        {
            var hash = vo.GetHashCode();
            lock (lockObject)
            {
                hashCodes.Add(hash);
            }
        });

        // Assert
        hashCodes.Should().HaveCount(100);
        hashCodes.Should().OnlyContain(h => h == hashCodes[0]); // All should be the same
    }

    // Helper class for testing empty value objects
    private class EmptyValueObject : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield break; // No components
        }
    }
}
