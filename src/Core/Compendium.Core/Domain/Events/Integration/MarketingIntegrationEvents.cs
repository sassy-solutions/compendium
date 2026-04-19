// -----------------------------------------------------------------------
// <copyright file="MarketingIntegrationEvents.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events.Integration;

/// <summary>
/// Integration event raised when a subscriber subscribes to a newsletter list.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="ListId">The identifier of the list subscribed to.</param>
/// <param name="ListName">The name of the list.</param>
/// <param name="SubscribedAt">The timestamp when the subscription occurred.</param>
/// <param name="SubscriptionMethod">The method of subscription (e.g., web form, API, import).</param>
/// <param name="IsDoubleOptIn">Whether double opt-in was required.</param>
public sealed record SubscriberSubscribedEvent(
    string SubscriberId,
    string Email,
    string ListId,
    string ListName,
    DateTimeOffset SubscribedAt,
    string SubscriptionMethod,
    bool IsDoubleOptIn) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.subscribed";
}

/// <summary>
/// Integration event raised when a subscriber unsubscribes from a newsletter list.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="ListId">The identifier of the list unsubscribed from.</param>
/// <param name="ListName">The name of the list.</param>
/// <param name="UnsubscribedAt">The timestamp when the unsubscription occurred.</param>
/// <param name="UnsubscribeReason">The reason for unsubscribing, if provided.</param>
/// <param name="UnsubscribeMethod">The method of unsubscription (e.g., link click, API, complaint).</param>
public sealed record SubscriberUnsubscribedEvent(
    string SubscriberId,
    string Email,
    string ListId,
    string ListName,
    DateTimeOffset UnsubscribedAt,
    string? UnsubscribeReason,
    string UnsubscribeMethod) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.unsubscribed";
}

/// <summary>
/// Integration event raised when a subscriber confirms their email (double opt-in).
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="ListId">The identifier of the list.</param>
/// <param name="ListName">The name of the list.</param>
/// <param name="ConfirmedAt">The timestamp when the confirmation occurred.</param>
public sealed record SubscriberConfirmedEvent(
    string SubscriberId,
    string Email,
    string ListId,
    string ListName,
    DateTimeOffset ConfirmedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.confirmed";
}

/// <summary>
/// Integration event raised when a subscriber's email bounces.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="BounceType">The type of bounce (hard, soft).</param>
/// <param name="BounceReason">The reason for the bounce.</param>
/// <param name="BouncedAt">The timestamp when the bounce occurred.</param>
/// <param name="CampaignId">The campaign that triggered the bounce, if applicable.</param>
public sealed record SubscriberBouncedEvent(
    string SubscriberId,
    string Email,
    string BounceType,
    string? BounceReason,
    DateTimeOffset BouncedAt,
    string? CampaignId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.bounced";
}

/// <summary>
/// Integration event raised when a subscriber marks an email as spam.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="ComplainedAt">The timestamp when the complaint was received.</param>
/// <param name="CampaignId">The campaign that triggered the complaint.</param>
public sealed record SubscriberComplainedEvent(
    string SubscriberId,
    string Email,
    DateTimeOffset ComplainedAt,
    string? CampaignId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.complained";
}

/// <summary>
/// Integration event raised when a subscriber is blocklisted.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="BlocklistReason">The reason for blocklisting.</param>
/// <param name="BlocklistedAt">The timestamp when the subscriber was blocklisted.</param>
public sealed record SubscriberBlocklistedEvent(
    string SubscriberId,
    string Email,
    string BlocklistReason,
    DateTimeOffset BlocklistedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.blocklisted";
}

/// <summary>
/// Integration event raised when a subscriber's attributes are updated.
/// </summary>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="UpdatedAttributes">The attributes that were updated.</param>
/// <param name="UpdatedAt">The timestamp when the update occurred.</param>
public sealed record SubscriberAttributesUpdatedEvent(
    string SubscriberId,
    string Email,
    IReadOnlyDictionary<string, string?> UpdatedAttributes,
    DateTimeOffset UpdatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.subscriber.attributes_updated";
}

/// <summary>
/// Integration event raised when a campaign is sent.
/// </summary>
/// <param name="CampaignId">The unique identifier of the campaign.</param>
/// <param name="CampaignName">The name of the campaign.</param>
/// <param name="Subject">The subject line of the campaign.</param>
/// <param name="ListIds">The identifiers of the lists the campaign was sent to.</param>
/// <param name="RecipientCount">The number of recipients.</param>
/// <param name="SentAt">The timestamp when the campaign was sent.</param>
public sealed record CampaignSentEvent(
    string CampaignId,
    string CampaignName,
    string Subject,
    IReadOnlyList<string> ListIds,
    int RecipientCount,
    DateTimeOffset SentAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.campaign.sent";
}

/// <summary>
/// Integration event raised when a campaign email is opened.
/// </summary>
/// <param name="CampaignId">The unique identifier of the campaign.</param>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="OpenedAt">The timestamp when the email was opened.</param>
/// <param name="IpAddress">The IP address from which the email was opened.</param>
/// <param name="UserAgent">The user agent string of the email client.</param>
public sealed record CampaignOpenedEvent(
    string CampaignId,
    string SubscriberId,
    string Email,
    DateTimeOffset OpenedAt,
    string? IpAddress,
    string? UserAgent) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.campaign.opened";
}

/// <summary>
/// Integration event raised when a link in a campaign email is clicked.
/// </summary>
/// <param name="CampaignId">The unique identifier of the campaign.</param>
/// <param name="SubscriberId">The unique identifier of the subscriber.</param>
/// <param name="Email">The subscriber's email address.</param>
/// <param name="LinkUrl">The URL of the clicked link.</param>
/// <param name="ClickedAt">The timestamp when the link was clicked.</param>
/// <param name="IpAddress">The IP address from which the link was clicked.</param>
public sealed record CampaignLinkClickedEvent(
    string CampaignId,
    string SubscriberId,
    string Email,
    string LinkUrl,
    DateTimeOffset ClickedAt,
    string? IpAddress) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.campaign.link_clicked";
}

/// <summary>
/// Integration event raised when a transactional email is sent.
/// </summary>
/// <param name="TransactionalId">The unique identifier of the transactional email.</param>
/// <param name="TemplateId">The template identifier used.</param>
/// <param name="RecipientEmail">The recipient's email address.</param>
/// <param name="Subject">The subject line of the email.</param>
/// <param name="SentAt">The timestamp when the email was sent.</param>
/// <param name="Status">The status of the send operation.</param>
public sealed record TransactionalEmailSentEvent(
    string TransactionalId,
    string TemplateId,
    string RecipientEmail,
    string Subject,
    DateTimeOffset SentAt,
    string Status) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.transactional.sent";
}

/// <summary>
/// Integration event raised when a list is created.
/// </summary>
/// <param name="ListId">The unique identifier of the list.</param>
/// <param name="Name">The name of the list.</param>
/// <param name="Description">The description of the list.</param>
/// <param name="ListType">The type of list (e.g., public, private).</param>
/// <param name="CreatedAt">The timestamp when the list was created.</param>
public sealed record ListCreatedEvent(
    string ListId,
    string Name,
    string? Description,
    string ListType,
    DateTimeOffset CreatedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.list.created";
}

/// <summary>
/// Integration event raised when a list is deleted.
/// </summary>
/// <param name="ListId">The unique identifier of the list.</param>
/// <param name="Name">The name of the list.</param>
/// <param name="SubscriberCount">The number of subscribers in the list at deletion time.</param>
/// <param name="DeletedAt">The timestamp when the list was deleted.</param>
public sealed record ListDeletedEvent(
    string ListId,
    string Name,
    int SubscriberCount,
    DateTimeOffset DeletedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "marketing.list.deleted";
}
