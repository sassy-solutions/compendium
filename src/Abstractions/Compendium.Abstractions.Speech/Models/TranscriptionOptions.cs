// -----------------------------------------------------------------------
// <copyright file="TranscriptionOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Captures the tunable parameters submitted to a speech-to-text provider.
/// </summary>
/// <param name="Language">Optional BCP-47 language hint (for example <c>en-US</c>, <c>fr-FR</c>). When <c>null</c>, the provider is expected to auto-detect.</param>
/// <param name="Model">Optional provider-specific model identifier (for example <c>whisper-large-v3</c>, <c>nova-3</c>).</param>
/// <param name="Diarization">When <c>true</c>, the provider should label speaker turns in the output. Defaults to <c>false</c>.</param>
/// <param name="Punctuation">When <c>true</c>, the provider should insert punctuation and capitalisation. Defaults to <c>true</c>.</param>
public sealed record TranscriptionOptions(
    string? Language = null,
    string? Model = null,
    bool Diarization = false,
    bool Punctuation = true);
