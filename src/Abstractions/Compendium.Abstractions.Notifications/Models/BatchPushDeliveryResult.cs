// -----------------------------------------------------------------------
// <copyright file="BatchPushDeliveryResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents the aggregated result of delivering a push notification to multiple device tokens
/// across one or more provider batches.
/// </summary>
public sealed record BatchPushDeliveryResult
{
    /// <summary>
    /// Gets the total number of device tokens that successfully received the notification.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the total number of device tokens that failed to receive the notification.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the per-batch delivery results that compose this aggregated result.
    /// </summary>
    public required IReadOnlyList<PushDeliveryResult> Results { get; init; }
}
