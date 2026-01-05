// -----------------------------------------------------------------------
// <copyright file="EntityTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Primitives;

public class EntityTests
{
    [Fact]
    public void Constructor_WithValidId_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Entity";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestEntity(id, name);

        // Assert
        entity.Id.Should().Be(id);
        entity.Name.Should().Be(name);
        entity.CreatedAt.Should().BeCloseTo(beforeCreation, TimeSpan.FromSeconds(1));
        entity.ModifiedAt.Should().Be(entity.CreatedAt);
        entity.IsTransient.Should().BeFalse();
        entity.BrokenRules.Should().BeEmpty();
        entity.HasBrokenRules.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestEntity(default(Guid));
        act.Should().NotThrow(); // Guid.Empty is valid, just makes entity transient
    }

    [Fact]
    public void IsTransient_WithEmptyGuid_ReturnsTrue()
    {
        // Arrange & Act
        var entity = new TestEntity(Guid.Empty);

        // Assert
        entity.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithValidGuid_ReturnsFalse()
    {
        // Arrange & Act
        var entity = new TestEntity(Guid.NewGuid());

        // Assert
        entity.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Entity 1");
        var entity2 = new TestEntity(Guid.NewGuid(), "Entity 2");

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        var result = entity.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        var result = entity.Equals(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var other = new object();

        // Act
        var result = entity.Equals(other);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_TransientEntities_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.Empty);
        var entity2 = new TestEntity(Guid.Empty);

        // Act
        var result = entity1.Equals(entity2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_OneTransientOneNot_ReturnsFalse()
    {
        // Arrange
        var transientEntity = new TestEntity(Guid.Empty);
        var persistentEntity = new TestEntity(Guid.NewGuid());

        // Act
        var result = transientEntity.Equals(persistentEntity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, "Entity 1");
        var entity2 = new TestEntity(id, "Entity 2");

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentId_ReturnsDifferentHash()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_TransientEntity_UsesBaseImplementation()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.Empty);
        var entity2 = new TestEntity(Guid.Empty);

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2); // Different instances should have different hashes
    }

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        TestEntity? entity2 = null;

        // Act
        var result = entity1 == entity2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_SameId_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act
        var result = entity1 != entity2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CheckRule_ValidRule_DoesNotThrow()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var rule = new TestBusinessRule(false, "Valid rule");

        // Act
        var act = () => entity.TestCheckRule(rule);

        // Assert
        act.Should().NotThrow();
        entity.BrokenRules.Should().BeEmpty();
        entity.HasBrokenRules.Should().BeFalse();
    }

    [Fact]
    public void CheckRule_BrokenRule_ThrowsBusinessRuleValidationException()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var rule = new TestBusinessRule(true, "Rule is broken", "TEST_001");

        // Act
        var act = () => entity.TestCheckRule(rule);

        // Assert
        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("Rule is broken")
           .And.BrokenRule.Should().Be(rule);

        entity.BrokenRules.Should().ContainSingle().Which.Should().Be(rule);
        entity.HasBrokenRules.Should().BeTrue();
    }

    [Fact]
    public void CheckRule_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act
        var act = () => entity.TestCheckRule(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CheckRule_MultipleBrokenRules_AccumulatesRules()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var rule1 = new TestBusinessRule(true, "Rule 1 broken", "TEST_001");
        var rule2 = new TestBusinessRule(true, "Rule 2 broken", "TEST_002");

        // Act
        var act1 = () => entity.TestCheckRule(rule1);
        var act2 = () => entity.TestCheckRule(rule2);

        // Assert
        act1.Should().Throw<BusinessRuleValidationException>();
        act2.Should().Throw<BusinessRuleValidationException>();

        entity.BrokenRules.Should().HaveCount(2);
        entity.BrokenRules.Should().Contain(rule1);
        entity.BrokenRules.Should().Contain(rule2);
        entity.HasBrokenRules.Should().BeTrue();
    }

    [Fact]
    public void Touch_UpdatesModifiedAt()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var originalModifiedAt = entity.ModifiedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        entity.TestTouch();

        // Assert
        entity.ModifiedAt.Should().BeAfter(originalModifiedAt);
        entity.CreatedAt.Should().Be(originalModifiedAt); // CreatedAt should not change
    }

    [Fact]
    public void UpdateName_UpdatesNameAndModifiedAt()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Original Name");
        var originalModifiedAt = entity.ModifiedAt;
        Thread.Sleep(10);

        // Act
        entity.UpdateName("New Name");

        // Assert
        entity.Name.Should().Be("New Name");
        entity.ModifiedAt.Should().BeAfter(originalModifiedAt);
    }

    [Fact]
    public void ClearBrokenRules_WithBrokenRules_ClearsCollection()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var rule = new TestBusinessRule(true, "Broken rule");

        try
        { entity.TestCheckRule(rule); }
        catch { /* Expected */ }

        // Act
        entity.TestClearBrokenRules();

        // Assert
        entity.BrokenRules.Should().BeEmpty();
        entity.HasBrokenRules.Should().BeFalse();
    }

    [Theory]
    [InlineData(1000)]
    public void Equals_PerformanceTest_CompletesQuickly(int iterations)
    {
        // Arrange
        var entities = Enumerable.Range(0, 100)
            .Select(_ => new TestEntity(Guid.NewGuid()))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            foreach (var e1 in entities.Take(10))
            {
                foreach (var e2 in entities.Take(10))
                {
                    _ = e1.Equals(e2);
                }
            }
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Entity equality should be fast");
    }

    [Fact]
    public void ConcurrentAccess_BrokenRules_ThreadSafe()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var rules = Enumerable.Range(0, 100)
            .Select(i => new TestBusinessRule(true, $"Rule {i}", $"TEST_{i:000}"))
            .ToList();

        // Act
        Parallel.ForEach(rules, rule =>
        {
            try
            { entity.TestCheckRule(rule); }
            catch { /* Expected */ }
        });

        // Assert
        entity.BrokenRules.Should().HaveCount(100);
        entity.HasBrokenRules.Should().BeTrue();
    }
}
