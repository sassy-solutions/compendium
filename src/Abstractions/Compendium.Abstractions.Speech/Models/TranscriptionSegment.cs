// -----------------------------------------------------------------------
// <copyright file="TranscriptionSegment.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents a single, time-aligned segment of a finalized transcription.
/// </summary>
/// <param name="Text">The transcribed text for the segment.</param>
/// <param name="Start">The segment start time relative to the beginning of the audio.</param>
/// <param name="End">The segment end time relative to the beginning of the audio.</param>
/// <param name="Speaker">Optional speaker label when diarization is enabled.</param>
public sealed record TranscriptionSegment(string Text, TimeSpan Start, TimeSpan End, string? Speaker);
