// -----------------------------------------------------------------------
// <copyright file="EventRegistryAssemblyAttributeTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.EventSourcing.Attributes;

namespace Compendium.Core.Tests.EventSourcing;

/// <summary>
/// Unit tests for <see cref="EventRegistryAssemblyAttribute"/> defaults and configurability.
/// </summary>
public class EventRegistryAssemblyAttributeTests
{
    [Fact]
    public void Constructor_WithDefaults_HasExpectedFlagDefaults()
    {
        // Act
        var attr = new EventRegistryAssemblyAttribute();

        // Assert
        attr.IncludeAbstract.Should().BeFalse();
        attr.IncludeInternal.Should().BeTrue();
        attr.NamespacePrefix.Should().BeNull();
    }

    [Fact]
    public void IncludeAbstract_CanBeSet()
    {
        // Act
        var attr = new EventRegistryAssemblyAttribute { IncludeAbstract = true };

        // Assert
        attr.IncludeAbstract.Should().BeTrue();
    }

    [Fact]
    public void IncludeInternal_CanBeOverridden()
    {
        // Act
        var attr = new EventRegistryAssemblyAttribute { IncludeInternal = false };

        // Assert
        attr.IncludeInternal.Should().BeFalse();
    }

    [Fact]
    public void NamespacePrefix_CanBeSet()
    {
        // Act
        var attr = new EventRegistryAssemblyAttribute { NamespacePrefix = "Compendium.Core.Domain" };

        // Assert
        attr.NamespacePrefix.Should().Be("Compendium.Core.Domain");
    }

    [Fact]
    public void Attribute_HasAssemblyTargetUsage()
    {
        // Act
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(EventRegistryAssemblyAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Assembly);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeFalse();
    }
}
