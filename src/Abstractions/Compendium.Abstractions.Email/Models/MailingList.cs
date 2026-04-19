// -----------------------------------------------------------------------
// <copyright file="MailingList.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Email.Models;

/// <summary>
/// Represents a mailing list for newsletter subscriptions.
/// </summary>
public sealed record MailingList
{
    /// <summary>
    /// Gets or initializes the unique identifier of the mailing list.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the unique slug/name of the mailing list.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Gets or initializes the display name of the mailing list.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the description of the mailing list.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or initializes whether the mailing list is public (visible to subscribers).
    /// </summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether the mailing list is single opt-in.
    /// </summary>
    public bool IsSingleOptIn { get; init; }

    /// <summary>
    /// Gets or initializes the total number of subscribers.
    /// </summary>
    public int SubscriberCount { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the mailing list was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes the timestamp when the mailing list was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
