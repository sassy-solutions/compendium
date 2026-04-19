// -----------------------------------------------------------------------
// <copyright file="ISubscriptionService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing.Models;

namespace Compendium.Abstractions.Billing;

/// <summary>
/// Provides operations for managing subscriptions.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Retrieves a subscription by its unique identifier.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the subscription or an error if not found.</returns>
    Task<Result<Subscription>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active subscription for a customer, if any.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the active subscription or null if none exists.</returns>
    Task<Result<Subscription?>> GetActiveSubscriptionAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all subscriptions for a customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of subscriptions or an error.</returns>
    Task<Result<IReadOnlyList<Subscription>>> ListSubscriptionsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to cancel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to pause.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> PauseSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to resume.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a subscription to a different variant/plan.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription.</param>
    /// <param name="newVariantId">The variant ID to change to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the updated subscription or an error.</returns>
    Task<Result<Subscription>> UpdateSubscriptionAsync(string subscriptionId, string newVariantId, CancellationToken cancellationToken = default);
}
