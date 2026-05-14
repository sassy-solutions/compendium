// -----------------------------------------------------------------------
// <copyright file="VectorMatchTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Tests.Models;

public class VectorMatchTests
{
    [Fact]
    public void VectorMatch_Construct_AssignsAllProperties()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["source"] = "wiki" };

        // Act
        var match = new VectorMatch("doc-9", 0.87f, metadata, "tenant-x");

        // Assert
        match.Id.Should().Be("doc-9");
        match.Score.Should().Be(0.87f);
        match.Metadata.Should().BeSameAs(metadata);
        match.TenantId.Should().Be("tenant-x");
    }

    [Fact]
    public void VectorMatch_TenantId_DefaultsToNull()
    {
        // Arrange / Act
        var match = new VectorMatch("doc-1", 0f, new Dictionary<string, object>());

        // Assert
        match.TenantId.Should().BeNull();
    }

    [Fact]
    public void VectorMatch_RecordEquality_HoldsForSameComponents()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["k"] = "v" };

        // Act
        var first = new VectorMatch("id", 0.5f, metadata, "t");
        var second = new VectorMatch("id", 0.5f, metadata, "t");

        // Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void VectorMatch_RecordEquality_DiffersOnScore()
    {
        // Arrange
        var metadata = new Dictionary<string, object>();

        // Act
        var first = new VectorMatch("id", 0.5f, metadata);
        var second = new VectorMatch("id", 0.6f, metadata);

        // Assert
        first.Should().NotBe(second);
    }
}
