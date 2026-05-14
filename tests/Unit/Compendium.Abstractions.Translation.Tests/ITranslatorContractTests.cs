// -----------------------------------------------------------------------
// <copyright file="ITranslatorContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Tests;

public class ITranslatorContractTests
{
    [Fact]
    public async Task TranslateAsync_OnSubstitute_ReturnsConfiguredResult()
    {
        // Arrange
        var translator = Substitute.For<ITranslator>();
        var options = new TranslationOptions("en", "fr", Formality.Default);
        var expected = new TranslationResult("Bonjour", "en", 0.9);
        translator
            .TranslateAsync("Hello", options, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        // Act
        var result = await translator.TranslateAsync("Hello", options, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task TranslateBatchAsync_OnSubstitute_ReturnsConfiguredList()
    {
        // Arrange
        var translator = Substitute.For<ITranslator>();
        var options = new TranslationOptions(null, "de", Formality.More);
        IReadOnlyList<string> inputs = new[] { "Hello", "World" };
        IReadOnlyList<TranslationResult> expected = new[]
        {
            new TranslationResult("Hallo", "en", 0.99),
            new TranslationResult("Welt", "en", 0.97),
        };
        translator
            .TranslateBatchAsync(inputs, options, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expected));

        // Act
        var result = await translator.TranslateBatchAsync(inputs, options, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task DetectLanguageAsync_OnSubstitute_ReturnsConfiguredLanguage()
    {
        // Arrange
        var translator = Substitute.For<ITranslator>();
        translator
            .DetectLanguageAsync("こんにちは", Arg.Any<CancellationToken>())
            .Returns(Result.Success("ja"));

        // Act
        var result = await translator.DetectLanguageAsync("こんにちは", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ja");
    }

    [Fact]
    public async Task TranslateAsync_WhenProviderFails_PropagatesFailureResult()
    {
        // Arrange
        var translator = Substitute.For<ITranslator>();
        var options = new TranslationOptions("en", "xx", Formality.Default);
        var expectedError = TranslationErrors.UnsupportedLanguage("xx");
        translator
            .TranslateAsync(Arg.Any<string>(), options, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TranslationResult>(expectedError));

        // Act
        var result = await translator.TranslateAsync("Hello", options, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedError.Code);
    }
}
