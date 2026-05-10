// -----------------------------------------------------------------------
// <copyright file="TranslationResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Models;

/// <summary>
/// Outcome of a successful translation.
/// </summary>
/// <param name="TranslatedText">The translated text.</param>
/// <param name="DetectedSourceLanguage">
/// The BCP-47 source language code reported by the provider — either echoed from the request
/// or auto-detected when <see cref="TranslationOptions.SourceLanguage"/> was <c>null</c>.
/// </param>
/// <param name="Confidence">
/// Optional confidence score in the range [0.0, 1.0]. <c>null</c> when the provider does not
/// expose a confidence value.
/// </param>
public sealed record TranslationResult(
    string TranslatedText,
    string DetectedSourceLanguage,
    double? Confidence);
