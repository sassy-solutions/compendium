// -----------------------------------------------------------------------
// <copyright file="SmsMessage.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents an SMS (or MMS) message payload to be delivered to a single recipient.
/// </summary>
public sealed record SmsMessage
{
    /// <summary>
    /// Gets the sender phone number in E.164 format (e.g. "+15551234567") or a provider-registered alphanumeric sender ID.
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// Gets the recipient phone number in E.164 format (e.g. "+15557654321").
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Gets the textual body of the message.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets the tenant identifier the message belongs to.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the optional collection of media URLs to attach (turning the message into an MMS).
    /// </summary>
    public IReadOnlyList<string>? MediaUrls { get; init; }
}
