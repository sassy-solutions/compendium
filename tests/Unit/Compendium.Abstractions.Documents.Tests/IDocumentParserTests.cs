// -----------------------------------------------------------------------
// <copyright file="IDocumentParserTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests;

public sealed class IDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_WhenSubstituteReturnsSuccess_PropagatesValue()
    {
        // Arrange
        var parser = Substitute.For<IDocumentParser>();
        using var stream = new MemoryStream();
        var input = new DocumentInput(stream, "application/pdf");
        var opts = new ParseOptions(DocumentModel.Receipt, "en");
        var parsed = new ParsedDocument(
            "raw",
            Array.Empty<ParsedPage>(),
            Array.Empty<ParsedTable>(),
            new Dictionary<string, ParsedField>(),
            0.9);
        parser
            .ParseAsync(input, opts, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(parsed)));

        // Act
        var result = await parser.ParseAsync(input, opts, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(parsed);
        await parser.Received(1).ParseAsync(input, opts, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseAsync_WhenSubstituteReturnsFailure_PropagatesError()
    {
        // Arrange
        var parser = Substitute.For<IDocumentParser>();
        using var stream = new MemoryStream();
        var input = new DocumentInput(stream, "application/x-bogus");
        var opts = new ParseOptions(DocumentModel.Generic);
        var error = DocumentsErrors.UnsupportedFormat("application/x-bogus");
        parser
            .ParseAsync(input, opts, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<ParsedDocument>(error)));

        // Act
        var result = await parser.ParseAsync(input, opts, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Documents.UnsupportedFormat");
    }
}
