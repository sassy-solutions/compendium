// -----------------------------------------------------------------------
// <copyright file="SynthesisOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Captures the tunable parameters submitted to a text-to-speech provider.
/// </summary>
/// <param name="VoiceId">The provider-specific voice identifier to render the synthesized audio with.</param>
/// <param name="Model">Optional provider-specific model identifier (for example <c>eleven_multilingual_v2</c>). When <c>null</c>, the provider default is used.</param>
/// <param name="Format">The desired audio container/codec for the synthesized output. Defaults to <see cref="AudioFormat.Mp3"/>.</param>
/// <param name="SampleRate">The audio sample rate in Hertz. Defaults to <c>22050</c>.</param>
/// <param name="Stability">The provider-specific voice stability factor in the range <c>[0.0, 1.0]</c>. Defaults to <c>0.5</c>.</param>
public sealed record SynthesisOptions(
    string VoiceId,
    string? Model = null,
    AudioFormat Format = AudioFormat.Mp3,
    int SampleRate = 22050,
    double Stability = 0.5);
