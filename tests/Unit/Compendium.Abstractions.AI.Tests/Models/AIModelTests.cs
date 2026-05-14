// -----------------------------------------------------------------------
// <copyright file="AIModelTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests.Models;

public sealed class AIModelTests
{
    [Fact]
    public void AIModel_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var model = new AIModel
        {
            Id = "anthropic/claude-3.5-sonnet",
            Name = "Claude 3.5 Sonnet",
            Provider = "anthropic",
        };

        // Assert
        model.Id.Should().Be("anthropic/claude-3.5-sonnet");
        model.Name.Should().Be("Claude 3.5 Sonnet");
        model.Provider.Should().Be("anthropic");
        model.SupportsStreaming.Should().BeTrue("streaming is enabled by default");
        model.SupportsEmbeddings.Should().BeFalse();
        model.SupportsVision.Should().BeFalse();
        model.SupportsTools.Should().BeFalse();
        model.ContextWindow.Should().BeNull();
        model.MaxOutputTokens.Should().BeNull();
        model.PricingInputPerMillion.Should().BeNull();
        model.PricingOutputPerMillion.Should().BeNull();
        model.Metadata.Should().BeNull();
    }

    [Fact]
    public void AIModel_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["family"] = "claude",
            ["release"] = "2024-10",
        };

        // Act
        var model = new AIModel
        {
            Id = "openai/gpt-4o",
            Name = "GPT-4o",
            Provider = "openai",
            ContextWindow = 128_000,
            MaxOutputTokens = 4_096,
            SupportsStreaming = true,
            SupportsEmbeddings = false,
            SupportsVision = true,
            SupportsTools = true,
            PricingInputPerMillion = 2.50m,
            PricingOutputPerMillion = 10.00m,
            Metadata = metadata,
        };

        // Assert
        model.Id.Should().Be("openai/gpt-4o");
        model.Name.Should().Be("GPT-4o");
        model.Provider.Should().Be("openai");
        model.ContextWindow.Should().Be(128_000);
        model.MaxOutputTokens.Should().Be(4_096);
        model.SupportsStreaming.Should().BeTrue();
        model.SupportsEmbeddings.Should().BeFalse();
        model.SupportsVision.Should().BeTrue();
        model.SupportsTools.Should().BeTrue();
        model.PricingInputPerMillion.Should().Be(2.50m);
        model.PricingOutputPerMillion.Should().Be(10.00m);
        model.Metadata.Should().BeSameAs(metadata);
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, true, true)]
    public void AIModel_CapabilityFlags_RoundTripUnchanged(
        bool streaming,
        bool embeddings,
        bool vision,
        bool tools)
    {
        // Arrange & Act
        var model = new AIModel
        {
            Id = "id",
            Name = "name",
            Provider = "p",
            SupportsStreaming = streaming,
            SupportsEmbeddings = embeddings,
            SupportsVision = vision,
            SupportsTools = tools,
        };

        // Assert
        model.SupportsStreaming.Should().Be(streaming);
        model.SupportsEmbeddings.Should().Be(embeddings);
        model.SupportsVision.Should().Be(vision);
        model.SupportsTools.Should().Be(tools);
    }

    [Fact]
    public void AIModel_RecordEquality_IsValueBased()
    {
        // Arrange
        var a = new AIModel { Id = "x", Name = "X", Provider = "p" };
        var b = new AIModel { Id = "x", Name = "X", Provider = "p" };

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void AIModel_RecordWith_ChangesProvider()
    {
        // Arrange
        var original = new AIModel { Id = "x", Name = "X", Provider = "anthropic" };

        // Act
        var changed = original with { Provider = "openai" };

        // Assert
        changed.Provider.Should().Be("openai");
        changed.Id.Should().Be(original.Id);
        changed.Should().NotBe(original);
    }
}
