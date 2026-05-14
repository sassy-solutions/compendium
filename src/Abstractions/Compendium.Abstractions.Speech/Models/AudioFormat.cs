// -----------------------------------------------------------------------
// <copyright file="AudioFormat.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Enumerates the audio container/codec formats supported as text-to-speech synthesis output.
/// </summary>
public enum AudioFormat
{
    /// <summary>MPEG-1 Audio Layer III container.</summary>
    Mp3 = 0,

    /// <summary>Waveform Audio File Format (uncompressed PCM).</summary>
    Wav = 1,

    /// <summary>Opus codec inside an Ogg container.</summary>
    Opus = 2,

    /// <summary>Free Lossless Audio Codec.</summary>
    Flac = 3,

    /// <summary>Raw 16-bit signed PCM samples.</summary>
    Pcm16 = 4,
}
