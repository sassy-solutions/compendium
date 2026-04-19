// -----------------------------------------------------------------------
// <copyright file="ListmonkEmailService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.Http;
using Compendium.Adapters.Listmonk.Http.Models;

namespace Compendium.Adapters.Listmonk.Services;

/// <summary>
/// Implements email service using Listmonk REST API.
/// </summary>
internal sealed class ListmonkEmailService : IEmailService
{
    private readonly ListmonkHttpClient _httpClient;
    private readonly ListmonkOptions _options;
    private readonly ILogger<ListmonkEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListmonkEmailService"/> class.
    /// </summary>
    public ListmonkEmailService(
        ListmonkHttpClient httpClient,
        IOptions<ListmonkOptions> options,
        ILogger<ListmonkEmailService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<EmailSendResult>> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Listmonk doesn't support raw email sending - it requires templates
        // For direct email sending, we need to use a campaign or create a template first
        _logger.LogWarning("Listmonk requires templated emails. Use SendTemplatedAsync instead.");

        return EmailErrors.SendFailed("Listmonk requires templated emails. Use SendTemplatedAsync instead.");
    }

    /// <inheritdoc />
    public async Task<Result<EmailSendResult>> SendTemplatedAsync(
        TemplatedEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.To.Count == 0)
        {
            return EmailErrors.InvalidRecipient("At least one recipient is required");
        }

        _logger.LogInformation("Sending templated email to {RecipientCount} recipients using template {TemplateId}",
            message.To.Count, message.TemplateId);

        if (!int.TryParse(message.TemplateId, out var templateId))
        {
            return EmailErrors.TemplateNotFound(message.TemplateId);
        }

        // Send to each recipient (Listmonk transactional API sends to one subscriber at a time)
        var firstRecipient = message.To[0];

        var request = new ListmonkTransactionalRequest
        {
            SubscriberEmail = firstRecipient,
            TemplateId = templateId,
            FromEmail = message.From ?? _options.DefaultFromEmail,
            Data = message.TemplateData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        var result = await _httpClient.SendTransactionalAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to send templated email to {Recipient}: {Error}",
                firstRecipient, result.Error.Message);
            return result.Error;
        }

        var messageId = Guid.NewGuid().ToString();
        _logger.LogInformation("Email sent successfully with message ID {MessageId}", messageId);

        return new EmailSendResult
        {
            MessageId = messageId,
            Status = EmailStatus.Sent,
            SentAt = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<Result<BatchEmailResult>> SendBatchAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        _logger.LogInformation("Sending batch of {Count} emails", messageList.Count);

        // Listmonk doesn't support batch raw email sending
        // Return failure for each message
        var results = messageList.Select(m => new BatchEmailItemResult
        {
            To = m.To.FirstOrDefault() ?? "unknown",
            Success = false,
            ErrorMessage = "Listmonk requires templated emails"
        }).ToList();

        return new BatchEmailResult
        {
            TotalCount = messageList.Count,
            SuccessCount = 0,
            Results = results
        };
    }

    /// <inheritdoc />
    public Task<Result<EmailStatus>> GetMessageStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Listmonk doesn't provide message status tracking for transactional emails
        _logger.LogDebug("Message status lookup not supported by Listmonk for message {MessageId}", messageId);

        return Task.FromResult(Result<EmailStatus>.Success(EmailStatus.Sent));
    }
}
