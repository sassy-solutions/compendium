// -----------------------------------------------------------------------
// <copyright file="BillingErrors.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing;

/// <summary>
/// Provides standard error definitions for billing operations.
/// </summary>
public static class BillingErrors
{
    /// <summary>
    /// Error returned when a customer is not found.
    /// </summary>
    public static Error CustomerNotFound(string customerId) =>
        Error.NotFound("Billing.CustomerNotFound", $"Customer with ID '{customerId}' was not found.");

    /// <summary>
    /// Error returned when a customer with the specified email is not found.
    /// </summary>
    public static Error CustomerNotFoundByEmail(string email) =>
        Error.NotFound("Billing.CustomerNotFoundByEmail", $"Customer with email '{email}' was not found.");

    /// <summary>
    /// Error returned when a subscription is not found.
    /// </summary>
    public static Error SubscriptionNotFound(string subscriptionId) =>
        Error.NotFound("Billing.SubscriptionNotFound", $"Subscription with ID '{subscriptionId}' was not found.");

    /// <summary>
    /// Error returned when no active subscription exists for a customer.
    /// </summary>
    public static Error NoActiveSubscription(string customerId) =>
        Error.NotFound("Billing.NoActiveSubscription", $"No active subscription found for customer '{customerId}'.");

    /// <summary>
    /// Error returned when the subscription is already canceled.
    /// </summary>
    public static Error SubscriptionAlreadyCanceled(string subscriptionId) =>
        Error.Conflict("Billing.SubscriptionAlreadyCanceled", $"Subscription '{subscriptionId}' is already canceled.");

    /// <summary>
    /// Error returned when the subscription is not pausable.
    /// </summary>
    public static Error SubscriptionNotPausable(string subscriptionId) =>
        Error.Conflict("Billing.SubscriptionNotPausable", $"Subscription '{subscriptionId}' cannot be paused.");

    /// <summary>
    /// Error returned when the subscription is not paused.
    /// </summary>
    public static Error SubscriptionNotPaused(string subscriptionId) =>
        Error.Conflict("Billing.SubscriptionNotPaused", $"Subscription '{subscriptionId}' is not paused.");

    /// <summary>
    /// Error returned when a license is invalid.
    /// </summary>
    public static Error InvalidLicense(string licenseKey) =>
        Error.Validation("Billing.InvalidLicense", $"License key '{licenseKey}' is invalid.");

    /// <summary>
    /// Error returned when a license has expired.
    /// </summary>
    public static Error LicenseExpired(string licenseKey) =>
        Error.Validation("Billing.LicenseExpired", $"License key '{licenseKey}' has expired.");

    /// <summary>
    /// Error returned when the license activation limit has been reached.
    /// </summary>
    public static Error LicenseActivationLimitReached(string licenseKey) =>
        Error.Conflict("Billing.LicenseActivationLimitReached", $"License key '{licenseKey}' has reached its activation limit.");

    /// <summary>
    /// Error returned when a license instance is not found.
    /// </summary>
    public static Error LicenseInstanceNotFound(string instanceId) =>
        Error.NotFound("Billing.LicenseInstanceNotFound", $"License instance '{instanceId}' was not found.");

    /// <summary>
    /// Error returned when a variant is not found.
    /// </summary>
    public static Error VariantNotFound(string variantId) =>
        Error.NotFound("Billing.VariantNotFound", $"Variant with ID '{variantId}' was not found.");

    /// <summary>
    /// Error returned when the webhook signature is invalid.
    /// </summary>
    public static readonly Error InvalidWebhookSignature =
        Error.Unauthorized("Billing.InvalidWebhookSignature", "The webhook signature is invalid.");

    /// <summary>
    /// Error returned when webhook processing fails.
    /// </summary>
    public static Error WebhookProcessingFailed(string reason) =>
        Error.Failure("Billing.WebhookProcessingFailed", $"Failed to process webhook: {reason}");

    /// <summary>
    /// Error returned when the billing provider is unavailable.
    /// </summary>
    public static readonly Error ProviderUnavailable =
        Error.Unavailable("Billing.ProviderUnavailable", "The billing provider is currently unavailable.");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static readonly Error RateLimitExceeded =
        Error.TooManyRequests("Billing.RateLimitExceeded", "Rate limit exceeded. Please try again later.");

    /// <summary>
    /// Error returned when required tenant context is missing.
    /// </summary>
    public static readonly Error TenantContextRequired =
        Error.Validation("Billing.TenantContextRequired", "Tenant context is required for this operation.");
}
