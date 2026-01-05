// -----------------------------------------------------------------------
// <copyright file="INewsletterService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Email.Models;

namespace Compendium.Abstractions.Email;

/// <summary>
/// Provides operations for managing newsletter subscribers and mailing lists.
/// </summary>
public interface INewsletterService
{
    /// <summary>
    /// Subscribes an email address to one or more mailing lists.
    /// </summary>
    /// <param name="request">The subscription request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the subscriber or an error.</returns>
    Task<Result<Subscriber>> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes an email address from specified or all mailing lists.
    /// </summary>
    /// <param name="email">The email address to unsubscribe.</param>
    /// <param name="listId">Optional list ID. If not specified, unsubscribes from all lists.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> UnsubscribeAsync(string email, string? listId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a subscriber by their email address.
    /// </summary>
    /// <param name="email">The email address of the subscriber.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the subscriber or an error if not found.</returns>
    Task<Result<Subscriber>> GetSubscriberAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the attributes of a subscriber.
    /// </summary>
    /// <param name="email">The email address of the subscriber.</param>
    /// <param name="attributes">The attributes to update or add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> UpdateSubscriberAttributesAsync(string email, IReadOnlyDictionary<string, object> attributes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available mailing lists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of mailing lists or an error.</returns>
    Task<Result<IReadOnlyList<MailingList>>> ListMailingListsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending subscription (for double opt-in).
    /// </summary>
    /// <param name="email">The email address to confirm.</param>
    /// <param name="token">The confirmation token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> ConfirmSubscriptionAsync(string email, string token, CancellationToken cancellationToken = default);
}
