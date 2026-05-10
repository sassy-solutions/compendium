// -----------------------------------------------------------------------
// <copyright file="ParseOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class ParseOptionsTests
{
    [Fact]
    public void ParseOptions_WithModelOnly_DefaultsLanguageNullAndOcrOnlyFalse()
    {
        // Act
        var opts = new ParseOptions(DocumentModel.Generic);

        // Assert
        opts.Model.Should().Be(DocumentModel.Generic);
        opts.Language.Should().BeNull();
        opts.OcrOnly.Should().BeFalse();
    }

    [Theory]
    [InlineData(DocumentModel.Receipt, "fr-CA", false)]
    [InlineData(DocumentModel.Invoice, "en", true)]
    [InlineData(DocumentModel.IdDocument, null, true)]
    [InlineData(DocumentModel.Custom, "de", false)]
    public void ParseOptions_WithAllProperties_ExposesValues(DocumentModel model, string? language, bool ocrOnly)
    {
        // Act
        var opts = new ParseOptions(model, language, ocrOnly);

        // Assert
        opts.Model.Should().Be(model);
        opts.Language.Should().Be(language);
        opts.OcrOnly.Should().Be(ocrOnly);
    }

    [Fact]
    public void ParseOptions_WithSameValues_ShouldBeEqualByValue()
    {
        // Act
        var a = new ParseOptions(DocumentModel.Receipt, "en", true);
        var b = new ParseOptions(DocumentModel.Receipt, "en", true);

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
