// -----------------------------------------------------------------------
// <copyright file="ObjectInfoTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests.Models;

public class ObjectInfoTests
{
    private static ObjectInfo Sample(
        string key = "tenants/t-1/file.png",
        long size = 1024,
        string etag = "abc",
        string? contentType = "image/png",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new ObjectInfo(
            key,
            size,
            etag,
            contentType,
            new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero),
            metadata);
    }

    [Fact]
    public void ObjectInfo_Constructor_StoresAllValues()
    {
        // Arrange
        var lastModified = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var metadata = new Dictionary<string, string> { ["owner"] = "alice" };

        // Act
        var info = new ObjectInfo("k", 42, "etag-1", "text/plain", lastModified, metadata);

        // Assert
        info.Key.Should().Be("k");
        info.Size.Should().Be(42);
        info.ETag.Should().Be("etag-1");
        info.ContentType.Should().Be("text/plain");
        info.LastModified.Should().Be(lastModified);
        info.Metadata.Should().BeSameAs(metadata);
    }

    [Fact]
    public void ObjectInfo_Metadata_DefaultsToNull()
    {
        // Act
        var info = new ObjectInfo("k", 0, "e", null, DateTimeOffset.UnixEpoch);

        // Assert
        info.ContentType.Should().BeNull();
        info.Metadata.Should().BeNull();
    }

    [Fact]
    public void ObjectInfo_Equality_TwoIdenticalRecords_AreEqual()
    {
        // Arrange
        var a = Sample();
        var b = Sample();

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ObjectInfo_Equality_DifferentKey_AreNotEqual()
    {
        // Arrange
        var a = Sample(key: "a");
        var b = Sample(key: "b");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void ObjectInfo_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = Sample();

        // Act
        var copy = original with { Size = 9999, ETag = "new-etag" };

        // Assert
        copy.Should().NotBe(original);
        copy.Size.Should().Be(9999);
        copy.ETag.Should().Be("new-etag");
        copy.Key.Should().Be(original.Key);
        copy.LastModified.Should().Be(original.LastModified);
    }

    [Fact]
    public void ObjectInfo_ToString_IncludesAllFieldNames()
    {
        // Arrange
        var info = Sample();

        // Act
        var text = info.ToString();

        // Assert
        text.Should().Contain("Key").And.Contain("Size").And.Contain("ETag");
        text.Should().Contain("ContentType").And.Contain("LastModified").And.Contain("Metadata");
    }
}
