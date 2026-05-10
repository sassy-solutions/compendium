// -----------------------------------------------------------------------
// <copyright file="TranscriptionResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class TranscriptionResultTests
{
    [Fact]
    public void TranscriptionResult_Constructor_PreservesValues()
    {
        // Arrange
        var segments = new List<TranscriptionSegment>
        {
            new("hello", TimeSpan.Zero, TimeSpan.FromSeconds(1), null),
            new("world", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "s1"),
        };

        // Act
        var result = new TranscriptionResult("hello world", segments);

        // Assert
        result.Text.Should().Be("hello world");
        result.Segments.Should().HaveCount(2);
        result.Segments.Should().BeSameAs(segments);
    }

    [Fact]
    public void TranscriptionResult_WithEmptySegments_HasNoSegments()
    {
        // Act
        var result = new TranscriptionResult(string.Empty, Array.Empty<TranscriptionSegment>());

        // Assert
        result.Text.Should().BeEmpty();
        result.Segments.Should().BeEmpty();
    }

    [Fact]
    public void TranscriptionResult_RecordEquality_SameSegmentsRef_AreEqual()
    {
        // Arrange
        IReadOnlyList<TranscriptionSegment> segments = new List<TranscriptionSegment>
        {
            new("hi", TimeSpan.Zero, TimeSpan.FromSeconds(1), null),
        };
        var first = new TranscriptionResult("hi", segments);
        var second = new TranscriptionResult("hi", segments);

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void TranscriptionResult_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new TranscriptionResult("a", Array.Empty<TranscriptionSegment>());
        var newSegments = new[] { new TranscriptionSegment("b", TimeSpan.Zero, TimeSpan.FromSeconds(1), null) };

        // Act
        var updated = original with { Text = "b", Segments = newSegments };

        // Assert
        updated.Text.Should().Be("b");
        updated.Segments.Should().BeSameAs(newSegments);
        original.Text.Should().Be("a");
        original.Segments.Should().BeEmpty();
    }
}
