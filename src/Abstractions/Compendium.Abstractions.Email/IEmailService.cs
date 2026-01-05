// -----------------------------------------------------------------------
// <copyright file="IEmailService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Email.Models;

namespace Compendium.Abstractions.Email;

/// <summary>
/// Provides operations for sending transactional emails.
/// This interface is provider-agnostic and can be implemented by various email providers
/// such as Listmonk, SendGrid, Mailgun, or AWS SES.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a single email message.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the send result or an error.</returns>
    Task<Result<EmailSendResult>> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a templated email message.
    /// </summary>
    /// <param name="message">The templated email message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the send result or an error.</returns>
    Task<Result<EmailSendResult>> SendTemplatedAsync(TemplatedEmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple email messages in a batch.
    /// </summary>
    /// <param name="messages">The email messages to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the batch result or an error.</returns>
    Task<Result<BatchEmailResult>> SendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a previously sent email.
    /// </summary>
    /// <param name="messageId">The message ID returned from the send operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the email status or an error.</returns>
    Task<Result<EmailStatus>> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken = default);
}
