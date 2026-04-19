// -----------------------------------------------------------------------
// <copyright file="OrderStatus.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

/// <summary>
/// Represents the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created but not yet completed.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Order has been completed.
    /// </summary>
    Completed = 1
}
