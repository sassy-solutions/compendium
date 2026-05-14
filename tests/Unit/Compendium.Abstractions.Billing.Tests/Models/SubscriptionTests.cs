// -----------------------------------------------------------------------
// <copyright file="SubscriptionTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests.Models;

public class SubscriptionTests
{
    private static Subscription Build(
        string id = "sub-1",
        string customerId = "cust-1",
        string productId = "prod-1",
        string variantId = "var-1",
        BillingSubscriptionStatus status = BillingSubscriptionStatus.Active,
        DateTimeOffset? trialEndsAt = null) =>
        new()
        {
            Id = id,
            CustomerId = customerId,
            ProductId = productId,
            VariantId = variantId,
            Status = status,
            TrialEndsAt = trialEndsAt
        };

    [Fact]
    public void Subscription_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var subscription = Build();

        // Assert
        subscription.Id.Should().Be("sub-1");
        subscription.CustomerId.Should().Be("cust-1");
        subscription.ProductId.Should().Be("prod-1");
        subscription.VariantId.Should().Be("var-1");
        subscription.Status.Should().Be(BillingSubscriptionStatus.Active);
        subscription.ProductName.Should().BeNull();
        subscription.VariantName.Should().BeNull();
        subscription.BillingInterval.Should().BeNull();
        subscription.BillingIntervalCount.Should().BeNull();
        subscription.PriceAmountCents.Should().BeNull();
        subscription.Currency.Should().BeNull();
        subscription.TenantId.Should().BeNull();
        subscription.CustomData.Should().BeNull();
    }

    [Fact]
    public void Subscription_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-30);
        var customData = new Dictionary<string, object> { ["plan_tier"] = "gold" };

        // Act
        var subscription = new Subscription
        {
            Id = "sub-2",
            CustomerId = "cust-2",
            ProductId = "prod-2",
            VariantId = "var-2",
            ProductName = "Gold Plan",
            VariantName = "Annual",
            Status = BillingSubscriptionStatus.OnTrial,
            BillingInterval = "year",
            BillingIntervalCount = 1,
            PriceAmountCents = 9900,
            Currency = "EUR",
            TenantId = "tenant-2",
            CustomData = customData,
            CreatedAt = createdAt,
            UpdatedAt = createdAt.AddDays(1),
            CurrentPeriodStart = createdAt,
            CurrentPeriodEnd = createdAt.AddYears(1),
            CancelAt = createdAt.AddYears(1),
            CanceledAt = createdAt.AddDays(2),
            EndedAt = createdAt.AddDays(3),
            TrialEndsAt = createdAt.AddDays(14),
            PausedAt = createdAt.AddDays(4),
            ResumesAt = createdAt.AddDays(20)
        };

        // Assert
        subscription.ProductName.Should().Be("Gold Plan");
        subscription.VariantName.Should().Be("Annual");
        subscription.Status.Should().Be(BillingSubscriptionStatus.OnTrial);
        subscription.BillingInterval.Should().Be("year");
        subscription.BillingIntervalCount.Should().Be(1);
        subscription.PriceAmountCents.Should().Be(9900);
        subscription.Currency.Should().Be("EUR");
        subscription.TenantId.Should().Be("tenant-2");
        subscription.CustomData.Should().BeSameAs(customData);
        subscription.CreatedAt.Should().Be(createdAt);
        subscription.UpdatedAt.Should().Be(createdAt.AddDays(1));
        subscription.CurrentPeriodStart.Should().Be(createdAt);
        subscription.CurrentPeriodEnd.Should().Be(createdAt.AddYears(1));
        subscription.CancelAt.Should().Be(createdAt.AddYears(1));
        subscription.CanceledAt.Should().Be(createdAt.AddDays(2));
        subscription.EndedAt.Should().Be(createdAt.AddDays(3));
        subscription.PausedAt.Should().Be(createdAt.AddDays(4));
        subscription.ResumesAt.Should().Be(createdAt.AddDays(20));
    }

    [Fact]
    public void IsInTrial_WhenTrialEndsAtIsNull_ReturnsFalse()
    {
        // Arrange
        var subscription = Build(trialEndsAt: null);

        // Act & Assert
        subscription.IsInTrial.Should().BeFalse();
    }

    [Fact]
    public void IsInTrial_WhenTrialEndsAtInTheFuture_ReturnsTrue()
    {
        // Arrange
        var subscription = Build(trialEndsAt: DateTimeOffset.UtcNow.AddDays(7));

        // Act & Assert
        subscription.IsInTrial.Should().BeTrue();
    }

    [Fact]
    public void IsInTrial_WhenTrialEndsAtInThePast_ReturnsFalse()
    {
        // Arrange
        var subscription = Build(trialEndsAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert
        subscription.IsInTrial.Should().BeFalse();
    }

    [Theory]
    [InlineData(BillingSubscriptionStatus.Active, true)]
    [InlineData(BillingSubscriptionStatus.OnTrial, true)]
    [InlineData(BillingSubscriptionStatus.Paused, false)]
    [InlineData(BillingSubscriptionStatus.PastDue, false)]
    [InlineData(BillingSubscriptionStatus.Unpaid, false)]
    [InlineData(BillingSubscriptionStatus.Cancelled, false)]
    [InlineData(BillingSubscriptionStatus.Expired, false)]
    public void IsActive_ForEachStatus_ReturnsExpected(BillingSubscriptionStatus status, bool expected)
    {
        // Arrange
        var subscription = Build(status: status);

        // Act
        var actual = subscription.IsActive;

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Subscription_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = Build();
        var second = Build();

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Subscription_TwoInstancesDifferingByStatus_AreNotEqual()
    {
        // Arrange
        var first = Build(status: BillingSubscriptionStatus.Active);
        var second = Build(status: BillingSubscriptionStatus.Paused);

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void Subscription_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = Build(status: BillingSubscriptionStatus.Active);

        // Act
        var canceled = original with { Status = BillingSubscriptionStatus.Cancelled };

        // Assert
        canceled.Status.Should().Be(BillingSubscriptionStatus.Cancelled);
        original.Status.Should().Be(BillingSubscriptionStatus.Active);
    }
}

public class BillingSubscriptionStatusTests
{
    [Fact]
    public void BillingSubscriptionStatus_DeclaresAllExpectedValues()
    {
        // Act
        var values = Enum.GetValues<BillingSubscriptionStatus>();

        // Assert
        values.Should().Contain(new[]
        {
            BillingSubscriptionStatus.Active,
            BillingSubscriptionStatus.OnTrial,
            BillingSubscriptionStatus.Paused,
            BillingSubscriptionStatus.PastDue,
            BillingSubscriptionStatus.Unpaid,
            BillingSubscriptionStatus.Cancelled,
            BillingSubscriptionStatus.Expired
        });
    }

    [Theory]
    [InlineData(BillingSubscriptionStatus.Active, 0)]
    [InlineData(BillingSubscriptionStatus.OnTrial, 1)]
    [InlineData(BillingSubscriptionStatus.Paused, 2)]
    [InlineData(BillingSubscriptionStatus.PastDue, 3)]
    [InlineData(BillingSubscriptionStatus.Unpaid, 4)]
    [InlineData(BillingSubscriptionStatus.Cancelled, 5)]
    [InlineData(BillingSubscriptionStatus.Expired, 6)]
    public void BillingSubscriptionStatus_NumericValues_AreStable(BillingSubscriptionStatus status, int expected)
    {
        // Act & Assert
        ((int)status).Should().Be(expected);
    }
}
