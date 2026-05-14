// -----------------------------------------------------------------------
// <copyright file="Formality.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Translation.Models;

/// <summary>
/// Specifies how formal the translated text should sound.
/// Providers that don't support formality should treat all values as <see cref="Default"/>.
/// </summary>
public enum Formality
{
    /// <summary>
    /// Use the provider's default formality (no explicit preference).
    /// </summary>
    Default = 0,

    /// <summary>
    /// Require a more formal tone. Fail if the target language does not support it.
    /// </summary>
    More = 1,

    /// <summary>
    /// Require a less formal tone. Fail if the target language does not support it.
    /// </summary>
    Less = 2,

    /// <summary>
    /// Prefer a more formal tone, but fall back silently if unsupported.
    /// </summary>
    PreferMore = 3,

    /// <summary>
    /// Prefer a less formal tone, but fall back silently if unsupported.
    /// </summary>
    PreferLess = 4,
}
