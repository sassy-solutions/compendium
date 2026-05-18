// -----------------------------------------------------------------------
// <copyright file="ObjectMetadataTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests.Models;

public class ObjectMetadataTests
{
    [Fact]
    public void ObjectMetadata_DefaultConstructor_AllFieldsNull()
    {
        // Act
        var metadata = new ObjectMetadata();

        // Assert
        metadata.ContentType.Should().BeNull();
        metadata.CacheControl.Should().BeNull();
        metadata.ContentDisposition.Should().BeNull();
        metadata.Custom.Should().BeNull();
    }

    [Fact]
    public void ObjectMetadata_Constructor_StoresAllValues()
    {
        // Arrange
        var custom = new Dictionary<string, string> { ["x-owner"] = "bob" };

        // Act
        var metadata = new ObjectMetadata(
            ContentType: "application/pdf",
            CacheControl: "max-age=3600",
            ContentDisposition: "attachment; filename=foo.pdf",
            Custom: custom);

        // Assert
        metadata.ContentType.Should().Be("application/pdf");
        metadata.CacheControl.Should().Be("max-age=3600");
        metadata.ContentDisposition.Should().Be("attachment; filename=foo.pdf");
        metadata.Custom.Should().BeSameAs(custom);
    }

    [Fact]
    public void ObjectMetadata_Equality_IdenticalRecords_AreEqual()
    {
        // Arrange
        var a = new ObjectMetadata("text/plain", "no-cache", "inline");
        var b = new ObjectMetadata("text/plain", "no-cache", "inline");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ObjectMetadata_Equality_DifferentContentType_AreNotEqual()
    {
        // Arrange
        var a = new ObjectMetadata(ContentType: "text/plain");
        var b = new ObjectMetadata(ContentType: "text/html");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void ObjectMetadata_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new ObjectMetadata("text/plain", "no-cache");

        // Act
        var copy = original with { CacheControl = "max-age=60" };

        // Assert
        copy.ContentType.Should().Be("text/plain");
        copy.CacheControl.Should().Be("max-age=60");
        copy.Should().NotBe(original);
    }

    [Fact]
    public void ObjectMetadata_ToString_IncludesFieldNames()
    {
        // Arrange
        var metadata = new ObjectMetadata("text/plain");

        // Act
        var text = metadata.ToString();

        // Assert
        text.Should().Contain("ContentType");
        text.Should().Contain("CacheControl");
        text.Should().Contain("ContentDisposition");
        text.Should().Contain("Custom");
    }
}
