// -----------------------------------------------------------------------
// <copyright file="PushDeliveryResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents the result of delivering a single push notification to a set of device tokens.
/// </summary>
public sealed record PushDeliveryResult
{
    /// <summary>
    /// Gets the number of device tokens the notification was successfully sent to.
    /// </summary>
    public required int Sent { get; init; }

    /// <summary>
    /// Gets the device tokens that the provider rejected, with the reason for each failure.
    /// </summary>
    public required IReadOnlyList<PushFailure> Failed { get; init; }
}
