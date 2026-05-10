// -----------------------------------------------------------------------
// <copyright file="TranslationOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Models;

/// <summary>
/// Options controlling a translation request.
/// </summary>
/// <param name="SourceLanguage">
/// Optional BCP-47 source language code (e.g. "en", "fr-CA"). When <c>null</c>, the provider
/// will auto-detect the source language.
/// </param>
/// <param name="TargetLanguage">
/// Required BCP-47 target language code (e.g. "de", "pt-BR").
/// </param>
/// <param name="Formality">
/// Desired formality of the translation. Defaults to <see cref="Formality.Default"/>.
/// </param>
public sealed record TranslationOptions(
    string? SourceLanguage,
    string TargetLanguage,
    Formality Formality = Formality.Default);
