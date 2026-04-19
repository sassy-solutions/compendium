// -----------------------------------------------------------------------
// <copyright file="EmailMessage.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Models;

/// <summary>
/// Represents an email message to be sent.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>
    /// Gets or initializes the recipient email addresses.
    /// </summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>
    /// Gets or initializes the carbon copy email addresses.
    /// </summary>
    public IReadOnlyList<string>? Cc { get; init; }

    /// <summary>
    /// Gets or initializes the blind carbon copy email addresses.
    /// </summary>
    public IReadOnlyList<string>? Bcc { get; init; }

    /// <summary>
    /// Gets or initializes the sender's email address. If not specified, uses the default from address.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Gets or initializes the reply-to email address.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Gets or initializes the subject of the email.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets or initializes the plain text body of the email.
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// Gets or initializes the HTML body of the email.
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// Gets or initializes the email attachments.
    /// </summary>
    public IReadOnlyList<EmailAttachment>? Attachments { get; init; }

    /// <summary>
    /// Gets or initializes custom headers for the email.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets or initializes the message priority.
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// Gets or initializes custom metadata for tracking purposes.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a templated email message.
/// </summary>
public sealed record TemplatedEmailMessage
{
    /// <summary>
    /// Gets or initializes the recipient email addresses.
    /// </summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>
    /// Gets or initializes the carbon copy email addresses.
    /// </summary>
    public IReadOnlyList<string>? Cc { get; init; }

    /// <summary>
    /// Gets or initializes the blind carbon copy email addresses.
    /// </summary>
    public IReadOnlyList<string>? Bcc { get; init; }

    /// <summary>
    /// Gets or initializes the sender's email address. If not specified, uses the default from address.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Gets or initializes the reply-to email address.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Gets or initializes the template identifier.
    /// </summary>
    public required string TemplateId { get; init; }

    /// <summary>
    /// Gets or initializes the template data for variable substitution.
    /// </summary>
    public IReadOnlyDictionary<string, object>? TemplateData { get; init; }

    /// <summary>
    /// Gets or initializes the email attachments.
    /// </summary>
    public IReadOnlyList<EmailAttachment>? Attachments { get; init; }

    /// <summary>
    /// Gets or initializes the message priority.
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// Gets or initializes custom metadata for tracking purposes.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents an email attachment.
/// </summary>
public sealed record EmailAttachment
{
    /// <summary>
    /// Gets or initializes the filename of the attachment.
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// Gets or initializes the content of the attachment as bytes.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// Gets or initializes the MIME type of the attachment.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets or initializes whether this is an inline attachment.
    /// </summary>
    public bool IsInline { get; init; }

    /// <summary>
    /// Gets or initializes the Content-ID for inline attachments.
    /// </summary>
    public string? ContentId { get; init; }
}

/// <summary>
/// Defines email priority levels.
/// </summary>
public enum EmailPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 2
}
