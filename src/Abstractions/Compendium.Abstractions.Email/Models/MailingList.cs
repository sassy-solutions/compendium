// -----------------------------------------------------------------------
// <copyright file="MailingList.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
