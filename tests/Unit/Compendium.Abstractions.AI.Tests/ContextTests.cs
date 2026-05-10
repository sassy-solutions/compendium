// -----------------------------------------------------------------------
// <copyright file="ContextTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.AI.Tests;

public sealed class ContextRequestTests
{
    [Fact]
    public void ContextRequest_WithDefaults_HasAllNullProperties()
    {
        // Arrange & Act
        var request = new ContextRequest();

        // Assert
        request.UserId.Should().BeNull();
        request.TenantId.Should().BeNull();
        request.RequestType.Should().BeNull();
        request.IncludeContextTypes.Should().BeNull();
        request.ExcludeContextTypes.Should().BeNull();
        request.MaxTokens.Should().BeNull();
        request.Parameters.Should().BeNull();
    }

    [Fact]
    public void ContextRequest_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange
        var include = new[] { "user_preferences", "conversation_history" };
        var exclude = new[] { "system_state" };
        var parameters = new Dictionary<string, object>
        {
            ["topK"] = 5,
            ["locale"] = "fr-FR",
        };

        // Act
        var request = new ContextRequest
        {
            UserId = "user-1",
            TenantId = "tenant-1",
            RequestType = "chat",
            IncludeContextTypes = include,
            ExcludeContextTypes = exclude,
            MaxTokens = 4096,
            Parameters = parameters,
        };

        // Assert
        request.UserId.Should().Be("user-1");
        request.TenantId.Should().Be("tenant-1");
        request.RequestType.Should().Be("chat");
        request.IncludeContextTypes.Should().BeEquivalentTo(include);
        request.ExcludeContextTypes.Should().BeEquivalentTo(exclude);
        request.MaxTokens.Should().Be(4096);
        request.Parameters.Should().BeSameAs(parameters);
    }

    [Theory]
    [InlineData("chat")]
    [InlineData("analysis")]
    [InlineData("generation")]
    [InlineData("")]
    public void ContextRequest_RequestType_RoundTripUnchanged(string requestType)
    {
        // Arrange & Act
        var request = new ContextRequest { RequestType = requestType };

        // Assert
        request.RequestType.Should().Be(requestType);
    }

    [Fact]
    public void ContextRequest_RecordEquality_IsValueBased()
    {
        // Arrange
        var a = new ContextRequest { UserId = "u1", TenantId = "t1" };
        var b = new ContextRequest { UserId = "u1", TenantId = "t1" };

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}

public sealed class ContextResultTests
{
    [Fact]
    public void ContextResult_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange
        var pieces = new[]
        {
            new ContextData { Type = "user", Content = "user data" },
            new ContextData { Type = "history", Content = "past chat" },
        };

        // Act
        var result = new ContextResult
        {
            Context = "user data\npast chat",
            ContextPieces = pieces,
        };

        // Assert
        result.Context.Should().Be("user data\npast chat");
        result.ContextPieces.Should().BeEquivalentTo(pieces);
        result.EstimatedTokens.Should().Be(0);
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void ContextResult_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["providers_used"] = 3,
            ["truncated"] = false,
        };

        // Act
        var result = new ContextResult
        {
            Context = "ctx",
            ContextPieces = Array.Empty<ContextData>(),
            EstimatedTokens = 256,
            Metadata = metadata,
        };

        // Assert
        result.EstimatedTokens.Should().Be(256);
        result.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void ContextResult_RecordWith_PreservesOtherFields()
    {
        // Arrange
        var original = new ContextResult
        {
            Context = "a",
            ContextPieces = Array.Empty<ContextData>(),
            EstimatedTokens = 10,
        };

        // Act
        var modified = original with { EstimatedTokens = 20 };

        // Assert
        modified.Context.Should().Be(original.Context);
        modified.EstimatedTokens.Should().Be(20);
        modified.Should().NotBe(original);
    }
}

public sealed class ContextDataTests
{
    [Fact]
    public void ContextData_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var data = new ContextData
        {
            Type = "user_preferences",
            Content = "{ \"locale\": \"en-US\" }",
        };

        // Assert
        data.Type.Should().Be("user_preferences");
        data.Content.Should().Be("{ \"locale\": \"en-US\" }");
        data.Priority.Should().Be(0);
        data.EstimatedTokens.Should().Be(0);
        data.Source.Should().BeNull();
    }

    [Fact]
    public void ContextData_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var data = new ContextData
        {
            Type = "history",
            Content = "previous turns",
            Priority = 100,
            EstimatedTokens = 64,
            Source = "database",
        };

        // Assert
        data.Type.Should().Be("history");
        data.Content.Should().Be("previous turns");
        data.Priority.Should().Be(100);
        data.EstimatedTokens.Should().Be(64);
        data.Source.Should().Be("database");
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void ContextData_Priority_RoundTripUnchanged(int priority)
    {
        // Arrange & Act
        var data = new ContextData { Type = "t", Content = "c", Priority = priority };

        // Assert
        data.Priority.Should().Be(priority);
    }

    [Theory]
    [InlineData("database")]
    [InlineData("cache")]
    [InlineData("api")]
    public void ContextData_Source_RoundTripUnchanged(string source)
    {
        // Arrange & Act
        var data = new ContextData { Type = "t", Content = "c", Source = source };

        // Assert
        data.Source.Should().Be(source);
    }

    [Fact]
    public void ContextData_RecordEquality_IsValueBased()
    {
        // Arrange
        var a = new ContextData { Type = "t", Content = "c", Priority = 5, Source = "s" };
        var b = new ContextData { Type = "t", Content = "c", Priority = 5, Source = "s" };

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
