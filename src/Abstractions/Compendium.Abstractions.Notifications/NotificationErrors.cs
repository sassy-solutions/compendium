// -----------------------------------------------------------------------
// <copyright file="NotificationErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications;

/// <summary>
/// Provides standard error definitions for notification (push / SMS) operations.
/// </summary>
public static class NotificationErrors
{
    /// <summary>
    /// Error returned when the supplied device token is invalid, unregistered, or expired.
    /// </summary>
    public static Error InvalidToken(string token) =>
        Error.Validation("Notification.InvalidToken", $"The device token '{token}' is invalid or no longer registered.");

    /// <summary>
    /// Error returned when the notification provider cannot be reached.
    /// </summary>
    public static Error ProviderUnreachable(string reason) =>
        Error.Unavailable("Notification.ProviderUnreachable", $"The notification provider is unreachable: {reason}");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static Error RateLimited(string reason) =>
        Error.TooManyRequests("Notification.RateLimited", $"Rate limit exceeded: {reason}");

    /// <summary>
    /// Error returned when the notification payload exceeds the provider-imposed size limit.
    /// </summary>
    public static Error PayloadTooLarge(int sizeBytes, int maxBytes) =>
        Error.Validation(
            "Notification.PayloadTooLarge",
            $"Notification payload of {sizeBytes} bytes exceeds the maximum allowed size of {maxBytes} bytes.");
}
