// -----------------------------------------------------------------------
// <copyright file="ISpeechToText.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Speech.Models;

namespace Compendium.Abstractions.Speech;

/// <summary>
/// Provides speech-to-text transcription operations.
/// This interface is provider-agnostic and can be implemented by adapters targeting
/// Whisper, Deepgram, Azure Speech, or similar engines.
/// </summary>
public interface ISpeechToText
{
    /// <summary>
    /// Transcribes the supplied audio payload in a single batch request.
    /// </summary>
    /// <param name="audio">The audio payload to transcribe.</param>
    /// <param name="opts">The transcription tuning options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the finalized transcription or an error.</returns>
    Task<Result<TranscriptionResult>> TranscribeAsync(AudioInput audio, TranscriptionOptions opts, CancellationToken ct);

    /// <summary>
    /// Transcribes a streaming sequence of audio chunks, emitting incremental updates as they arrive.
    /// </summary>
    /// <param name="audio">The asynchronous sequence of audio chunks composing the live stream.</param>
    /// <param name="opts">The transcription tuning options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous sequence of transcription chunks emitted by the provider.</returns>
    IAsyncEnumerable<TranscriptionChunk> TranscribeStreamAsync(IAsyncEnumerable<AudioChunk> audio, TranscriptionOptions opts, CancellationToken ct);
}
