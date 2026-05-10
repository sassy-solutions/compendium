// -----------------------------------------------------------------------
// <copyright file="SearchResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests.Models;

public class SearchResultTests
{
    private sealed record Document(string Id);

    [Fact]
    public void Defaults_ProduceEmptyResult()
    {
        // Act
        var result = new SearchResult<Document>();

        // Assert
        result.Hits.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.FacetCounts.Should().BeEmpty();
        result.Took.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AllProperties_AssignableViaInit()
    {
        // Arrange
        var hits = new[]
        {
            new SearchHit<Document> { Document = new Document("1"), Score = 1.0 },
            new SearchHit<Document> { Document = new Document("2"), Score = 0.5 },
        };
        var facets = new Dictionary<string, IReadOnlyDictionary<string, long>>
        {
            ["category"] = new Dictionary<string, long> { ["books"] = 12, ["games"] = 3 },
        };

        // Act
        var result = new SearchResult<Document>
        {
            Hits = hits,
            Total = 42,
            FacetCounts = facets,
            Took = TimeSpan.FromMilliseconds(15),
        };

        // Assert
        result.Hits.Should().HaveCount(2);
        result.Total.Should().Be(42);
        result.FacetCounts.Should().ContainKey("category");
        result.FacetCounts["category"]["books"].Should().Be(12);
        result.Took.Should().Be(TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public void Records_WithSameContent_AreStructurallyEquivalent()
    {
        // Arrange
        var a = new SearchResult<Document> { Total = 7 };
        var b = new SearchResult<Document> { Total = 7 };

        // Assert
        a.Should().BeEquivalentTo(b);
    }
}
