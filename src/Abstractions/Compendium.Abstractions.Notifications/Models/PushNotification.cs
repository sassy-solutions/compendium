// -----------------------------------------------------------------------
// <copyright file="PushNotification.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents a push notification payload to be delivered to one or more devices.
/// </summary>
public sealed record PushNotification
{
    /// <summary>
    /// Gets the title shown at the top of the notification.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the body text of the notification.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets the custom data payload delivered alongside the notification (provider-specific limits apply).
    /// </summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the optional sound identifier to play on the device when the notification is delivered.
    /// </summary>
    public string? Sound { get; init; }

    /// <summary>
    /// Gets the optional badge count to display on the application icon (primarily APNS).
    /// </summary>
    public int? Badge { get; init; }

    /// <summary>
    /// Gets the optional click action that the device should invoke when the notification is tapped.
    /// </summary>
    public string? ClickAction { get; init; }
}
