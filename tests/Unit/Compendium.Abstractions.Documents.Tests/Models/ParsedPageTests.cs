// -----------------------------------------------------------------------
// <copyright file="ParsedPageTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class ParsedPageTests
{
    [Fact]
    public void ParsedPage_ExposesAllProperties()
    {
        // Act
        var page = new ParsedPage(2, "hello", 0.95);

        // Assert
        page.PageNumber.Should().Be(2);
        page.Text.Should().Be("hello");
        page.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void ParsedPage_WithSameValues_ShouldBeEqualByValue()
    {
        // Act
        var a = new ParsedPage(1, "x", 0.5);
        var b = new ParsedPage(1, "x", 0.5);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
