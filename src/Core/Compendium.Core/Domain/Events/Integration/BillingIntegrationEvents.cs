// -----------------------------------------------------------------------
// <copyright file="BillingIntegrationEvents.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Events.Integration;

/// <summary>
/// Integration event raised when a subscription is created.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PlanId">The identifier of the subscribed plan.</param>
/// <param name="Status">The status of the subscription.</param>
/// <param name="BillingPeriodStart">The start date of the billing period.</param>
/// <param name="BillingPeriodEnd">The end date of the billing period.</param>
public sealed record SubscriptionCreatedEvent(
    string SubscriptionId,
    string CustomerId,
    string PlanId,
    string Status,
    DateTimeOffset BillingPeriodStart,
    DateTimeOffset BillingPeriodEnd) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.created";
}

/// <summary>
/// Integration event raised when a subscription is updated.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PlanId">The identifier of the subscribed plan.</param>
/// <param name="PreviousPlanId">The identifier of the previous plan, if changed.</param>
/// <param name="Status">The status of the subscription.</param>
/// <param name="ChangeType">The type of change (e.g., upgrade, downgrade, renewal).</param>
public sealed record SubscriptionUpdatedEvent(
    string SubscriptionId,
    string CustomerId,
    string PlanId,
    string? PreviousPlanId,
    string Status,
    string ChangeType) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.updated";
}

/// <summary>
/// Integration event raised when a subscription is cancelled.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="CancellationReason">The reason for cancellation, if provided.</param>
/// <param name="EffectiveDate">The date when the cancellation takes effect.</param>
/// <param name="ImmediateCancel">Whether the subscription was cancelled immediately.</param>
public sealed record SubscriptionCancelledEvent(
    string SubscriptionId,
    string CustomerId,
    string? CancellationReason,
    DateTimeOffset EffectiveDate,
    bool ImmediateCancel) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.cancelled";
}

/// <summary>
/// Integration event raised when a subscription is paused.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PausedAt">The timestamp when the subscription was paused.</param>
/// <param name="ResumeAt">The timestamp when the subscription will resume, if scheduled.</param>
public sealed record SubscriptionPausedEvent(
    string SubscriptionId,
    string CustomerId,
    DateTimeOffset PausedAt,
    DateTimeOffset? ResumeAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.paused";
}

/// <summary>
/// Integration event raised when a subscription is resumed.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="ResumedAt">The timestamp when the subscription was resumed.</param>
public sealed record SubscriptionResumedEvent(
    string SubscriptionId,
    string CustomerId,
    DateTimeOffset ResumedAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.resumed";
}

/// <summary>
/// Integration event raised when a subscription enters a trial period.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PlanId">The identifier of the subscribed plan.</param>
/// <param name="TrialStart">The start date of the trial period.</param>
/// <param name="TrialEnd">The end date of the trial period.</param>
public sealed record SubscriptionTrialStartedEvent(
    string SubscriptionId,
    string CustomerId,
    string PlanId,
    DateTimeOffset TrialStart,
    DateTimeOffset TrialEnd) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.trial_started";
}

/// <summary>
/// Integration event raised when a subscription trial ends.
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="TrialEnd">The end date of the trial period.</param>
/// <param name="ConvertedToPaid">Whether the subscription converted to a paid subscription.</param>
public sealed record SubscriptionTrialEndedEvent(
    string SubscriptionId,
    string CustomerId,
    DateTimeOffset TrialEnd,
    bool ConvertedToPaid) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.subscription.trial_ended";
}

/// <summary>
/// Integration event raised when a payment succeeds.
/// </summary>
/// <param name="PaymentId">The unique identifier of the payment.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="SubscriptionId">The subscription identifier, if applicable.</param>
/// <param name="Amount">The payment amount in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="PaymentMethod">The payment method used.</param>
/// <param name="InvoiceId">The invoice identifier, if applicable.</param>
public sealed record PaymentSucceededEvent(
    string PaymentId,
    string CustomerId,
    string? SubscriptionId,
    long Amount,
    string Currency,
    string PaymentMethod,
    string? InvoiceId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.payment.succeeded";
}

/// <summary>
/// Integration event raised when a payment fails.
/// </summary>
/// <param name="PaymentId">The unique identifier of the payment.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="SubscriptionId">The subscription identifier, if applicable.</param>
/// <param name="Amount">The payment amount in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="FailureCode">The failure code from the payment provider.</param>
/// <param name="FailureMessage">The failure message from the payment provider.</param>
/// <param name="AttemptCount">The number of payment attempts.</param>
public sealed record PaymentFailedEvent(
    string PaymentId,
    string CustomerId,
    string? SubscriptionId,
    long Amount,
    string Currency,
    string? FailureCode,
    string? FailureMessage,
    int AttemptCount) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.payment.failed";
}

/// <summary>
/// Integration event raised when a refund is issued.
/// </summary>
/// <param name="RefundId">The unique identifier of the refund.</param>
/// <param name="PaymentId">The original payment identifier.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="Amount">The refund amount in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="Reason">The reason for the refund.</param>
/// <param name="IsPartial">Whether this is a partial refund.</param>
public sealed record RefundIssuedEvent(
    string RefundId,
    string PaymentId,
    string CustomerId,
    long Amount,
    string Currency,
    string? Reason,
    bool IsPartial) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.refund.issued";
}

/// <summary>
/// Integration event raised when an invoice is created.
/// </summary>
/// <param name="InvoiceId">The unique identifier of the invoice.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="SubscriptionId">The subscription identifier, if applicable.</param>
/// <param name="Amount">The invoice amount in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="DueDate">The due date of the invoice.</param>
public sealed record InvoiceCreatedEvent(
    string InvoiceId,
    string CustomerId,
    string? SubscriptionId,
    long Amount,
    string Currency,
    DateTimeOffset DueDate) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.invoice.created";
}

/// <summary>
/// Integration event raised when an invoice is paid.
/// </summary>
/// <param name="InvoiceId">The unique identifier of the invoice.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="PaymentId">The payment identifier used to pay the invoice.</param>
/// <param name="Amount">The invoice amount in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="PaidAt">The timestamp when the invoice was paid.</param>
public sealed record InvoicePaidEvent(
    string InvoiceId,
    string CustomerId,
    string PaymentId,
    long Amount,
    string Currency,
    DateTimeOffset PaidAt) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.invoice.paid";
}

/// <summary>
/// Integration event raised when a customer is created.
/// </summary>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="Name">The customer's name.</param>
/// <param name="ExternalId">An external identifier for the customer (e.g., from the identity system).</param>
public sealed record BillingCustomerCreatedEvent(
    string CustomerId,
    string Email,
    string? Name,
    string? ExternalId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.customer.created";
}

/// <summary>
/// Integration event raised when a customer is updated.
/// </summary>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="Name">The customer's name.</param>
public sealed record BillingCustomerUpdatedEvent(
    string CustomerId,
    string Email,
    string? Name) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.customer.updated";
}

/// <summary>
/// Integration event raised when a checkout session is completed.
/// </summary>
/// <param name="SessionId">The unique identifier of the checkout session.</param>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="SubscriptionId">The subscription identifier, if a subscription was created.</param>
/// <param name="Amount">The amount paid in the smallest currency unit (e.g., cents).</param>
/// <param name="Currency">The currency code (e.g., USD, EUR).</param>
/// <param name="ProductId">The product identifier.</param>
/// <param name="VariantId">The product variant identifier.</param>
public sealed record CheckoutCompletedEvent(
    string SessionId,
    string CustomerId,
    string? SubscriptionId,
    long Amount,
    string Currency,
    string ProductId,
    string? VariantId) : IntegrationEventBase
{
    /// <inheritdoc />
    public override string EventType => "billing.checkout.completed";
}
