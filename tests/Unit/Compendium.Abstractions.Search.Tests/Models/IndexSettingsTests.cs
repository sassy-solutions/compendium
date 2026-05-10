// -----------------------------------------------------------------------
// <copyright file="IndexSettingsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests.Models;

public class IndexSettingsTests
{
    [Fact]
    public void Defaults_AreEmptyCollections()
    {
        // Act
        var settings = new IndexSettings();

        // Assert
        settings.SearchableAttributes.Should().BeEmpty();
        settings.FilterableAttributes.Should().BeEmpty();
        settings.SortableAttributes.Should().BeEmpty();
        settings.DistinctAttribute.Should().BeNull();
        settings.RankingRules.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_AssignableViaInit()
    {
        // Arrange
        var searchable = new[] { "title", "description" };
        var filterable = new[] { "category", "in_stock" };
        var sortable = new[] { "price", "created_at" };
        var ranking = new[] { "words", "typo", "proximity" };

        // Act
        var settings = new IndexSettings
        {
            SearchableAttributes = searchable,
            FilterableAttributes = filterable,
            SortableAttributes = sortable,
            DistinctAttribute = "sku",
            RankingRules = ranking,
        };

        // Assert
        settings.SearchableAttributes.Should().BeEquivalentTo(searchable);
        settings.FilterableAttributes.Should().BeEquivalentTo(filterable);
        settings.SortableAttributes.Should().BeEquivalentTo(sortable);
        settings.DistinctAttribute.Should().Be("sku");
        settings.RankingRules.Should().BeEquivalentTo(ranking);
    }

    [Fact]
    public void Records_WithSameContent_AreEqual()
    {
        // Arrange
        var a = new IndexSettings { DistinctAttribute = "id" };
        var b = new IndexSettings { DistinctAttribute = "id" };

        // Assert
        a.Should().Be(b);
    }
}
