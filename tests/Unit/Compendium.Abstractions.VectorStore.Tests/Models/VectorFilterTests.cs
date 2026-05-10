// -----------------------------------------------------------------------
// <copyright file="VectorFilterTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Tests.Models;

public class VectorFilterTests
{
    [Fact]
    public void Eq_BuildsEqualityNode()
    {
        // Act
        var filter = VectorFilter.Eq("category", "books");

        // Assert
        filter.Kind.Should().Be(VectorFilterKind.Eq);
        filter.Field.Should().Be("category");
        filter.Value.Should().Be("books");
        filter.Values.Should().BeNull();
        filter.Children.Should().BeNull();
        filter.TenantId.Should().BeNull();
    }

    [Fact]
    public void Ne_BuildsInequalityNode()
    {
        // Act
        var filter = VectorFilter.Ne("status", "deleted");

        // Assert
        filter.Kind.Should().Be(VectorFilterKind.Ne);
        filter.Field.Should().Be("status");
        filter.Value.Should().Be("deleted");
    }

    [Fact]
    public void In_BuildsMembershipNode()
    {
        // Arrange
        var values = new object[] { "a", "b", "c" };

        // Act
        var filter = VectorFilter.In("tag", values);

        // Assert
        filter.Kind.Should().Be(VectorFilterKind.In);
        filter.Field.Should().Be("tag");
        filter.Values.Should().NotBeNull();
        filter.Values!.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Range_WithBothBounds_PreservesInclusivity()
    {
        // Act
        var filter = VectorFilter.Range("price", 10, 100, minInclusive: false, maxInclusive: true);

        // Assert
        filter.Kind.Should().Be(VectorFilterKind.Range);
        filter.Field.Should().Be("price");
        filter.RangeMin.Should().Be(10);
        filter.RangeMax.Should().Be(100);
        filter.RangeMinInclusive.Should().BeFalse();
        filter.RangeMaxInclusive.Should().BeTrue();
    }

    [Fact]
    public void Range_WithOnlyMin_IsAccepted()
    {
        // Act
        var filter = VectorFilter.Range("price", 5, null);

        // Assert
        filter.RangeMin.Should().Be(5);
        filter.RangeMax.Should().BeNull();
    }

    [Fact]
    public void Range_WithOnlyMax_IsAccepted()
    {
        // Act
        var filter = VectorFilter.Range("price", null, 50);

        // Assert
        filter.RangeMin.Should().BeNull();
        filter.RangeMax.Should().Be(50);
    }

    [Fact]
    public void And_BuildsConjunction()
    {
        // Arrange
        var a = VectorFilter.Eq("a", 1);
        var b = VectorFilter.Eq("b", 2);

        // Act
        var combined = VectorFilter.And(a, b);

        // Assert
        combined.Kind.Should().Be(VectorFilterKind.And);
        combined.Children.Should().NotBeNull();
        combined.Children!.Should().HaveCount(2);
        combined.Children![0].Should().BeSameAs(a);
        combined.Children![1].Should().BeSameAs(b);
        combined.Field.Should().BeNull();
        combined.Value.Should().BeNull();
    }

    [Fact]
    public void Or_BuildsDisjunction()
    {
        // Arrange
        var a = VectorFilter.Eq("a", 1);

        // Act
        var combined = VectorFilter.Or(a);

        // Assert
        combined.Kind.Should().Be(VectorFilterKind.Or);
        combined.Children!.Should().ContainSingle().Which.Should().BeSameAs(a);
    }

    [Fact]
    public void ForTenant_ReturnsScopedClone_WithoutMutatingOriginal()
    {
        // Arrange
        var original = VectorFilter.Eq("kind", "doc");

        // Act
        var scoped = original.ForTenant("tenant-7");

        // Assert
        scoped.Should().NotBeSameAs(original);
        scoped.TenantId.Should().Be("tenant-7");
        scoped.Kind.Should().Be(VectorFilterKind.Eq);
        scoped.Field.Should().Be("kind");
        scoped.Value.Should().Be("doc");
        original.TenantId.Should().BeNull();
    }

    [Fact]
    public void ForTenant_PreservesCompoundShape()
    {
        // Arrange
        var compound = VectorFilter.And(VectorFilter.Eq("a", 1), VectorFilter.Ne("b", 2));

        // Act
        var scoped = compound.ForTenant("t-99");

        // Assert
        scoped.Kind.Should().Be(VectorFilterKind.And);
        scoped.Children.Should().BeSameAs(compound.Children);
        scoped.TenantId.Should().Be("t-99");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Eq_WithBlankField_Throws(string? field)
    {
        // Act
        var act = () => VectorFilter.Eq(field!, "x");

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("field");
    }

    [Fact]
    public void Eq_WithNullValue_Throws()
    {
        // Act
        var act = () => VectorFilter.Eq("k", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Ne_WithBlankField_Throws(string? field)
    {
        // Act
        var act = () => VectorFilter.Ne(field!, "x");

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("field");
    }

    [Fact]
    public void Ne_WithNullValue_Throws()
    {
        // Act
        var act = () => VectorFilter.Ne("k", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("value");
    }

    [Fact]
    public void In_WithBlankField_Throws()
    {
        // Act
        var act = () => VectorFilter.In(" ", new object[] { 1 });

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("field");
    }

    [Fact]
    public void In_WithNullValues_Throws()
    {
        // Act
        var act = () => VectorFilter.In("k", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("values");
    }

    [Fact]
    public void In_WithEmptyValues_Throws()
    {
        // Act
        var act = () => VectorFilter.In("k", Array.Empty<object>());

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("values");
    }

    [Fact]
    public void Range_WithBlankField_Throws()
    {
        // Act
        var act = () => VectorFilter.Range("", 0, 10);

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("field");
    }

    [Fact]
    public void Range_WithNoBounds_Throws()
    {
        // Act
        var act = () => VectorFilter.Range("price", null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void And_WithNoArgs_Throws()
    {
        // Act
        var act = () => VectorFilter.And();

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("filters");
    }

    [Fact]
    public void Or_WithNoArgs_Throws()
    {
        // Act
        var act = () => VectorFilter.Or();

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("filters");
    }

    [Fact]
    public void And_WithNullFiltersArray_Throws()
    {
        // Act
        var act = () => VectorFilter.And((VectorFilter[])null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("filters");
    }

    [Fact]
    public void Or_WithNullChild_Throws()
    {
        // Act
        var act = () => VectorFilter.Or(null!);

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("filters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ForTenant_WithBlankTenantId_Throws(string? tenantId)
    {
        // Arrange
        var filter = VectorFilter.Eq("a", 1);

        // Act
        var act = () => filter.ForTenant(tenantId!);

        // Assert
        act.Should().Throw<ArgumentException>().And.ParamName.Should().Be("tenantId");
    }
}
