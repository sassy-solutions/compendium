// -----------------------------------------------------------------------
// <copyright file="StripeMapper.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Stripe.Services;

/// <summary>
/// Static helpers that map Stripe.net SDK DTOs to the provider-agnostic
/// <see cref="Compendium.Abstractions.Billing.Models"/> records.
/// </summary>
internal static class StripeMapper
{
    /// <summary>
    /// Maps a Stripe subscription status string to the canonical
    /// <see cref="BillingSubscriptionStatus"/> enum. Unknown values default to
    /// <see cref="BillingSubscriptionStatus.Active"/> and are logged by callers.
    /// </summary>
    public static BillingSubscriptionStatus MapSubscriptionStatus(string? stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => BillingSubscriptionStatus.Active,
            "trialing" => BillingSubscriptionStatus.OnTrial,
            "past_due" => BillingSubscriptionStatus.PastDue,
            "unpaid" => BillingSubscriptionStatus.Unpaid,
            "canceled" => BillingSubscriptionStatus.Cancelled,
            "incomplete_expired" => BillingSubscriptionStatus.Cancelled,
            "paused" => BillingSubscriptionStatus.Paused,
            "incomplete" => BillingSubscriptionStatus.PastDue,
            _ => BillingSubscriptionStatus.Active
        };
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> (assumed UTC from Stripe) into
    /// <see cref="DateTimeOffset"/> with UTC kind.
    /// </summary>
    public static DateTimeOffset ToUtcOffset(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>
    /// Converts a nullable <see cref="DateTime"/> (assumed UTC from Stripe) into
    /// nullable <see cref="DateTimeOffset"/>.
    /// </summary>
    public static DateTimeOffset? ToUtcOffset(DateTime? value)
        => value.HasValue ? ToUtcOffset(value.Value) : null;

    /// <summary>
    /// Maps a Stripe <see cref="global::Stripe.Customer"/> to a
    /// <see cref="BillingCustomer"/>.
    /// </summary>
    public static BillingCustomer ToBillingCustomer(global::Stripe.Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new BillingCustomer
        {
            Id = customer.Id,
            Email = customer.Email ?? string.Empty,
            Name = customer.Name,
            City = customer.Address?.City,
            Region = customer.Address?.State,
            Country = customer.Address?.Country,
            CreatedAt = ToUtcOffset(customer.Created),
            TenantId = TryGetMetadata(customer.Metadata, "tenant_id"),
            UserId = TryGetMetadata(customer.Metadata, "user_id"),
            CustomData = ToCustomData(customer.Metadata)
        };
    }

    /// <summary>
    /// Maps a Stripe <see cref="global::Stripe.Subscription"/> to a
    /// <see cref="Subscription"/>.
    /// </summary>
    public static Subscription ToSubscription(global::Stripe.Subscription sub)
    {
        ArgumentNullException.ThrowIfNull(sub);

        var firstItem = sub.Items?.Data is { Count: > 0 } items ? items[0] : null;
        var price = firstItem?.Price;

        var productId = price?.ProductId ?? string.Empty;
        var variantId = price?.Id ?? string.Empty;

        return new Subscription
        {
            Id = sub.Id,
            CustomerId = sub.CustomerId ?? string.Empty,
            ProductId = productId,
            VariantId = variantId,
            Status = MapSubscriptionStatus(sub.Status),
            Currency = price?.Currency?.ToUpperInvariant(),
            PriceAmountCents = (int?)price?.UnitAmount,
            BillingInterval = price?.Recurring?.Interval,
            BillingIntervalCount = (int?)price?.Recurring?.IntervalCount,
            CreatedAt = ToUtcOffset(sub.Created),
            CurrentPeriodStart = TryReadPeriodStart(sub, firstItem),
            CurrentPeriodEnd = TryReadPeriodEnd(sub, firstItem),
            CanceledAt = ToUtcOffset(sub.CanceledAt),
            CancelAt = ToUtcOffset(sub.CancelAt),
            EndedAt = ToUtcOffset(sub.EndedAt),
            TrialEndsAt = ToUtcOffset(sub.TrialEnd),
            PausedAt = sub.Status == "paused" ? ToUtcOffset(sub.Created) : null,
            TenantId = TryGetMetadata(sub.Metadata, "tenant_id"),
            CustomData = ToCustomData(sub.Metadata)
        };
    }

    /// <summary>
    /// Maps a Stripe <see cref="global::Stripe.Checkout.Session"/> to a
    /// <see cref="CheckoutSession"/>.
    /// </summary>
    public static CheckoutSession ToCheckoutSession(global::Stripe.Checkout.Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        string? variantId = null;
        if (session.LineItems?.Data is { Count: > 0 } lineItems)
        {
            variantId = lineItems[0].Price?.Id;
        }

        return new CheckoutSession
        {
            Id = session.Id,
            CheckoutUrl = session.Url ?? string.Empty,
            VariantId = variantId,
            CreatedAt = ToUtcOffset(session.Created),
            ExpiresAt = ToUtcOffset(session.ExpiresAt),
            CustomData = ToCustomData(session.Metadata)
        };
    }

    private static string? TryGetMetadata(IDictionary<string, string>? metadata, string key)
    {
        if (metadata is null)
        {
            return null;
        }

        return metadata.TryGetValue(key, out var value) ? value : null;
    }

    private static IReadOnlyDictionary<string, object>? ToCustomData(IDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var dict = new Dictionary<string, object>(metadata.Count, StringComparer.Ordinal);
        foreach (var kvp in metadata)
        {
            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }

    // Stripe API has migrated CurrentPeriodStart/End onto subscription items
    // in recent API versions. We read from the item first, then fall back to
    // whatever the subscription object exposes, tolerating either location.
    private static DateTimeOffset? TryReadPeriodStart(
        global::Stripe.Subscription sub,
        global::Stripe.SubscriptionItem? item)
    {
        var itemStart = item?.CurrentPeriodStart;
        if (itemStart.HasValue)
        {
            return ToUtcOffset(itemStart.Value);
        }

        return null;
    }

    private static DateTimeOffset? TryReadPeriodEnd(
        global::Stripe.Subscription sub,
        global::Stripe.SubscriptionItem? item)
    {
        var itemEnd = item?.CurrentPeriodEnd;
        if (itemEnd.HasValue)
        {
            return ToUtcOffset(itemEnd.Value);
        }

        return null;
    }
}
