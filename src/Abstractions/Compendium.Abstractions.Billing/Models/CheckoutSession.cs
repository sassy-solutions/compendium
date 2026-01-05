// -----------------------------------------------------------------------
// <copyright file="CheckoutSession.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Models;

/// <summary>
/// Represents a checkout session for purchasing a product or subscription.
/// </summary>
public sealed record CheckoutSession
{
    /// <summary>
    /// Gets or initializes the unique identifier of the checkout.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the URL to redirect the customer to for checkout.
    /// </summary>
    public required string CheckoutUrl { get; init; }

    /// <summary>
    /// Gets or initializes the store ID.
    /// </summary>
    public string? StoreId { get; init; }

    /// <summary>
    /// Gets or initializes the variant ID being purchased.
    /// </summary>
    public string? VariantId { get; init; }

    /// <summary>
    /// Gets or initializes when the checkout was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the checkout expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets or initializes custom data associated with the checkout.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }
}

/// <summary>
/// Represents a request to create a checkout session.
/// </summary>
public sealed record CreateCheckoutRequest
{
    /// <summary>
    /// Gets or initializes the variant ID to purchase.
    /// </summary>
    public required string VariantId { get; init; }

    /// <summary>
    /// Gets or initializes the customer's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or initializes the customer's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the URL to redirect to after successful checkout.
    /// </summary>
    public string? SuccessUrl { get; init; }

    /// <summary>
    /// Gets or initializes the URL to redirect to if checkout is canceled.
    /// </summary>
    public string? CancelUrl { get; init; }

    /// <summary>
    /// Gets or initializes whether to embed the checkout (vs redirect).
    /// </summary>
    public bool? Embed { get; init; }

    /// <summary>
    /// Gets or initializes a discount code to apply.
    /// </summary>
    public string? DiscountCode { get; init; }

    /// <summary>
    /// Gets or initializes the user ID to associate with this checkout.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or initializes custom data to store with the checkout/order.
    /// This data will be included in webhook payloads.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }
}
