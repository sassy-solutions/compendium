// -----------------------------------------------------------------------
// <copyright file="AudioFormatTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class AudioFormatTests
{
    [Fact]
    public void AudioFormat_DefinesExpectedMembers()
    {
        // Act
        var names = Enum.GetNames<AudioFormat>();

        // Assert
        names.Should().BeEquivalentTo(new[] { "Mp3", "Wav", "Opus", "Flac", "Pcm16" });
    }

    [Theory]
    [InlineData(AudioFormat.Mp3, 0)]
    [InlineData(AudioFormat.Wav, 1)]
    [InlineData(AudioFormat.Opus, 2)]
    [InlineData(AudioFormat.Flac, 3)]
    [InlineData(AudioFormat.Pcm16, 4)]
    public void AudioFormat_NumericValues_AreStable(AudioFormat format, int expected)
    {
        // Act
        var value = (int)format;

        // Assert
        value.Should().Be(expected);
    }

    [Fact]
    public void AudioFormat_DefaultValue_IsMp3()
    {
        // Arrange
        AudioFormat defaultValue = default;

        // Act / Assert
        defaultValue.Should().Be(AudioFormat.Mp3);
    }
}
