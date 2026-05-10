// -----------------------------------------------------------------------
// <copyright file="VectorRecordTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Tests.Models;

public class VectorRecordTests
{
    [Fact]
    public void VectorRecord_Construct_AssignsAllProperties()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>(new[] { 0.1f, 0.2f, 0.3f });
        var metadata = new Dictionary<string, object> { ["k"] = "v" };

        // Act
        var record = new VectorRecord("doc-1", embedding, metadata, "tenant-1");

        // Assert
        record.Id.Should().Be("doc-1");
        record.Embedding.ToArray().Should().Equal(0.1f, 0.2f, 0.3f);
        record.Metadata.Should().BeSameAs(metadata);
        record.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void VectorRecord_TenantId_DefaultsToNull()
    {
        // Arrange
        var metadata = new Dictionary<string, object>();

        // Act
        var record = new VectorRecord("doc-1", ReadOnlyMemory<float>.Empty, metadata);

        // Assert
        record.TenantId.Should().BeNull();
    }

    [Fact]
    public void VectorRecord_RecordEquality_HoldsForSameComponents()
    {
        // Arrange
        var embedding = new ReadOnlyMemory<float>(new[] { 1f });
        var metadata = new Dictionary<string, object> { ["a"] = 1 };

        // Act
        var first = new VectorRecord("id", embedding, metadata, "t");
        var second = new VectorRecord("id", embedding, metadata, "t");

        // Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void VectorRecord_RecordEquality_DiffersOnId()
    {
        // Arrange
        var embedding = ReadOnlyMemory<float>.Empty;
        var metadata = new Dictionary<string, object>();

        // Act
        var first = new VectorRecord("id-a", embedding, metadata);
        var second = new VectorRecord("id-b", embedding, metadata);

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void VectorRecord_With_OverridesTenantId()
    {
        // Arrange
        var record = new VectorRecord("id", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>(), "t-1");

        // Act
        var rotated = record with { TenantId = "t-2" };

        // Assert
        rotated.TenantId.Should().Be("t-2");
        record.TenantId.Should().Be("t-1");
    }
}
