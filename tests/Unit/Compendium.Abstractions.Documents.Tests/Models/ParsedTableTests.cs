// -----------------------------------------------------------------------
// <copyright file="ParsedTableTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class ParsedTableTests
{
    [Fact]
    public void ParsedTable_ExposesPageNumberAndRows()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Item", "Qty", "Price" },
            new[] { "Coffee", "2", "8.00" },
        };

        // Act
        var table = new ParsedTable(3, rows);

        // Assert
        table.PageNumber.Should().Be(3);
        table.Rows.Should().HaveCount(2);
        table.Rows[1][0].Should().Be("Coffee");
        table.Rows[1][2].Should().Be("8.00");
    }

    [Fact]
    public void ParsedTable_WithSameReferenceRows_ShouldBeEqualByValue()
    {
        // Arrange
        var rows = new List<IReadOnlyList<string>> { new[] { "a" } };

        // Act
        var a = new ParsedTable(1, rows);
        var b = new ParsedTable(1, rows);

        // Assert
        a.Should().Be(b);
    }
}
