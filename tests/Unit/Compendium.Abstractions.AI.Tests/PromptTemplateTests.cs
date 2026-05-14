// -----------------------------------------------------------------------
// <copyright file="PromptTemplateTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests;

public sealed class PromptTemplateTests
{
    [Fact]
    public void PromptTemplate_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var template = new PromptTemplate
        {
            Key = "support.greeting",
            Name = "Customer-support greeting",
            Template = "Hello {{name}}, how can I help?",
        };

        // Assert
        template.Key.Should().Be("support.greeting");
        template.Name.Should().Be("Customer-support greeting");
        template.Template.Should().Be("Hello {{name}}, how can I help?");
        template.Description.Should().BeNull();
        template.Category.Should().BeNull();
        template.RecommendedModel.Should().BeNull();
        template.RecommendedTemperature.Should().BeNull();
        template.RequiredVariables.Should().BeNull();
        template.OptionalVariables.Should().BeNull();
        template.Version.Should().Be(1, "default version is 1");
        template.TenantId.Should().BeNull();
        template.UpdatedAt.Should().BeNull();
        template.Metadata.Should().BeNull();
    }

    [Fact]
    public void PromptTemplate_CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var template = new PromptTemplate
        {
            Key = "k",
            Name = "n",
            Template = "t",
        };
        var after = DateTime.UtcNow;

        // Assert
        template.CreatedAt.Should().BeOnOrAfter(before);
        template.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void PromptTemplate_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc);
        var required = new[] { "name", "issue" };
        var optional = new Dictionary<string, string>
        {
            ["tone"] = "friendly",
            ["language"] = "en",
        };
        var metadata = new Dictionary<string, object>
        {
            ["author"] = "team-x",
            ["reviewed"] = true,
        };

        // Act
        var template = new PromptTemplate
        {
            Key = "support.refund",
            Name = "Refund response",
            Template = "Hi {{name}}, regarding {{issue}}...",
            Description = "Generates a refund-handling response.",
            Category = "customer-support",
            RecommendedModel = "anthropic/claude-3.5-sonnet",
            RecommendedTemperature = 0.3f,
            RequiredVariables = required,
            OptionalVariables = optional,
            Version = 7,
            TenantId = "tenant-acme",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Metadata = metadata,
        };

        // Assert
        template.Key.Should().Be("support.refund");
        template.Name.Should().Be("Refund response");
        template.Description.Should().Be("Generates a refund-handling response.");
        template.Category.Should().Be("customer-support");
        template.RecommendedModel.Should().Be("anthropic/claude-3.5-sonnet");
        template.RecommendedTemperature.Should().Be(0.3f);
        template.RequiredVariables.Should().BeEquivalentTo(required);
        template.OptionalVariables.Should().BeEquivalentTo(optional);
        template.Version.Should().Be(7);
        template.TenantId.Should().Be("tenant-acme");
        template.CreatedAt.Should().Be(createdAt);
        template.UpdatedAt.Should().Be(updatedAt);
        template.Metadata.Should().BeSameAs(metadata);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void PromptTemplate_Version_RoundTripUnchanged(int version)
    {
        // Arrange & Act
        var template = new PromptTemplate
        {
            Key = "k",
            Name = "n",
            Template = "t",
            Version = version,
        };

        // Assert
        template.Version.Should().Be(version);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void PromptTemplate_RecommendedTemperature_RoundTripUnchanged(float temperature)
    {
        // Arrange & Act
        var template = new PromptTemplate
        {
            Key = "k",
            Name = "n",
            Template = "t",
            RecommendedTemperature = temperature,
        };

        // Assert
        template.RecommendedTemperature.Should().Be(temperature);
    }

    [Fact]
    public void PromptTemplate_RecordEquality_IsValueBased()
    {
        // Arrange
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new PromptTemplate { Key = "k", Name = "n", Template = "t", CreatedAt = createdAt };
        var b = new PromptTemplate { Key = "k", Name = "n", Template = "t", CreatedAt = createdAt };

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void PromptTemplate_RecordWith_BumpsVersionAndUpdatedAt()
    {
        // Arrange
        var original = new PromptTemplate
        {
            Key = "k",
            Name = "n",
            Template = "v1",
            Version = 1,
        };
        var updatedAt = DateTime.UtcNow;

        // Act
        var modified = original with
        {
            Template = "v2",
            Version = 2,
            UpdatedAt = updatedAt,
        };

        // Assert
        modified.Should().NotBe(original);
        modified.Template.Should().Be("v2");
        modified.Version.Should().Be(2);
        modified.UpdatedAt.Should().Be(updatedAt);
        modified.Key.Should().Be(original.Key);
    }

    [Fact]
    public void PromptTemplate_GlobalScope_HasNullTenantId()
    {
        // Arrange & Act
        var template = new PromptTemplate
        {
            Key = "global.greeting",
            Name = "Global greeting",
            Template = "Hello",
        };

        // Assert
        template.TenantId.Should().BeNull("a null TenantId means the prompt is global");
    }
}
