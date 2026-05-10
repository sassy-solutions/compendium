// -----------------------------------------------------------------------
// <copyright file="ParsedDocumentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class ParsedDocumentTests
{
    [Fact]
    public void ParsedDocument_ExposesAllSections()
    {
        // Arrange
        var pages = new List<ParsedPage> { new(1, "page-1", 0.9) };
        var tables = new List<ParsedTable> { new(1, new List<IReadOnlyList<string>> { new[] { "a" } }) };
        var keyValues = new Dictionary<string, ParsedField> { ["Total"] = new("42.00", 0.99) };

        // Act
        var doc = new ParsedDocument("page-1", pages, tables, keyValues, 0.95);

        // Assert
        doc.RawText.Should().Be("page-1");
        doc.Pages.Should().BeSameAs(pages);
        doc.Tables.Should().BeSameAs(tables);
        doc.KeyValues.Should().ContainKey("Total");
        doc.KeyValues["Total"].Value.Should().Be("42.00");
        doc.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void ParsedDocument_WithEmptyCollections_IsValid()
    {
        // Act
        var doc = new ParsedDocument(
            string.Empty,
            Array.Empty<ParsedPage>(),
            Array.Empty<ParsedTable>(),
            new Dictionary<string, ParsedField>(),
            0.0);

        // Assert
        doc.Pages.Should().BeEmpty();
        doc.Tables.Should().BeEmpty();
        doc.KeyValues.Should().BeEmpty();
        doc.Confidence.Should().Be(0.0);
    }
}
