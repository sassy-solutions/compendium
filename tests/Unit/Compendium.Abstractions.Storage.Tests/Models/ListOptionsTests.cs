// -----------------------------------------------------------------------
// <copyright file="ListOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Tests.Models;

public class ListOptionsTests
{
    [Fact]
    public void ListOptions_Defaults_PrefixNull_MaxKeysOneThousand_TokenNull()
    {
        // Act
        var options = new ListOptions();

        // Assert
        options.Prefix.Should().BeNull();
        options.MaxKeys.Should().Be(1000);
        options.ContinuationToken.Should().BeNull();
    }

    [Fact]
    public void ListOptions_Constructor_StoresAllValues()
    {
        // Act
        var options = new ListOptions(Prefix: "tenants/t-1/", MaxKeys: 50, ContinuationToken: "tok-42");

        // Assert
        options.Prefix.Should().Be("tenants/t-1/");
        options.MaxKeys.Should().Be(50);
        options.ContinuationToken.Should().Be("tok-42");
    }

    [Fact]
    public void ListOptions_Equality_IdenticalRecords_AreEqual()
    {
        // Arrange
        var a = new ListOptions("p", 10, "t");
        var b = new ListOptions("p", 10, "t");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ListOptions_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new ListOptions("p", 10, "t1");

        // Act
        var copy = original with { ContinuationToken = "t2" };

        // Assert
        copy.Prefix.Should().Be("p");
        copy.MaxKeys.Should().Be(10);
        copy.ContinuationToken.Should().Be("t2");
        copy.Should().NotBe(original);
    }

    [Fact]
    public void ListOptions_ToString_IncludesFieldNames()
    {
        // Arrange
        var options = new ListOptions("p", 10, "t");

        // Act
        var text = options.ToString();

        // Assert
        text.Should().Contain("Prefix");
        text.Should().Contain("MaxKeys");
        text.Should().Contain("ContinuationToken");
    }

    [Fact]
    public void ListPage_Constructor_StoresItemsAndToken()
    {
        // Arrange
        var items = new List<ObjectInfo>
        {
            new("k1", 1, "e1", null, DateTimeOffset.UnixEpoch),
            new("k2", 2, "e2", null, DateTimeOffset.UnixEpoch),
        };

        // Act
        var page = new ListPage(items, "next-tok");

        // Assert
        page.Items.Should().BeSameAs(items);
        page.NextContinuationToken.Should().Be("next-tok");
    }

    [Fact]
    public void ListPage_Constructor_DefaultsTokenToNull()
    {
        // Arrange
        var items = Array.Empty<ObjectInfo>();

        // Act
        var page = new ListPage(items);

        // Assert
        page.Items.Should().BeSameAs(items);
        page.NextContinuationToken.Should().BeNull();
    }

    [Fact]
    public void ListPage_Equality_SameItemsReferenceAndToken_AreEqual()
    {
        // Arrange
        var items = new List<ObjectInfo>();
        var a = new ListPage(items, "t");
        var b = new ListPage(items, "t");

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ListPage_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new ListPage(Array.Empty<ObjectInfo>(), "t1");

        // Act
        var copy = original with { NextContinuationToken = "t2" };

        // Assert
        copy.NextContinuationToken.Should().Be("t2");
        copy.Should().NotBe(original);
    }

    [Fact]
    public void ListPage_ToString_IncludesFieldNames()
    {
        // Arrange
        var page = new ListPage(Array.Empty<ObjectInfo>(), "t");

        // Act
        var text = page.ToString();

        // Assert
        text.Should().Contain("Items");
        text.Should().Contain("NextContinuationToken");
    }
}
