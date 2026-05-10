// -----------------------------------------------------------------------
// <copyright file="ParsedFieldTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class ParsedFieldTests
{
    [Fact]
    public void ParsedField_ExposesValueAndConfidence()
    {
        // Act
        var field = new ParsedField("ACME Inc.", 0.87);

        // Assert
        field.Value.Should().Be("ACME Inc.");
        field.Confidence.Should().Be(0.87);
    }

    [Fact]
    public void ParsedField_WithSameValues_ShouldBeEqualByValue()
    {
        // Act
        var a = new ParsedField("v", 1.0);
        var b = new ParsedField("v", 1.0);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
