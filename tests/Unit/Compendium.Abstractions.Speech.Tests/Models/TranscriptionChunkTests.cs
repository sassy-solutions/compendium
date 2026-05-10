// -----------------------------------------------------------------------
// <copyright file="TranscriptionChunkTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class TranscriptionChunkTests
{
    [Fact]
    public void TranscriptionChunk_Constructor_PreservesValues()
    {
        // Arrange
        var timestamp = TimeSpan.FromMilliseconds(250);

        // Act
        var chunk = new TranscriptionChunk("hello", IsFinal: false, timestamp);

        // Assert
        chunk.PartialText.Should().Be("hello");
        chunk.IsFinal.Should().BeFalse();
        chunk.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TranscriptionChunk_WithIsFinal_PreservesFlag(bool isFinal)
    {
        // Act
        var chunk = new TranscriptionChunk("text", isFinal, TimeSpan.Zero);

        // Assert
        chunk.IsFinal.Should().Be(isFinal);
    }

    [Fact]
    public void TranscriptionChunk_RecordEquality_TwoIdentical_AreEqual()
    {
        // Arrange
        var first = new TranscriptionChunk("hi", true, TimeSpan.FromSeconds(1));
        var second = new TranscriptionChunk("hi", true, TimeSpan.FromSeconds(1));

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void TranscriptionChunk_RecordEquality_DifferingPartialText_AreNotEqual()
    {
        // Arrange
        var first = new TranscriptionChunk("a", false, TimeSpan.Zero);
        var second = new TranscriptionChunk("b", false, TimeSpan.Zero);

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void TranscriptionChunk_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new TranscriptionChunk("partial", false, TimeSpan.Zero);

        // Act
        var updated = original with { PartialText = "complete", IsFinal = true };

        // Assert
        updated.PartialText.Should().Be("complete");
        updated.IsFinal.Should().BeTrue();
        original.PartialText.Should().Be("partial");
        original.IsFinal.Should().BeFalse();
    }
}
