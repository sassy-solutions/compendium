// -----------------------------------------------------------------------
// <copyright file="SearchQueryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests.Models;

public class SearchQueryTests
{
    [Fact]
    public void Defaults_AreApplied_WhenOnlyTextProvided()
    {
        // Act
        var query = new SearchQuery { Text = "laptop" };

        // Assert
        query.Text.Should().Be("laptop");
        query.Filters.Should().BeEmpty();
        query.Facets.Should().BeEmpty();
        query.Sort.Should().BeNull();
        query.Limit.Should().Be(20);
        query.Offset.Should().Be(0);
        query.Highlight.Should().BeFalse();
        query.TenantId.Should().BeNull();
    }

    [Fact]
    public void AllProperties_AreAssignable_ViaInit()
    {
        // Arrange
        var filters = new Dictionary<string, object> { ["category"] = "books", ["in_stock"] = true };
        var facets = new[] { "category", "brand" };
        var sort = SearchSort.Descending("price");

        // Act
        var query = new SearchQuery
        {
            Text = "harry potter",
            Filters = filters,
            Facets = facets,
            Sort = sort,
            Limit = 50,
            Offset = 100,
            Highlight = true,
            TenantId = "tenant-1",
        };

        // Assert
        query.Text.Should().Be("harry potter");
        query.Filters.Should().BeEquivalentTo(filters);
        query.Facets.Should().BeEquivalentTo(facets);
        query.Sort.Should().Be(sort);
        query.Limit.Should().Be(50);
        query.Offset.Should().Be(100);
        query.Highlight.Should().BeTrue();
        query.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void Records_WithSameContent_AreStructurallyEquivalent()
    {
        // Arrange
        var a = new SearchQuery { Text = "x", Limit = 5 };
        var b = new SearchQuery { Text = "x", Limit = 5 };

        // Assert
        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void With_Expression_ProducesNewInstanceWithMutatedField()
    {
        // Arrange
        var original = new SearchQuery { Text = "x" };

        // Act
        var clone = original with { Text = "y", Limit = 100 };

        // Assert
        clone.Should().NotBe(original);
        clone.Text.Should().Be("y");
        clone.Limit.Should().Be(100);
        original.Text.Should().Be("x");
        original.Limit.Should().Be(20);
    }
}
