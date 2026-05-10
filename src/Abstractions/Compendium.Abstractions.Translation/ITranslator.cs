// -----------------------------------------------------------------------
// <copyright file="ITranslator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Translation.Models;

namespace Compendium.Abstractions.Translation;

/// <summary>
/// Provides text translation and language detection.
/// This interface is provider-agnostic and can be implemented by providers such as
/// DeepL, Google Cloud Translation, Azure Translator, or local models.
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Translates a single piece of text.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="opts">Translation options including target language and formality.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The translation result, or a failure describing why translation could not be performed.</returns>
    Task<Result<TranslationResult>> TranslateAsync(string text, TranslationOptions opts, CancellationToken ct);

    /// <summary>
    /// Translates a batch of texts in one call. Order of the result corresponds to the order of the inputs.
    /// </summary>
    /// <param name="texts">The texts to translate.</param>
    /// <param name="opts">Translation options applied to every input.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of translation results, or a failure if the batch could not be processed.</returns>
    Task<Result<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(IReadOnlyList<string> texts, TranslationOptions opts, CancellationToken ct);

    /// <summary>
    /// Detects the BCP-47 language of the supplied text.
    /// </summary>
    /// <param name="text">The text to inspect.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The detected language code, or a failure describing why detection could not be performed.</returns>
    Task<Result<string>> DetectLanguageAsync(string text, CancellationToken ct);
}
