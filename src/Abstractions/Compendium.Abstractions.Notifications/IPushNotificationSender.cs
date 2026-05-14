// -----------------------------------------------------------------------
// <copyright file="IPushNotificationSender.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Notifications.Models;

namespace Compendium.Abstractions.Notifications;

/// <summary>
/// Provides operations for sending push notifications to one or more device tokens.
/// This interface is provider-agnostic and can be implemented by adapters targeting
/// FCM, APNS, Web Push, OneSignal, and similar services.
/// </summary>
public interface IPushNotificationSender
{
    /// <summary>
    /// Sends a push notification to the supplied device tokens.
    /// </summary>
    /// <param name="notification">The notification payload to deliver.</param>
    /// <param name="targets">The device tokens that should receive the notification.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the delivery outcome or an error.</returns>
    Task<Result<PushDeliveryResult>> SendAsync(
        PushNotification notification,
        IEnumerable<DeviceToken> targets,
        CancellationToken ct);

    /// <summary>
    /// Sends a push notification to the supplied device tokens using provider-side batching.
    /// </summary>
    /// <param name="notification">The notification payload to deliver.</param>
    /// <param name="targets">The device tokens that should receive the notification.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the aggregated batch delivery outcome or an error.</returns>
    Task<Result<BatchPushDeliveryResult>> SendBatchAsync(
        PushNotification notification,
        IReadOnlyList<DeviceToken> targets,
        CancellationToken ct);
}
