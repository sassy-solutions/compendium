// -----------------------------------------------------------------------
// <copyright file="EmbeddingModelsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests.Models;

public sealed class EmbeddingRequestTests
{
    [Fact]
    public void EmbeddingRequest_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange
        var inputs = new List<string> { "hello", "world" };

        // Act
        var request = new EmbeddingRequest
        {
            Model = "openai/text-embedding-3-small",
            Inputs = inputs,
        };

        // Assert
        request.Model.Should().Be("openai/text-embedding-3-small");
        request.Inputs.Should().BeEquivalentTo(new[] { "hello", "world" });
        request.Dimensions.Should().BeNull();
        request.TenantId.Should().BeNull();
        request.UserId.Should().BeNull();
    }

    [Fact]
    public void EmbeddingRequest_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var request = new EmbeddingRequest
        {
            Model = "openai/text-embedding-3-large",
            Inputs = new[] { "alpha" },
            Dimensions = 1024,
            TenantId = "tenant-1",
            UserId = "user-7",
        };

        // Assert
        request.Dimensions.Should().Be(1024);
        request.TenantId.Should().Be("tenant-1");
        request.UserId.Should().Be("user-7");
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(3072)]
    public void EmbeddingRequest_Dimensions_RoundTripUnchanged(int dimensions)
    {
        // Arrange & Act
        var request = new EmbeddingRequest
        {
            Model = "m",
            Inputs = new[] { "x" },
            Dimensions = dimensions,
        };

        // Assert
        request.Dimensions.Should().Be(dimensions);
    }

    [Fact]
    public void EmbeddingRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var inputs = new[] { "a", "b" };
        var a = new EmbeddingRequest { Model = "m", Inputs = inputs };
        var b = new EmbeddingRequest { Model = "m", Inputs = inputs };

        // Act & Assert
        a.Should().Be(b);
    }
}

public sealed class EmbeddingTests
{
    [Fact]
    public void Embedding_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange
        var vector = new[] { 0.1f, 0.2f, 0.3f };

        // Act
        var embedding = new Embedding
        {
            Index = 0,
            Vector = vector,
        };

        // Assert
        embedding.Index.Should().Be(0);
        embedding.Vector.Should().BeEquivalentTo(vector);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Embedding_Index_RoundTripUnchanged(int index)
    {
        // Arrange & Act
        var embedding = new Embedding
        {
            Index = index,
            Vector = new[] { 1.0f },
        };

        // Assert
        embedding.Index.Should().Be(index);
    }

    [Fact]
    public void Embedding_Vector_PreservesAllValues()
    {
        // Arrange
        var vector = Enumerable.Range(0, 1536).Select(i => (float)i / 1536f).ToArray();

        // Act
        var embedding = new Embedding { Index = 0, Vector = vector };

        // Assert
        embedding.Vector.Should().HaveCount(1536);
        embedding.Vector[0].Should().Be(0f);
        embedding.Vector[^1].Should().Be(1535f / 1536f);
    }
}

public sealed class EmbeddingResponseTests
{
    [Fact]
    public void EmbeddingResponse_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange
        var embeddings = new[]
        {
            new Embedding { Index = 0, Vector = new[] { 0.1f, 0.2f } },
            new Embedding { Index = 1, Vector = new[] { 0.3f, 0.4f } },
        };
        var usage = new EmbeddingUsage { PromptTokens = 8 };

        // Act
        var response = new EmbeddingResponse
        {
            Model = "openai/text-embedding-3-small",
            Embeddings = embeddings,
            Usage = usage,
        };

        // Assert
        response.Model.Should().Be("openai/text-embedding-3-small");
        response.Embeddings.Should().HaveCount(2);
        response.Embeddings[1].Index.Should().Be(1);
        response.Usage.Should().BeSameAs(usage);
    }

    [Fact]
    public void EmbeddingResponse_RecordEquality_DiffersWhenModelChanges()
    {
        // Arrange
        var usage = new EmbeddingUsage { PromptTokens = 1 };
        var a = new EmbeddingResponse
        {
            Model = "m1",
            Embeddings = Array.Empty<Embedding>(),
            Usage = usage,
        };

        // Act
        var b = a with { Model = "m2" };

        // Assert
        b.Should().NotBe(a);
        b.Model.Should().Be("m2");
    }
}

public sealed class EmbeddingUsageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(int.MaxValue)]
    public void EmbeddingUsage_TotalTokens_EqualsPromptTokens(int promptTokens)
    {
        // Arrange & Act
        var usage = new EmbeddingUsage { PromptTokens = promptTokens };

        // Assert
        usage.TotalTokens.Should().Be(promptTokens);
    }

    [Fact]
    public void EmbeddingUsage_WithEstimatedCost_PreservesCost()
    {
        // Arrange & Act
        var usage = new EmbeddingUsage
        {
            PromptTokens = 1000,
            EstimatedCostUsd = 0.0001m,
        };

        // Assert
        usage.PromptTokens.Should().Be(1000);
        usage.TotalTokens.Should().Be(1000);
        usage.EstimatedCostUsd.Should().Be(0.0001m);
    }

    [Fact]
    public void EmbeddingUsage_WithoutCost_LeavesCostNull()
    {
        // Arrange & Act
        var usage = new EmbeddingUsage { PromptTokens = 5 };

        // Assert
        usage.EstimatedCostUsd.Should().BeNull();
    }
}
