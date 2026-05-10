// -----------------------------------------------------------------------
// <copyright file="TranscriptionSegmentTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class TranscriptionSegmentTests
{
    [Fact]
    public void TranscriptionSegment_Constructor_PreservesValues()
    {
        // Arrange
        var start = TimeSpan.FromSeconds(1);
        var end = TimeSpan.FromSeconds(2.5);

        // Act
        var segment = new TranscriptionSegment("hello world", start, end, "speaker-1");

        // Assert
        segment.Text.Should().Be("hello world");
        segment.Start.Should().Be(start);
        segment.End.Should().Be(end);
        segment.Speaker.Should().Be("speaker-1");
    }

    [Fact]
    public void TranscriptionSegment_WithNullSpeaker_LeavesSpeakerNull()
    {
        // Act
        var segment = new TranscriptionSegment("text", TimeSpan.Zero, TimeSpan.FromSeconds(1), null);

        // Assert
        segment.Speaker.Should().BeNull();
    }

    [Fact]
    public void TranscriptionSegment_RecordEquality_TwoIdentical_AreEqual()
    {
        // Arrange
        var first = new TranscriptionSegment("hi", TimeSpan.Zero, TimeSpan.FromSeconds(1), "s1");
        var second = new TranscriptionSegment("hi", TimeSpan.Zero, TimeSpan.FromSeconds(1), "s1");

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void TranscriptionSegment_RecordEquality_DifferingText_AreNotEqual()
    {
        // Arrange
        var first = new TranscriptionSegment("a", TimeSpan.Zero, TimeSpan.FromSeconds(1), null);
        var second = new TranscriptionSegment("b", TimeSpan.Zero, TimeSpan.FromSeconds(1), null);

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void TranscriptionSegment_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new TranscriptionSegment("hello", TimeSpan.Zero, TimeSpan.FromSeconds(1), null);

        // Act
        var updated = original with { Speaker = "s2", Text = "bonjour" };

        // Assert
        updated.Speaker.Should().Be("s2");
        updated.Text.Should().Be("bonjour");
        original.Speaker.Should().BeNull();
        original.Text.Should().Be("hello");
    }
}
