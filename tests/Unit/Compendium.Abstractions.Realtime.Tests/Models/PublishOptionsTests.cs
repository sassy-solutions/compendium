// -----------------------------------------------------------------------
// <copyright file="PublishOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Tests.Models;

public class PublishOptionsTests
{
    [Fact]
    public void Ctor_WithTenantIdOnly_DefaultsTtlAndHeadersToNull()
    {
        // Arrange / Act
        var opts = new PublishOptions("tenant-1");

        // Assert
        opts.TenantId.Should().Be("tenant-1");
        opts.Ttl.Should().BeNull();
        opts.Headers.Should().BeNull();
    }

    [Fact]
    public void Ctor_WithAllFields_AssignsAllValues()
    {
        // Arrange
        var headers = new Dictionary<string, string> { ["x-source"] = "tests" };
        var ttl = TimeSpan.FromMinutes(5);

        // Act
        var opts = new PublishOptions("tenant-1", ttl, headers);

        // Assert
        opts.TenantId.Should().Be("tenant-1");
        opts.Ttl.Should().Be(ttl);
        opts.Headers.Should().BeSameAs(headers);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var a = new PublishOptions("tenant-1", TimeSpan.FromSeconds(30));
        var b = new PublishOptions("tenant-1", TimeSpan.FromSeconds(30));

        // Act / Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentTenantId_AreNotEqual()
    {
        // Arrange
        var a = new PublishOptions("tenant-1");
        var b = new PublishOptions("tenant-2");

        // Act / Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void With_ChangesTenantIdWithoutMutatingOriginal()
    {
        // Arrange
        var original = new PublishOptions("tenant-1", TimeSpan.FromSeconds(10));

        // Act
        var modified = original with { TenantId = "tenant-2" };

        // Assert
        original.TenantId.Should().Be("tenant-1");
        modified.TenantId.Should().Be("tenant-2");
        modified.Ttl.Should().Be(TimeSpan.FromSeconds(10));
    }
}
