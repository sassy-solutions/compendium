// -----------------------------------------------------------------------
// <copyright file="PushFailure.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Describes a single device token that failed to receive a push notification.
/// </summary>
public sealed record PushFailure
{
    /// <summary>
    /// Gets the device token value that failed.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets a human-readable reason describing why the delivery failed.
    /// </summary>
    public required string Reason { get; init; }
}
