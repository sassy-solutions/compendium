// -----------------------------------------------------------------------
// <copyright file="EventTypeMetadataTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing;
using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Unit tests for the <see cref="EventTypeMetadata"/> DTO used by source-generated event registries.
/// </summary>
public class EventTypeMetadataTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsAllProperties()
    {
        // Arrange
        var type = typeof(TestDomainEvent);
        var typeName = type.AssemblyQualifiedName!;
        const int priority = 50;
        const string assembly = "Compendium.Core.Tests";

        // Act
        var metadata = new EventTypeMetadata(type, typeName, priority, assembly);

        // Assert
        metadata.Type.Should().Be(type);
        metadata.TypeName.Should().Be(typeName);
        metadata.Priority.Should().Be(priority);
        metadata.SourceAssembly.Should().Be(assembly);
    }

    [Fact]
    public void Constructor_WithNullType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EventTypeMetadata(null!, "TypeName", 0, "Asm");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("type");
    }

    [Fact]
    public void Constructor_WithNullTypeName_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EventTypeMetadata(typeof(TestDomainEvent), null!, 0, "Asm");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("typeName");
    }

    [Fact]
    public void Constructor_WithNullSourceAssembly_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EventTypeMetadata(typeof(TestDomainEvent), "TypeName", 0, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("sourceAssembly");
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Constructor_AcceptsAnyPriorityIntegerValue(int priority)
    {
        // Act
        var metadata = new EventTypeMetadata(typeof(TestDomainEvent), "TypeName", priority, "Asm");

        // Assert
        metadata.Priority.Should().Be(priority);
    }
}
