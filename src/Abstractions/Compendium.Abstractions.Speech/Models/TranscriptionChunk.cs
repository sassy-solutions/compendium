// -----------------------------------------------------------------------
// <copyright file="TranscriptionChunk.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents an incremental transcription update emitted by a streaming speech-to-text provider.
/// </summary>
/// <param name="PartialText">The partial transcript text accumulated up to this chunk.</param>
/// <param name="IsFinal">When <c>true</c>, this chunk is the terminal update for the current utterance.</param>
/// <param name="Timestamp">The timestamp of this chunk relative to the beginning of the audio stream.</param>
public sealed record TranscriptionChunk(string PartialText, bool IsFinal, TimeSpan Timestamp);
