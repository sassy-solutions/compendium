// -----------------------------------------------------------------------
// <copyright file="SynthesisOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Speech.Tests.Models;

public class SynthesisOptionsTests
{
    [Fact]
    public void SynthesisOptions_WithVoiceIdOnly_AppliesDefaults()
    {
        // Arrange / Act
        var opts = new SynthesisOptions(VoiceId: "voice-1");

        // Assert
        opts.VoiceId.Should().Be("voice-1");
        opts.Model.Should().BeNull();
        opts.Format.Should().Be(AudioFormat.Mp3);
        opts.SampleRate.Should().Be(22050);
        opts.Stability.Should().Be(0.5);
    }

    [Fact]
    public void SynthesisOptions_WithAllProperties_PreservesValues()
    {
        // Arrange / Act
        var opts = new SynthesisOptions(
            VoiceId: "voice-xyz",
            Model: "eleven_multilingual_v2",
            Format: AudioFormat.Wav,
            SampleRate: 44100,
            Stability: 0.85);

        // Assert
        opts.VoiceId.Should().Be("voice-xyz");
        opts.Model.Should().Be("eleven_multilingual_v2");
        opts.Format.Should().Be(AudioFormat.Wav);
        opts.SampleRate.Should().Be(44100);
        opts.Stability.Should().Be(0.85);
    }

    [Theory]
    [InlineData(AudioFormat.Mp3)]
    [InlineData(AudioFormat.Wav)]
    [InlineData(AudioFormat.Opus)]
    [InlineData(AudioFormat.Flac)]
    [InlineData(AudioFormat.Pcm16)]
    public void SynthesisOptions_AcceptsEveryAudioFormat(AudioFormat format)
    {
        // Act
        var opts = new SynthesisOptions("v", Format: format);

        // Assert
        opts.Format.Should().Be(format);
    }

    [Fact]
    public void SynthesisOptions_RecordEquality_TwoIdentical_AreEqual()
    {
        // Arrange
        var a = new SynthesisOptions("v", "m", AudioFormat.Wav, 16000, 0.4);
        var b = new SynthesisOptions("v", "m", AudioFormat.Wav, 16000, 0.4);

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void SynthesisOptions_RecordEquality_DifferingVoiceId_AreNotEqual()
    {
        // Arrange
        var a = new SynthesisOptions("voice-1");
        var b = new SynthesisOptions("voice-2");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void SynthesisOptions_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new SynthesisOptions("voice-1");

        // Act
        var updated = original with { Format = AudioFormat.Opus, Stability = 0.9 };

        // Assert
        updated.VoiceId.Should().Be("voice-1");
        updated.Format.Should().Be(AudioFormat.Opus);
        updated.Stability.Should().Be(0.9);
        original.Format.Should().Be(AudioFormat.Mp3);
        original.Stability.Should().Be(0.5);
    }
}
