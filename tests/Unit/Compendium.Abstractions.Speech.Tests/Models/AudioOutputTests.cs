// -----------------------------------------------------------------------
// <copyright file="AudioOutputTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;

namespace Compendium.Abstractions.Speech.Tests.Models;

public class AudioOutputTests
{
    [Fact]
    public void AudioOutput_Constructor_PreservesValues()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var duration = TimeSpan.FromSeconds(2.5);

        // Act
        var output = new AudioOutput(stream, "audio/mpeg", duration);

        // Assert
        output.Stream.Should().BeSameAs(stream);
        output.MimeType.Should().Be("audio/mpeg");
        output.Duration.Should().Be(duration);
    }

    [Fact]
    public void AudioOutput_RecordEquality_SameReferences_AreEqual()
    {
        // Arrange
        using var stream = new MemoryStream();
        var duration = TimeSpan.FromMilliseconds(500);
        var a = new AudioOutput(stream, "audio/wav", duration);
        var b = new AudioOutput(stream, "audio/wav", duration);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void AudioOutput_RecordEquality_DifferingMimeType_AreNotEqual()
    {
        // Arrange
        using var stream = new MemoryStream();
        var duration = TimeSpan.FromSeconds(1);
        var a = new AudioOutput(stream, "audio/mpeg", duration);
        var b = new AudioOutput(stream, "audio/wav", duration);

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void AudioOutput_With_ProducesModifiedCopy()
    {
        // Arrange
        using var stream = new MemoryStream();
        var original = new AudioOutput(stream, "audio/mpeg", TimeSpan.FromSeconds(1));

        // Act
        var updated = original with { MimeType = "audio/wav", Duration = TimeSpan.FromSeconds(2) };

        // Assert
        updated.Stream.Should().BeSameAs(stream);
        updated.MimeType.Should().Be("audio/wav");
        updated.Duration.Should().Be(TimeSpan.FromSeconds(2));
        original.MimeType.Should().Be("audio/mpeg");
    }
}
