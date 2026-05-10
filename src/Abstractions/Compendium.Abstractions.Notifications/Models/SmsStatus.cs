// -----------------------------------------------------------------------
// <copyright file="SmsStatus.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents the delivery status of an SMS message as reported by a provider.
/// </summary>
public enum SmsStatus
{
    /// <summary>
    /// The message has been accepted by the provider and is queued for delivery.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// The message has been sent by the provider to the carrier.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// The carrier has confirmed delivery to the recipient handset.
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// The message failed to be sent or delivered.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The carrier reported that the message could not be delivered to the recipient.
    /// </summary>
    Undelivered = 4,
}
