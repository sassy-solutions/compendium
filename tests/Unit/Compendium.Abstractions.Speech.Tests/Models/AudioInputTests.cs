// -----------------------------------------------------------------------
// <copyright file="AudioInputTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class AudioInputTests
{
    [Fact]
    public void AudioInput_Constructor_PreservesValues()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var input = new AudioInput(stream, "audio/wav");

        // Assert
        input.Stream.Should().BeSameAs(stream);
        input.MimeType.Should().Be("audio/wav");
    }

    [Fact]
    public void AudioInput_RecordEquality_TwoInstancesWithSameRefs_AreEqual()
    {
        // Arrange
        using var stream = new MemoryStream();
        var first = new AudioInput(stream, "audio/mpeg");
        var second = new AudioInput(stream, "audio/mpeg");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void AudioInput_RecordEquality_DifferentMime_AreNotEqual()
    {
        // Arrange
        using var stream = new MemoryStream();
        var first = new AudioInput(stream, "audio/wav");
        var second = new AudioInput(stream, "audio/mpeg");

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void AudioInput_With_ProducesModifiedCopy()
    {
        // Arrange
        using var stream = new MemoryStream();
        var original = new AudioInput(stream, "audio/wav");

        // Act
        var updated = original with { MimeType = "audio/ogg" };

        // Assert
        updated.MimeType.Should().Be("audio/ogg");
        updated.Stream.Should().BeSameAs(stream);
        original.MimeType.Should().Be("audio/wav");
    }
}
