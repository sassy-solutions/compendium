// -----------------------------------------------------------------------
// <copyright file="ISmsSender.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Notifications.Models;

namespace Compendium.Abstractions.Notifications;

/// <summary>
/// Provides operations for sending SMS / MMS messages. This interface is provider-agnostic
/// and can be implemented by adapters targeting Twilio, MessageBird, Vonage, and similar services.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Sends a single SMS (or MMS) message to a recipient.
    /// </summary>
    /// <param name="message">The message payload to deliver.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the per-message delivery outcome or an error.</returns>
    Task<Result<SmsDeliveryResult>> SendAsync(SmsMessage message, CancellationToken ct);

    /// <summary>
    /// Sends multiple SMS messages in a single batch using provider-side batching where supported.
    /// </summary>
    /// <param name="messages">The messages to deliver.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the aggregated batch delivery outcome or an error.</returns>
    Task<Result<BatchSmsDeliveryResult>> SendBatchAsync(IReadOnlyList<SmsMessage> messages, CancellationToken ct);
}
