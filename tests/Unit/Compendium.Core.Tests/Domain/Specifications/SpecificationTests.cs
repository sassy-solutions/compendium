// -----------------------------------------------------------------------
// <copyright file="SpecificationTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;

namespace Compendium.Core.Tests.Domain.Specifications;

public class SpecificationTests
{
    #region Test Data Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    #endregion

    #region Test Specifications

    private class IdSpecification : Specification<TestEntity>
    {
        public IdSpecification(int id) : base(x => x.Id == id)
        {
        }
    }

    private class NameSpecification : Specification<TestEntity>
    {
        public NameSpecification(string name) : base(x => x.Name == name)
        {
        }
    }

    private class AgeRangeSpecification : Specification<TestEntity>
    {
        public AgeRangeSpecification(int minAge, int maxAge) : base(x => x.Age >= minAge && x.Age <= maxAge)
        {
        }
    }

    private class ActiveSpecification : Specification<TestEntity>
    {
        public ActiveSpecification() : base(x => x.IsActive)
        {
        }
    }

    private class SpecificationWithIncludes : Specification<TestEntity>
    {
        public SpecificationWithIncludes() : base(x => x.IsActive)
        {
            AddInclude(x => x.Name);
            AddInclude("Category");
        }
    }

    private sealed class SpecificationWithOrdering : Specification<TestEntity>
    {
        public SpecificationWithOrdering(bool descending = false) : base(x => x.IsActive)
        {
            if (descending)
            {
                ApplyOrderByDescending(x => x.CreatedAt);
            }
            else
            {
                ApplyOrderBy(x => x.CreatedAt);
            }
        }
    }

    private sealed class SpecificationWithPaging : Specification<TestEntity>
    {
        public SpecificationWithPaging(int skip, int take) : base(x => x.IsActive)
        {
            ApplyPaging(skip, take);
        }
    }

    private sealed class SpecificationWithGrouping : Specification<TestEntity>
    {
        public SpecificationWithGrouping() : base(x => x.IsActive)
        {
            ApplyGroupBy(x => x.Category);
        }
    }

    #endregion

    #region Basic Specification Tests

    [Fact]
    public void Specification_Constructor_WithValidCriteria_SetsPropertiesCorrectly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.Id == 1;

        // Act
        var spec = new IdSpecification(1);

        // Assert
        spec.Criteria.Should().NotBeNull();
        spec.Includes.Should().BeEmpty();
        spec.IncludeStrings.Should().BeEmpty();
        spec.OrderBy.Should().BeNull();
        spec.OrderByDescending.Should().BeNull();
        spec.GroupBy.Should().BeNull();
        spec.Take.Should().BeNull();
        spec.Skip.Should().BeNull();
        spec.IsPagingEnabled.Should().BeFalse();
    }

    [Fact]
    public void Specification_Constructor_WithNullCriteria_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestSpecification(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        var spec = new IdSpecification(1);
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNonMatchingEntity_ReturnsFalse()
    {
        // Arrange
        var spec = new IdSpecification(1);
        var entity = new TestEntity { Id = 2, Name = "Test" };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new IdSpecification(1);

        // Act
        var act = () => spec.IsSatisfiedBy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Include Tests

    [Fact]
    public void Specification_WithIncludes_SetsIncludesCorrectly()
    {
        // Act
        var spec = new SpecificationWithIncludes();

        // Assert
        spec.Includes.Should().HaveCount(1);
        spec.IncludeStrings.Should().HaveCount(1);
        spec.IncludeStrings.Should().Contain("Category");
    }

    [Fact]
    public void AddInclude_WithExpression_AddsToIncludes()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        spec.TestAddInclude(x => x.Name);

        // Assert
        spec.Includes.Should().HaveCount(1);
    }

    [Fact]
    public void AddInclude_WithString_AddsToIncludeStrings()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        spec.TestAddInclude("Category");

        // Assert
        spec.IncludeStrings.Should().HaveCount(1);
        spec.IncludeStrings.Should().Contain("Category");
    }

    [Fact]
    public void AddInclude_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestAddInclude((Expression<Func<TestEntity, object>>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddInclude_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestAddInclude((string)null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddInclude_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestAddInclude(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void ApplyOrderBy_SetsOrderByCorrectly()
    {
        // Act
        var spec = new SpecificationWithOrdering(false);

        // Assert
        spec.OrderBy.Should().NotBeNull();
        spec.OrderByDescending.Should().BeNull();
    }

    [Fact]
    public void ApplyOrderByDescending_SetsOrderByDescendingCorrectly()
    {
        // Act
        var spec = new SpecificationWithOrdering(true);

        // Assert
        spec.OrderBy.Should().BeNull();
        spec.OrderByDescending.Should().NotBeNull();
    }

    [Fact]
    public void ApplyOrderBy_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyOrderBy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyOrderByDescending_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyOrderByDescending(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Grouping Tests

    [Fact]
    public void ApplyGroupBy_SetsGroupByCorrectly()
    {
        // Act
        var spec = new SpecificationWithGrouping();

        // Assert
        spec.GroupBy.Should().NotBeNull();
    }

    [Fact]
    public void ApplyGroupBy_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyGroupBy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Paging Tests

    [Fact]
    public void ApplyPaging_SetsPagingCorrectly()
    {
        // Act
        var spec = new SpecificationWithPaging(10, 20);

        // Assert
        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(20);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void ApplyPaging_WithNegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyPaging(-1, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApplyPaging_WithZeroTake_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyPaging(0, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ApplyPaging_WithNegativeTake_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id == 1);

        // Act
        var act = () => spec.TestApplyPaging(0, -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Composition Tests

    [Fact]
    public void And_CombinesSpecificationsWithLogicalAnd()
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new ActiveSpecification();
        var entity = new TestEntity { Id = 1, IsActive = true };

        // Act
        var combinedSpec = spec1.And(spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_WithOneFailingSpec_ReturnsFalse()
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new ActiveSpecification();
        var entity = new TestEntity { Id = 1, IsActive = false };

        // Act
        var combinedSpec = spec1.And(spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Or_CombinesSpecificationsWithLogicalOr()
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new IdSpecification(2);
        var entity = new TestEntity { Id = 2 };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Or_WithBothFailingSpecs_ReturnsFalse()
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new IdSpecification(2);
        var entity = new TestEntity { Id = 3 };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Not_InvertsSpecificationLogic()
    {
        // Arrange
        var spec = new IdSpecification(1);
        var entity = new TestEntity { Id = 2 };

        // Act
        var notSpec = spec.Not();
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Not_WithMatchingEntity_ReturnsFalse()
    {
        // Arrange
        var spec = new IdSpecification(1);
        var entity = new TestEntity { Id = 1 };

        // Act
        var notSpec = spec.Not();
        var result = notSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void And_WithNullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new IdSpecification(1);

        // Act
        var act = () => spec.And(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new IdSpecification(1);

        // Act
        var act = () => spec.Or(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Complex Composition Tests

    [Fact]
    public void ComplexComposition_AndOrNot_WorksCorrectly()
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new IdSpecification(2);
        var spec3 = new ActiveSpecification();

        var entity1 = new TestEntity { Id = 1, IsActive = true };
        var entity2 = new TestEntity { Id = 2, IsActive = false };
        var entity3 = new TestEntity { Id = 3, IsActive = true };

        // Act
        // (Id == 1 OR Id == 2) AND NOT IsActive
        var complexSpec = spec1.Or(spec2).And(spec3.Not());

        // Assert
        complexSpec.IsSatisfiedBy(entity1).Should().BeFalse(); // Id=1, Active=true -> false
        complexSpec.IsSatisfiedBy(entity2).Should().BeTrue();  // Id=2, Active=false -> true
        complexSpec.IsSatisfiedBy(entity3).Should().BeFalse(); // Id=3, Active=true -> false
    }

    [Fact]
    public void ChainedComposition_WorksCorrectly()
    {
        // Arrange
        var ageSpec = new AgeRangeSpecification(18, 65);
        var activeSpec = new ActiveSpecification();
        var nameSpec = new NameSpecification("John");

        var entity = new TestEntity { Id = 1, Name = "John", Age = 25, IsActive = true };

        // Act
        var chainedSpec = ageSpec.And(activeSpec).And(nameSpec);
        var result = chainedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void Specification_Evaluation_PerformanceTest(int iterations)
    {
        // Arrange
        var spec = new AgeRangeSpecification(18, 65);
        var entity = new TestEntity { Age = 25 };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = spec.IsSatisfiedBy(entity);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Specification evaluation should be fast");
    }

    [Theory]
    [InlineData(100)]
    public void Specification_Composition_PerformanceTest(int iterations)
    {
        // Arrange
        var spec1 = new IdSpecification(1);
        var spec2 = new ActiveSpecification();
        var entity = new TestEntity { Id = 1, IsActive = true };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var combined = spec1.And(spec2).Or(spec1.Not());
            _ = combined.IsSatisfiedBy(entity);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Specification composition should be fast (relaxed for CI)");
    }

    [Fact]
    public void Specification_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var spec = new AgeRangeSpecification(18, 65);
        var entities = Enumerable.Range(1, 100)
            .Select(i => new TestEntity { Id = i, Age = 10 + (i % 60) }) // Ages 10-69, some outside range
            .ToList();
        var results = new List<bool>();
        var lockObject = new object();

        // Act
        Parallel.ForEach(entities, entity =>
        {
            var result = spec.IsSatisfiedBy(entity);
            lock (lockObject)
            {
                results.Add(result);
            }
        });

        // Assert
        results.Should().HaveCount(100);
        results.Should().Contain(true);
        results.Should().Contain(false);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Specification_WithComplexExpression_WorksCorrectly()
    {
        // Arrange
        var spec = new TestSpecification(x =>
            x.Name.StartsWith("Test") &&
            x.Age > 18 &&
            x.CreatedAt > DateTime.Now.AddDays(-30));

        var entity = new TestEntity
        {
            Name = "TestUser",
            Age = 25,
            CreatedAt = DateTime.Now.AddDays(-10)
        };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Specification_WithNullableProperties_HandlesCorrectly()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Name != null && x.Name.Length > 0);
        var entityWithName = new TestEntity { Name = "Test" };
        var entityWithoutName = new TestEntity { Name = null! };

        // Act & Assert
        spec.IsSatisfiedBy(entityWithName).Should().BeTrue();
        spec.IsSatisfiedBy(entityWithoutName).Should().BeFalse();
    }

    #endregion

    #region Helper Test Specification

    private class TestSpecification : Specification<TestEntity>
    {
        public TestSpecification(Expression<Func<TestEntity, bool>> criteria) : base(criteria)
        {
        }

        public TestSpecification TestAddInclude(Expression<Func<TestEntity, object>> includeExpression)
        {
            AddInclude(includeExpression);
            return this;
        }

        public TestSpecification TestAddInclude(string includeString)
        {
            AddInclude(includeString);
            return this;
        }

        public TestSpecification TestApplyOrderBy(Expression<Func<TestEntity, object>> orderByExpression)
        {
            ApplyOrderBy(orderByExpression);
            return this;
        }

        public TestSpecification TestApplyOrderByDescending(Expression<Func<TestEntity, object>> orderByDescendingExpression)
        {
            ApplyOrderByDescending(orderByDescendingExpression);
            return this;
        }

        public TestSpecification TestApplyGroupBy(Expression<Func<TestEntity, object>> groupByExpression)
        {
            ApplyGroupBy(groupByExpression);
            return this;
        }

        public TestSpecification TestApplyPaging(int skip, int take)
        {
            ApplyPaging(skip, take);
            return this;
        }
    }

    #endregion

    #region Integration with TestData

    [Fact]
    public void TestData_Specifications_WorkCorrectly()
    {
        // Arrange
        var spec = new TestSpecification(x => x.Id > 0);
        var entity = new TestEntity { Id = 1 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
