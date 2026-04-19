// -----------------------------------------------------------------------
// <copyright file="EmailSendResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Models;

/// <summary>
/// Represents the result of sending an email.
/// </summary>
public sealed record EmailSendResult
{
    /// <summary>
    /// Gets or initializes the unique message identifier assigned by the email provider.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or initializes the status of the email send operation.
    /// </summary>
    public required EmailStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the email was accepted for delivery.
    /// </summary>
    public DateTimeOffset SentAt { get; init; }

    /// <summary>
    /// Gets or initializes additional provider-specific information.
    /// </summary>
    public IReadOnlyDictionary<string, object>? ProviderData { get; init; }
}

/// <summary>
/// Represents the result of a batch email send operation.
/// </summary>
public sealed record BatchEmailResult
{
    /// <summary>
    /// Gets or initializes the total number of emails in the batch.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets or initializes the number of emails successfully sent.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets or initializes the number of emails that failed to send.
    /// </summary>
    public int FailedCount => TotalCount - SuccessCount;

    /// <summary>
    /// Gets or initializes the individual results for each email.
    /// </summary>
    public required IReadOnlyList<BatchEmailItemResult> Results { get; init; }
}

/// <summary>
/// Represents the result of sending a single email in a batch.
/// </summary>
public sealed record BatchEmailItemResult
{
    /// <summary>
    /// Gets or initializes the recipient email address.
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Gets or initializes whether the send was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or initializes the message ID if successful.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets or initializes the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Defines email status values.
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// Email has been queued for delivery.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Email is being sent.
    /// </summary>
    Sending = 1,

    /// <summary>
    /// Email has been sent successfully.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Email has been delivered to the recipient's server.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Email has been opened by the recipient.
    /// </summary>
    Opened = 4,

    /// <summary>
    /// A link in the email has been clicked.
    /// </summary>
    Clicked = 5,

    /// <summary>
    /// Email has bounced.
    /// </summary>
    Bounced = 6,

    /// <summary>
    /// Email has been marked as spam.
    /// </summary>
    SpamComplaint = 7,

    /// <summary>
    /// Email sending failed.
    /// </summary>
    Failed = 8
}
