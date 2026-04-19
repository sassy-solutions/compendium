// -----------------------------------------------------------------------
// <copyright file="AutoRegisterEventAttribute.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.EventSourcing.Attributes;

/// <summary>
/// Marks a domain event type for automatic registration in the event type registry.
/// This attribute is used by source generators to create compile-time safe event registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AutoRegisterEventAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoRegisterEventAttribute"/> class.
    /// </summary>
    public AutoRegisterEventAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the priority for registration order. Higher values are registered first.
    /// Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this event type should be registered
    /// even if it's in a different assembly than the calling assembly.
    /// Default is true.
    /// </summary>
    public bool AllowCrossAssembly { get; set; } = true;
}
