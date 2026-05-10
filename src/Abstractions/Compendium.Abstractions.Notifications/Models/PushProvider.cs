// -----------------------------------------------------------------------
// <copyright file="PushProvider.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Identifies the push notification provider associated with a device token.
/// </summary>
public enum PushProvider
{
    /// <summary>
    /// Firebase Cloud Messaging (Android, iOS, Web).
    /// </summary>
    FCM = 0,

    /// <summary>
    /// Apple Push Notification service (iOS, macOS).
    /// </summary>
    APNS = 1,

    /// <summary>
    /// Web Push protocol (browsers).
    /// </summary>
    WebPush = 2,
}
