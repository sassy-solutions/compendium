// -----------------------------------------------------------------------
// <copyright file="Subscription.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Models;

/// <summary>
/// Represents a subscription in the billing system.
/// </summary>
public sealed record Subscription
{
    /// <summary>
    /// Gets or initializes the unique identifier of the subscription.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the customer ID associated with this subscription.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Gets or initializes the product/plan ID.
    /// </summary>
    public required string ProductId { get; init; }

    /// <summary>
    /// Gets or initializes the variant/price ID.
    /// </summary>
    public required string VariantId { get; init; }

    /// <summary>
    /// Gets or initializes the name of the product.
    /// </summary>
    public string? ProductName { get; init; }

    /// <summary>
    /// Gets or initializes the name of the variant.
    /// </summary>
    public string? VariantName { get; init; }

    /// <summary>
    /// Gets or initializes the subscription status.
    /// </summary>
    public required BillingSubscriptionStatus Status { get; init; }

    /// <summary>
    /// Gets or initializes the billing interval (e.g., "month", "year").
    /// </summary>
    public string? BillingInterval { get; init; }

    /// <summary>
    /// Gets or initializes the billing interval count.
    /// </summary>
    public int? BillingIntervalCount { get; init; }

    /// <summary>
    /// Gets or initializes the price amount in cents.
    /// </summary>
    public int? PriceAmountCents { get; init; }

    /// <summary>
    /// Gets or initializes the currency code (e.g., "USD", "EUR").
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Gets or initializes the tenant ID associated with this subscription.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata associated with the subscription.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the current billing period started.
    /// </summary>
    public DateTimeOffset? CurrentPeriodStart { get; init; }

    /// <summary>
    /// Gets or initializes when the current billing period ends.
    /// </summary>
    public DateTimeOffset? CurrentPeriodEnd { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription will be canceled, if scheduled.
    /// </summary>
    public DateTimeOffset? CancelAt { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription was actually canceled.
    /// </summary>
    public DateTimeOffset? CanceledAt { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription ended.
    /// </summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the trial period ends, if applicable.
    /// </summary>
    public DateTimeOffset? TrialEndsAt { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription was paused.
    /// </summary>
    public DateTimeOffset? PausedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the subscription will resume from pause.
    /// </summary>
    public DateTimeOffset? ResumesAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the subscription is in a trial period.
    /// </summary>
    public bool IsInTrial => TrialEndsAt.HasValue && DateTimeOffset.UtcNow < TrialEndsAt.Value;

    /// <summary>
    /// Gets a value indicating whether the subscription is active.
    /// </summary>
    public bool IsActive => Status is BillingSubscriptionStatus.Active or BillingSubscriptionStatus.OnTrial;
}

/// <summary>
/// Defines subscription status values.
/// </summary>
public enum BillingSubscriptionStatus
{
    /// <summary>
    /// Subscription is active and in good standing.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Subscription is in a trial period.
    /// </summary>
    OnTrial = 1,

    /// <summary>
    /// Subscription is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Subscription payment is past due.
    /// </summary>
    PastDue = 3,

    /// <summary>
    /// Subscription is unpaid.
    /// </summary>
    Unpaid = 4,

    /// <summary>
    /// Subscription has been canceled.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired = 6
}
