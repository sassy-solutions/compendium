// -----------------------------------------------------------------------
// <copyright file="SpeechErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech;

/// <summary>
/// Provides standard error definitions for speech-to-text operations.
/// </summary>
public static class SpeechErrors
{
    /// <summary>
    /// Error returned when the supplied audio MIME type or codec is not supported by the adapter.
    /// </summary>
    public static Error UnsupportedFormat(string mimeType) =>
        Error.Validation("Speech.UnsupportedFormat", $"The audio format '{mimeType}' is not supported.");

    /// <summary>
    /// Error returned when the audio payload exceeds the adapter's maximum duration.
    /// </summary>
    public static Error AudioTooLong(TimeSpan duration, TimeSpan maximum) =>
        Error.Validation(
            "Speech.AudioTooLong",
            $"The audio duration {duration.TotalSeconds:0.##}s exceeds the maximum of {maximum.TotalSeconds:0.##}s.");

    /// <summary>
    /// Error returned when the speech-to-text provider cannot be reached.
    /// </summary>
    public static Error ProviderUnreachable(string reason) =>
        Error.Unavailable("Speech.ProviderUnreachable", $"The speech-to-text provider is unreachable: {reason}");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static Error RateLimited(string reason) =>
        Error.TooManyRequests("Speech.RateLimited", $"Rate limit exceeded: {reason}");
}
