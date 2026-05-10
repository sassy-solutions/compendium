// -----------------------------------------------------------------------
// <copyright file="DocumentInputTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class DocumentInputTests
{
    [Fact]
    public void DocumentInput_WithStreamAndMimeType_ExposesProperties()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        // Act
        var input = new DocumentInput(stream, "application/pdf");

        // Assert
        input.Stream.Should().BeSameAs(stream);
        input.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public void DocumentInput_WithSameValues_ShouldBeEqualByValue()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var a = new DocumentInput(stream, "image/png");
        var b = new DocumentInput(stream, "image/png");

        // Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void DocumentInput_WithDifferentMime_ShouldNotBeEqual()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var a = new DocumentInput(stream, "image/png");
        var b = new DocumentInput(stream, "image/jpeg");

        // Assert
        a.Should().NotBe(b);
    }
}
