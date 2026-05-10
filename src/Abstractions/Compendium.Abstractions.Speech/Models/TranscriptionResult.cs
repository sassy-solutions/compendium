// -----------------------------------------------------------------------
// <copyright file="TranscriptionResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents the finalized output of a batch transcription request.
/// </summary>
/// <param name="Text">The full transcript text concatenated across all segments.</param>
/// <param name="Segments">The time-aligned segments composing the transcript.</param>
public sealed record TranscriptionResult(string Text, IReadOnlyList<TranscriptionSegment> Segments);
