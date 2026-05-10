// -----------------------------------------------------------------------
// <copyright file="StripeMapperBehaviorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Billing.Models;
using Compendium.Adapters.Stripe.Configuration;
using FluentAssertions;
using Stripe;

namespace Compendium.Adapters.Stripe.Tests.Services;

/// <summary>
/// Behavioural tests for the internal <c>StripeMapper</c>. Exercises the full
/// mapping surface (<c>ToBillingCustomer</c>, <c>ToSubscription</c>,
/// <c>ToCheckoutSession</c>, <c>ToUtcOffset</c>) via reflection because the
/// mapper is intentionally <c>internal</c>.
/// </summary>
public sealed class StripeMapperBehaviorTests
{
    // ---------------------------------------------------------------------
    // ToUtcOffset
    // ---------------------------------------------------------------------

    [Fact]
    public void ToUtcOffset_DateTime_ReturnsUtcOffset()
    {
        // Arrange
        var input = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var actual = (DateTimeOffset)InvokeStatic("ToUtcOffset", new Type[] { typeof(DateTime) }, input)!;

        // Assert
        actual.Offset.Should().Be(TimeSpan.Zero);
        actual.UtcDateTime.Should().Be(DateTime.SpecifyKind(input, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcOffset_NullableDateTimeWithValue_ReturnsUtcOffset()
    {
        // Arrange
        DateTime? input = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var actual = (DateTimeOffset?)InvokeStatic("ToUtcOffset", new Type[] { typeof(DateTime?) }, input)!;

        // Assert
        actual.Should().NotBeNull();
        actual!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToUtcOffset_NullDateTime_ReturnsNull()
    {
        // Arrange
        DateTime? input = null;

        // Act
        var actual = (DateTimeOffset?)InvokeStatic("ToUtcOffset", new Type[] { typeof(DateTime?) }, input);

        // Assert
        actual.Should().BeNull();
    }

    // ---------------------------------------------------------------------
    // ToBillingCustomer
    // ---------------------------------------------------------------------

    [Fact]
    public void ToBillingCustomer_FullCustomer_MapsAllFields()
    {
        // Arrange
        var customer = new Customer
        {
            Id = "cus_full",
            Email = "alice@example.com",
            Name = "Alice",
            Address = new Address { City = "Lyon", State = "ARA", Country = "FR" },
            Created = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = "tenant-7",
                ["user_id"] = "user-7",
                ["plan"] = "enterprise"
            }
        };

        // Act
        var actual = (BillingCustomer)InvokeStatic(
            "ToBillingCustomer",
            new Type[] { typeof(Customer) },
            customer)!;

        // Assert
        actual.Id.Should().Be("cus_full");
        actual.Email.Should().Be("alice@example.com");
        actual.Name.Should().Be("Alice");
        actual.City.Should().Be("Lyon");
        actual.Region.Should().Be("ARA");
        actual.Country.Should().Be("FR");
        actual.TenantId.Should().Be("tenant-7");
        actual.UserId.Should().Be("user-7");
        actual.CustomData!["plan"].Should().Be("enterprise");
        actual.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToBillingCustomer_NoEmailNoMetadataNoAddress_FillsDefaults()
    {
        // Arrange — minimal customer envelope.
        var customer = new Customer
        {
            Id = "cus_min",
            Email = null,
            Created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = null
        };

        // Act
        var actual = (BillingCustomer)InvokeStatic(
            "ToBillingCustomer",
            new Type[] { typeof(Customer) },
            customer)!;

        // Assert
        actual.Email.Should().BeEmpty();
        actual.Name.Should().BeNull();
        actual.City.Should().BeNull();
        actual.Region.Should().BeNull();
        actual.Country.Should().BeNull();
        actual.TenantId.Should().BeNull();
        actual.UserId.Should().BeNull();
        actual.CustomData.Should().BeNull();
    }

    [Fact]
    public void ToBillingCustomer_EmptyMetadataDict_StillReturnsNullCustomData()
    {
        // Arrange — metadata is present but empty: ToCustomData() should return null.
        var customer = new Customer
        {
            Id = "cus_empty_meta",
            Email = "x@example.com",
            Created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var actual = (BillingCustomer)InvokeStatic(
            "ToBillingCustomer",
            new Type[] { typeof(Customer) },
            customer)!;

        // Assert
        actual.CustomData.Should().BeNull();
    }

    [Fact]
    public void ToBillingCustomer_NullCustomer_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => InvokeStatic("ToBillingCustomer", new Type[] { typeof(Customer) }, (Customer?)null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // ToSubscription
    // ---------------------------------------------------------------------

    [Fact]
    public void ToSubscription_FullSubscription_MapsItemPriceAndTimestamps()
    {
        // Arrange
        var item = new SubscriptionItem
        {
            Id = "si_1",
            Price = new Price
            {
                Id = "price_1",
                ProductId = "prod_1",
                Currency = "eur",
                UnitAmount = 999,
                Recurring = new PriceRecurring { Interval = "month", IntervalCount = 1 }
            },
            CurrentPeriodStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CurrentPeriodEnd = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var sub = new global::Stripe.Subscription
        {
            Id = "sub_1",
            CustomerId = "cus_1",
            Status = "active",
            Created = new DateTime(2024, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            CanceledAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CancelAt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            EndedAt = null,
            TrialEnd = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc),
            Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem> { item } },
            Metadata = new Dictionary<string, string> { ["tenant_id"] = "t-77", ["origin"] = "campaign-x" }
        };

        // Act
        var actual = (Compendium.Abstractions.Billing.Models.Subscription)InvokeStatic(
            "ToSubscription",
            new Type[] { typeof(global::Stripe.Subscription) },
            sub)!;

        // Assert
        actual.Id.Should().Be("sub_1");
        actual.CustomerId.Should().Be("cus_1");
        actual.ProductId.Should().Be("prod_1");
        actual.VariantId.Should().Be("price_1");
        actual.Currency.Should().Be("EUR");
        actual.PriceAmountCents.Should().Be(999);
        actual.BillingInterval.Should().Be("month");
        actual.BillingIntervalCount.Should().Be(1);
        actual.Status.Should().Be(BillingSubscriptionStatus.Active);
        actual.CanceledAt.Should().NotBeNull();
        actual.CancelAt.Should().NotBeNull();
        actual.EndedAt.Should().BeNull();
        actual.TrialEndsAt.Should().NotBeNull();
        actual.PausedAt.Should().BeNull();
        actual.CurrentPeriodStart.Should().NotBeNull();
        actual.CurrentPeriodEnd.Should().NotBeNull();
        actual.TenantId.Should().Be("t-77");
        actual.CustomData!["origin"].Should().Be("campaign-x");
    }

    [Fact]
    public void ToSubscription_PausedStatus_SetsPausedAt()
    {
        // Arrange
        var sub = new global::Stripe.Subscription
        {
            Id = "sub_paused",
            CustomerId = "cus_p",
            Status = "paused",
            Created = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem>() }
        };

        // Act
        var actual = (Compendium.Abstractions.Billing.Models.Subscription)InvokeStatic(
            "ToSubscription",
            new Type[] { typeof(global::Stripe.Subscription) },
            sub)!;

        // Assert
        actual.Status.Should().Be(BillingSubscriptionStatus.Paused);
        actual.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToSubscription_NoItems_LeavesProductAndVariantEmpty()
    {
        // Arrange
        var sub = new global::Stripe.Subscription
        {
            Id = "sub_empty",
            CustomerId = "cus_e",
            Status = "active",
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem>() }
        };

        // Act
        var actual = (Compendium.Abstractions.Billing.Models.Subscription)InvokeStatic(
            "ToSubscription",
            new Type[] { typeof(global::Stripe.Subscription) },
            sub)!;

        // Assert
        actual.ProductId.Should().BeEmpty();
        actual.VariantId.Should().BeEmpty();
        actual.Currency.Should().BeNull();
        actual.PriceAmountCents.Should().BeNull();
        actual.BillingInterval.Should().BeNull();
        actual.BillingIntervalCount.Should().BeNull();
        actual.CurrentPeriodStart.Should().BeNull();
        actual.CurrentPeriodEnd.Should().BeNull();
    }

    [Fact]
    public void ToSubscription_NullCustomerId_DefaultsToEmpty()
    {
        // Arrange
        var sub = new global::Stripe.Subscription
        {
            Id = "sub_no_cus",
            CustomerId = null,
            Status = "active",
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem>() }
        };

        // Act
        var actual = (Compendium.Abstractions.Billing.Models.Subscription)InvokeStatic(
            "ToSubscription",
            new Type[] { typeof(global::Stripe.Subscription) },
            sub)!;

        // Assert
        actual.CustomerId.Should().BeEmpty();
    }

    [Fact]
    public void ToSubscription_NullSubscription_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => InvokeStatic("ToSubscription", new Type[] { typeof(global::Stripe.Subscription) }, (global::Stripe.Subscription?)null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // ToCheckoutSession
    // ---------------------------------------------------------------------

    [Fact]
    public void ToCheckoutSession_WithLineItems_MapsVariantId()
    {
        // Arrange
        var session = new global::Stripe.Checkout.Session
        {
            Id = "cs_1",
            Url = "https://stripe.test/co/cs_1",
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2025, 1, 1, 1, 0, 0, DateTimeKind.Utc),
            LineItems = new StripeList<global::Stripe.LineItem>
            {
                Data = new List<global::Stripe.LineItem>
                {
                    new()
                    {
                        Id = "li_1",
                        Price = new Price { Id = "price_1" }
                    }
                }
            },
            Metadata = new Dictionary<string, string> { ["promo"] = "yes" }
        };

        // Act
        var actual = (CheckoutSession)InvokeStatic(
            "ToCheckoutSession",
            new Type[] { typeof(global::Stripe.Checkout.Session) },
            session)!;

        // Assert
        actual.Id.Should().Be("cs_1");
        actual.CheckoutUrl.Should().Be("https://stripe.test/co/cs_1");
        actual.VariantId.Should().Be("price_1");
        actual.CustomData!["promo"].Should().Be("yes");
        actual.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToCheckoutSession_NoLineItems_LeavesVariantIdNullAndUrlDefaultsToEmpty()
    {
        // Arrange
        var session = new global::Stripe.Checkout.Session
        {
            Id = "cs_2",
            Url = null,
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2025, 1, 1, 1, 0, 0, DateTimeKind.Utc),
            LineItems = null,
            Metadata = null
        };

        // Act
        var actual = (CheckoutSession)InvokeStatic(
            "ToCheckoutSession",
            new Type[] { typeof(global::Stripe.Checkout.Session) },
            session)!;

        // Assert
        actual.VariantId.Should().BeNull();
        actual.CheckoutUrl.Should().BeEmpty();
        actual.CustomData.Should().BeNull();
    }

    [Fact]
    public void ToCheckoutSession_EmptyLineItems_LeavesVariantIdNull()
    {
        // Arrange — Data list exists but is empty.
        var session = new global::Stripe.Checkout.Session
        {
            Id = "cs_3",
            Url = "https://stripe.test/co/cs_3",
            Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2025, 1, 1, 1, 0, 0, DateTimeKind.Utc),
            LineItems = new StripeList<global::Stripe.LineItem>
            {
                Data = new List<global::Stripe.LineItem>()
            },
            Metadata = null
        };

        // Act
        var actual = (CheckoutSession)InvokeStatic(
            "ToCheckoutSession",
            new Type[] { typeof(global::Stripe.Checkout.Session) },
            session)!;

        // Assert
        actual.VariantId.Should().BeNull();
    }

    [Fact]
    public void ToCheckoutSession_NullSession_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => InvokeStatic(
            "ToCheckoutSession",
            new Type[] { typeof(global::Stripe.Checkout.Session) },
            (global::Stripe.Checkout.Session?)null);

        // Assert
        act.Should().Throw<TargetInvocationException>()
           .WithInnerException<ArgumentNullException>();
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static object? InvokeStatic(string methodName, Type[] argTypes, params object?[] args)
    {
        var adapterAssembly = typeof(StripeOptions).Assembly;
        var mapperType = adapterAssembly.GetType("Compendium.Adapters.Stripe.Services.StripeMapper", throwOnError: true)!;
        var method = mapperType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: argTypes,
            modifiers: null)!;
        return method.Invoke(null, args);
    }
}
