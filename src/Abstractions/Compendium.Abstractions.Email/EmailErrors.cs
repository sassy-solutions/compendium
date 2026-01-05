// -----------------------------------------------------------------------
// <copyright file="EmailErrors.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email;

/// <summary>
/// Provides standard error definitions for email operations.
/// </summary>
public static class EmailErrors
{
    /// <summary>
    /// Error returned when a subscriber is not found.
    /// </summary>
    public static Error SubscriberNotFound(string email) =>
        Error.NotFound("Email.SubscriberNotFound", $"Subscriber with email '{email}' was not found.");

    /// <summary>
    /// Error returned when a subscriber already exists.
    /// </summary>
    public static Error SubscriberAlreadyExists(string email) =>
        Error.Conflict("Email.SubscriberAlreadyExists", $"A subscriber with email '{email}' already exists.");

    /// <summary>
    /// Error returned when a mailing list is not found.
    /// </summary>
    public static Error MailingListNotFound(string listId) =>
        Error.NotFound("Email.MailingListNotFound", $"Mailing list with ID '{listId}' was not found.");

    /// <summary>
    /// Error returned when a template is not found.
    /// </summary>
    public static Error TemplateNotFound(string templateId) =>
        Error.NotFound("Email.TemplateNotFound", $"Email template with ID '{templateId}' was not found.");

    /// <summary>
    /// Error returned when the email format is invalid.
    /// </summary>
    public static Error InvalidEmailFormat(string email) =>
        Error.Validation("Email.InvalidEmailFormat", $"The email address '{email}' is not valid.");

    /// <summary>
    /// Error returned when a recipient is invalid or missing.
    /// </summary>
    public static Error InvalidRecipient(string reason) =>
        Error.Validation("Email.InvalidRecipient", reason);

    /// <summary>
    /// Error returned when sending an email fails.
    /// </summary>
    public static Error SendFailed(string reason) =>
        Error.Failure("Email.SendFailed", $"Failed to send email: {reason}");

    /// <summary>
    /// Error returned when the email provider is unavailable.
    /// </summary>
    public static readonly Error ProviderUnavailable =
        Error.Unavailable("Email.ProviderUnavailable", "The email provider is currently unavailable.");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static readonly Error RateLimitExceeded =
        Error.TooManyRequests("Email.RateLimitExceeded", "Rate limit exceeded. Please try again later.");

    /// <summary>
    /// Error returned when the message ID is not found.
    /// </summary>
    public static Error MessageNotFound(string messageId) =>
        Error.NotFound("Email.MessageNotFound", $"Message with ID '{messageId}' was not found.");

    /// <summary>
    /// Error returned when the confirmation token is invalid.
    /// </summary>
    public static readonly Error InvalidConfirmationToken =
        Error.Validation("Email.InvalidConfirmationToken", "The confirmation token is invalid or expired.");

    /// <summary>
    /// Error returned when required tenant context is missing.
    /// </summary>
    public static readonly Error TenantContextRequired =
        Error.Validation("Email.TenantContextRequired", "Tenant context is required for this operation.");
}
