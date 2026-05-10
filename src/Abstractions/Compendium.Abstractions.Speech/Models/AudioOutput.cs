// -----------------------------------------------------------------------
// <copyright file="AudioOutput.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Models;

/// <summary>
/// Represents the synthesized audio payload returned by a text-to-speech provider.
/// </summary>
/// <param name="Stream">The audio stream containing the synthesized payload. The caller takes ownership and is responsible for disposing it.</param>
/// <param name="MimeType">The MIME type associated with <paramref name="Stream"/> (for example <c>audio/mpeg</c> or <c>audio/wav</c>).</param>
/// <param name="Duration">The total duration of the synthesized audio.</param>
public sealed record AudioOutput(Stream Stream, string MimeType, TimeSpan Duration);
