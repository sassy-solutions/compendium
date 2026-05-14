// -----------------------------------------------------------------------
// <copyright file="SearchSortTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests.Models;

public class SearchSortTests
{
    [Fact]
    public void Constructor_WithFieldOnly_DefaultsToAscending()
    {
        // Act
        var sort = new SearchSort("price");

        // Assert
        sort.Field.Should().Be("price");
        sort.Direction.Should().Be(SortDirection.Asc);
    }

    [Fact]
    public void Constructor_WithExplicitDirection_AssignsBothFields()
    {
        // Act
        var sort = new SearchSort("created_at", SortDirection.Desc);

        // Assert
        sort.Field.Should().Be("created_at");
        sort.Direction.Should().Be(SortDirection.Desc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithBlankField_Throws(string? field)
    {
        // Act
        var act = () => new SearchSort(field!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("field");
    }

    [Fact]
    public void Ascending_Factory_BuildsAscendingSort()
    {
        // Act
        var sort = SearchSort.Ascending("name");

        // Assert
        sort.Field.Should().Be("name");
        sort.Direction.Should().Be(SortDirection.Asc);
    }

    [Fact]
    public void Descending_Factory_BuildsDescendingSort()
    {
        // Act
        var sort = SearchSort.Descending("name");

        // Assert
        sort.Field.Should().Be("name");
        sort.Direction.Should().Be(SortDirection.Desc);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        // Arrange
        var a = new SearchSort("price", SortDirection.Desc);
        var b = new SearchSort("price", SortDirection.Desc);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentDirection_AreNotEqual()
    {
        // Arrange
        var asc = new SearchSort("price");
        var desc = new SearchSort("price", SortDirection.Desc);

        // Assert
        asc.Should().NotBe(desc);
    }

    [Fact]
    public void SortDirection_HasExpectedMembers()
    {
        // Assert
        Enum.GetNames<SortDirection>().Should().BeEquivalentTo("Asc", "Desc");
    }
}
