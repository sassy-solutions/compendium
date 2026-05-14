// -----------------------------------------------------------------------
// <copyright file="AudioChunk.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents a single audio frame used to feed a streaming speech-to-text provider.
/// </summary>
/// <param name="Bytes">The raw PCM (or codec-defined) bytes of the chunk.</param>
/// <param name="SampleRate">The audio sample rate in Hertz (for example 16000 or 48000).</param>
public sealed record AudioChunk(ReadOnlyMemory<byte> Bytes, int SampleRate);
