// -----------------------------------------------------------------------
// <copyright file="ITextToSpeech.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Speech.Models;

namespace Compendium.Abstractions.Speech;

/// <summary>
/// Provides text-to-speech synthesis operations.
/// This interface is provider-agnostic and can be implemented by adapters targeting
/// ElevenLabs, Azure Speech, Google Cloud TTS, OpenAI, or similar engines.
/// </summary>
public interface ITextToSpeech
{
    /// <summary>
    /// Synthesizes the supplied text into a complete audio payload in a single batch request.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="opts">The synthesis tuning options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the synthesized audio output or an error.</returns>
    Task<Result<AudioOutput>> SynthesizeAsync(string text, SynthesisOptions opts, CancellationToken ct);

    /// <summary>
    /// Synthesizes the supplied text into a sequence of audio chunks, emitting them incrementally as the provider streams them.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="opts">The synthesis tuning options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous sequence of audio chunks emitted by the provider.</returns>
    IAsyncEnumerable<AudioChunk> SynthesizeStreamAsync(string text, SynthesisOptions opts, CancellationToken ct);
}
