// -----------------------------------------------------------------------
// <copyright file="Subscriber.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Models;

/// <summary>
/// Represents a newsletter subscriber.
/// </summary>
public sealed record Subscriber
{
    /// <summary>
    /// Gets or initializes the unique identifier of the subscriber.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the email address of the subscriber.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the name of the subscriber.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the subscription status.
    /// </summary>
    public required SubscriptionStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the mailing lists the subscriber belongs to.
    /// </summary>
    public IReadOnlyList<string>? ListIds { get; init; }

    /// <summary>
    /// Gets or initializes custom attributes for the subscriber.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the subscriber was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the subscriber was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the subscriber confirmed their subscription.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the subscriber unsubscribed.
    /// </summary>
    public DateTimeOffset? UnsubscribedAt { get; init; }
}

/// <summary>
/// Represents a request to subscribe to a newsletter.
/// </summary>
public sealed record SubscribeRequest
{
    /// <summary>
    /// Gets or initializes the email address to subscribe.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the name of the subscriber.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the mailing list IDs to subscribe to.
    /// </summary>
    public IReadOnlyList<string>? ListIds { get; init; }

    /// <summary>
    /// Gets or initializes custom attributes for the subscriber.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }

    /// <summary>
    /// Gets or initializes whether to require double opt-in confirmation.
    /// </summary>
    public bool RequireConfirmation { get; init; } = true;
}

/// <summary>
/// Defines subscription status values.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is pending confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Subscription is active and confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Subscriber has unsubscribed.
    /// </summary>
    Unsubscribed = 2,

    /// <summary>
    /// Subscriber has been blocked or blacklisted.
    /// </summary>
    Blocked = 3,

    /// <summary>
    /// Subscriber's email bounced.
    /// </summary>
    Bounced = 4
}
