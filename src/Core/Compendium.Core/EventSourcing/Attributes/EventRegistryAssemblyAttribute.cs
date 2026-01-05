// -----------------------------------------------------------------------
// <copyright file="EventRegistryAssemblyAttribute.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.EventSourcing.Attributes;

/// <summary>
/// Marks an assembly for automatic event type registration scanning.
/// This attribute is used by source generators to identify assemblies containing domain events.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class EventRegistryAssemblyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventRegistryAssemblyAttribute"/> class.
    /// </summary>
    public EventRegistryAssemblyAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include abstract classes in registration.
    /// Default is false.
    /// </summary>
    public bool IncludeAbstract { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include internal classes in registration.
    /// Default is true.
    /// </summary>
    public bool IncludeInternal { get; set; } = true;

    /// <summary>
    /// Gets or sets a namespace prefix filter. Only types in namespaces starting with this prefix will be registered.
    /// If null or empty, all namespaces are included.
    /// </summary>
    public string? NamespacePrefix { get; set; }
}
