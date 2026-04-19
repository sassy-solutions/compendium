// -----------------------------------------------------------------------
// <copyright file="StripeMapperTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing.Models;
using FluentAssertions;

namespace Compendium.Adapters.Stripe.Tests.Services;

/// <summary>
/// Unit tests for the internal Stripe mapper. These exercise the subscription
/// status mapping which is the most behaviourally sensitive part of the adapter.
/// </summary>
public class StripeMapperTests
{
    // The mapper is internal; reach it via the InternalsVisibleTo-free reflection
    // path on the adapter assembly to avoid making it public for tests. Rather
    // than reflection, we resolve it via the public type in the same assembly:
    // the DI extension sets StripeConfiguration.ApiKey using these utilities.
    // Instead, we test the enum mapping through a thin wrapper defined below.

    [Theory]
    [InlineData("active", BillingSubscriptionStatus.Active)]
    [InlineData("trialing", BillingSubscriptionStatus.OnTrial)]
    [InlineData("past_due", BillingSubscriptionStatus.PastDue)]
    [InlineData("unpaid", BillingSubscriptionStatus.Unpaid)]
    [InlineData("canceled", BillingSubscriptionStatus.Cancelled)]
    [InlineData("incomplete_expired", BillingSubscriptionStatus.Cancelled)]
    [InlineData("paused", BillingSubscriptionStatus.Paused)]
    [InlineData("incomplete", BillingSubscriptionStatus.PastDue)]
    public void MapSubscriptionStatus_KnownValues_MapCorrectly(string stripeStatus, BillingSubscriptionStatus expected)
    {
        var actual = InvokeMapStatus(stripeStatus);

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("gibberish")]
    [InlineData("future_unknown_state")]
    public void MapSubscriptionStatus_UnknownValues_DefaultToActive(string? stripeStatus)
    {
        var actual = InvokeMapStatus(stripeStatus);

        actual.Should().Be(BillingSubscriptionStatus.Active);
    }

    [Fact]
    public void MapSubscriptionStatus_Null_DefaultsToActive()
    {
        var actual = InvokeMapStatus(null);

        actual.Should().Be(BillingSubscriptionStatus.Active);
    }

    // StripeMapper is internal; reflect through its type to avoid exposing it.
    private static BillingSubscriptionStatus InvokeMapStatus(string? stripeStatus)
    {
        var adapterAssembly = typeof(global::Compendium.Adapters.Stripe.Configuration.StripeOptions).Assembly;
        var mapperType = adapterAssembly.GetType("Compendium.Adapters.Stripe.Services.StripeMapper", throwOnError: true)!;
        var method = mapperType.GetMethod(
            "MapSubscriptionStatus",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (BillingSubscriptionStatus)method.Invoke(null, new object?[] { stripeStatus })!;
    }
}
