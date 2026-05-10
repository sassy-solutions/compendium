// -----------------------------------------------------------------------
// <copyright file="AudioChunkTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class AudioChunkTests
{
    [Fact]
    public void AudioChunk_Constructor_PreservesValues()
    {
        // Arrange
        var bytes = new byte[] { 0x10, 0x20, 0x30 };

        // Act
        var chunk = new AudioChunk(bytes, SampleRate: 16000);

        // Assert
        chunk.Bytes.ToArray().Should().Equal(bytes);
        chunk.SampleRate.Should().Be(16000);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(16000)]
    [InlineData(44100)]
    [InlineData(48000)]
    public void AudioChunk_WithCommonSampleRates_PreservesValue(int sampleRate)
    {
        // Act
        var chunk = new AudioChunk(ReadOnlyMemory<byte>.Empty, sampleRate);

        // Assert
        chunk.SampleRate.Should().Be(sampleRate);
    }

    [Fact]
    public void AudioChunk_WithEmptyBytes_HasZeroLength()
    {
        // Act
        var chunk = new AudioChunk(ReadOnlyMemory<byte>.Empty, 16000);

        // Assert
        chunk.Bytes.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void AudioChunk_RecordEquality_SameBackingArray_AreEqual()
    {
        // Arrange
        var buffer = new byte[] { 1, 2, 3 };
        var memory = new ReadOnlyMemory<byte>(buffer);
        var first = new AudioChunk(memory, 16000);
        var second = new AudioChunk(memory, 16000);

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void AudioChunk_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new AudioChunk(new byte[] { 1 }, 16000);

        // Act
        var updated = original with { SampleRate = 48000 };

        // Assert
        updated.SampleRate.Should().Be(48000);
        original.SampleRate.Should().Be(16000);
    }
}
