// -----------------------------------------------------------------------
// <copyright file="AudioInput.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents an audio payload submitted to a speech-to-text provider for batch transcription.
/// </summary>
/// <param name="Stream">The audio stream to transcribe. The caller retains ownership and is responsible for disposal.</param>
/// <param name="MimeType">The MIME type describing the audio container/codec (for example <c>audio/wav</c>, <c>audio/mpeg</c>, <c>audio/ogg</c>).</param>
public sealed record AudioInput(Stream Stream, string MimeType);
