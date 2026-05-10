// -----------------------------------------------------------------------
// <copyright file="DocumentModelTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Compendium.Abstractions.Documents.Tests.Models;

public sealed class DocumentModelTests
{
    [Theory]
    [InlineData(DocumentModel.Generic, "Generic")]
    [InlineData(DocumentModel.Receipt, "Receipt")]
    [InlineData(DocumentModel.Invoice, "Invoice")]
    [InlineData(DocumentModel.IdDocument, "IdDocument")]
    [InlineData(DocumentModel.Custom, "Custom")]
    public void DocumentModel_DefinesExpectedMembers(DocumentModel value, string name)
    {
        // Act
        var actual = value.ToString();

        // Assert
        actual.Should().Be(name);
    }

    [Fact]
    public void DocumentModel_HasExactlyFiveMembers()
    {
        // Act
        var members = Enum.GetValues<DocumentModel>();

        // Assert
        members.Should().HaveCount(5);
    }

    [Fact]
    public void DocumentModel_SerializesAsString()
    {
        // Act
        var json = JsonSerializer.Serialize(DocumentModel.Invoice);

        // Assert
        json.Should().Be("\"Invoice\"");
    }

    [Fact]
    public void DocumentModel_DeserializesFromString()
    {
        // Act
        var value = JsonSerializer.Deserialize<DocumentModel>("\"Receipt\"");

        // Assert
        value.Should().Be(DocumentModel.Receipt);
    }
}
