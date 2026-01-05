// -----------------------------------------------------------------------
// <copyright file="IBillingService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing.Models;

namespace Compendium.Abstractions.Billing;

/// <summary>
/// Provides operations for billing and checkout functionality.
/// This interface is provider-agnostic and can be implemented by various billing providers
/// such as LemonSqueezy, Stripe, Paddle, or Chargebee.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Creates a checkout session for purchasing a product or subscription.
    /// </summary>
    /// <param name="request">The checkout request details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the checkout session or an error.</returns>
    Task<Result<CheckoutSession>> CreateCheckoutSessionAsync(CreateCheckoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by their unique identifier.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the customer or an error if not found.</returns>
    Task<Result<BillingCustomer>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by their email address.
    /// </summary>
    /// <param name="email">The email address of the customer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the customer or an error if not found.</returns>
    Task<Result<BillingCustomer>> GetCustomerByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a customer in the billing system.
    /// </summary>
    /// <param name="request">The customer details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created or updated customer or an error.</returns>
    Task<Result<BillingCustomer>> UpsertCustomerAsync(UpsertCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a URL for the customer portal where customers can manage their subscriptions.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="returnUrl">Optional URL to redirect to after the customer is done.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the portal URL or an error.</returns>
    Task<Result<string>> CreateCustomerPortalUrlAsync(string customerId, string? returnUrl = null, CancellationToken cancellationToken = default);
}
