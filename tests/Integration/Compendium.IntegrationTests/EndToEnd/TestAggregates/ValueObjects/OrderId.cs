// -----------------------------------------------------------------------
// <copyright file="OrderId.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

/// <summary>
/// Strong-typed identifier for Order aggregates.
/// </summary>
public sealed record OrderId(Guid Value)
{
    /// <summary>
    /// Creates a new OrderId with a generated GUID.
    /// </summary>
    public static OrderId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an OrderId from a string.
    /// </summary>
    public static OrderId From(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Converts OrderId to string.
    /// </summary>
    public override string ToString() => Value.ToString();
}
