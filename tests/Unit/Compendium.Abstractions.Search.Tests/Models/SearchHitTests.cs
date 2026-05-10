// -----------------------------------------------------------------------
// <copyright file="SearchHitTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Search.Tests.Models;

public class SearchHitTests
{
    private sealed record Document(string Id, string Title);

    [Fact]
    public void Defaults_AssignedWhenOnlyDocumentSet()
    {
        // Arrange
        var doc = new Document("1", "Hello");

        // Act
        var hit = new SearchHit<Document> { Document = doc };

        // Assert
        hit.Document.Should().Be(doc);
        hit.Score.Should().Be(0d);
        hit.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_AssignableViaInit()
    {
        // Arrange
        var doc = new Document("42", "Title");
        var highlights = new Dictionary<string, IReadOnlyList<string>>
        {
            ["title"] = new[] { "<em>Title</em>" },
        };

        // Act
        var hit = new SearchHit<Document>
        {
            Document = doc,
            Score = 0.95,
            Highlights = highlights,
        };

        // Assert
        hit.Document.Should().Be(doc);
        hit.Score.Should().Be(0.95);
        hit.Highlights.Should().ContainKey("title");
        hit.Highlights["title"].Should().ContainSingle().Which.Should().Be("<em>Title</em>");
    }

    [Fact]
    public void Records_WithSameContent_AreStructurallyEquivalent()
    {
        // Arrange
        var doc = new Document("1", "x");
        var a = new SearchHit<Document> { Document = doc, Score = 1.0 };
        var b = new SearchHit<Document> { Document = doc, Score = 1.0 };

        // Assert
        a.Should().BeEquivalentTo(b);
    }
}
