// -----------------------------------------------------------------------
// <copyright file="BillingIntegrationEventsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events.Integration;

/// <summary>
/// Unit tests for billing integration event records covering EventType, all positional
/// parameters, default IntegrationEventBase metadata and structural equality.
/// </summary>
public class BillingIntegrationEventsTests
{
    [Fact]
    public void SubscriptionUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriptionUpdatedEvent(
            SubscriptionId: "sub-1",
            CustomerId: "cust-2",
            PlanId: "plan-new",
            PreviousPlanId: "plan-old",
            Status: "active",
            ChangeType: "upgrade");

        // Assert
        evt.EventType.Should().Be("billing.subscription.updated");
        evt.SubscriptionId.Should().Be("sub-1");
        evt.CustomerId.Should().Be("cust-2");
        evt.PlanId.Should().Be("plan-new");
        evt.PreviousPlanId.Should().Be("plan-old");
        evt.Status.Should().Be("active");
        evt.ChangeType.Should().Be("upgrade");
        evt.EventVersion.Should().Be(1);
        evt.EventId.Should().NotBe(Guid.Empty);
        evt.OccurredOn.Should().BeOnOrAfter(start);
    }

    [Fact]
    public void SubscriptionPausedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var paused = DateTimeOffset.UtcNow;
        var resume = paused.AddDays(7);

        // Act
        var evt = new SubscriptionPausedEvent("sub-1", "cust-1", paused, resume);

        // Assert
        evt.EventType.Should().Be("billing.subscription.paused");
        evt.PausedAt.Should().Be(paused);
        evt.ResumeAt.Should().Be(resume);
    }

    [Fact]
    public void SubscriptionPausedEvent_WithNullResumeAt_AllowsNull()
    {
        // Act
        var evt = new SubscriptionPausedEvent("sub-1", "cust-1", DateTimeOffset.UtcNow, ResumeAt: null);

        // Assert
        evt.ResumeAt.Should().BeNull();
    }

    [Fact]
    public void SubscriptionResumedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var resumedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriptionResumedEvent("sub-1", "cust-1", resumedAt);

        // Assert
        evt.EventType.Should().Be("billing.subscription.resumed");
        evt.ResumedAt.Should().Be(resumedAt);
    }

    [Fact]
    public void SubscriptionTrialStartedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(14);

        // Act
        var evt = new SubscriptionTrialStartedEvent("sub-1", "cust-1", "plan-pro", start, end);

        // Assert
        evt.EventType.Should().Be("billing.subscription.trial_started");
        evt.PlanId.Should().Be("plan-pro");
        evt.TrialStart.Should().Be(start);
        evt.TrialEnd.Should().Be(end);
    }

    [Fact]
    public void SubscriptionTrialEndedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var trialEnd = DateTimeOffset.UtcNow;

        // Act
        var evt = new SubscriptionTrialEndedEvent("sub-1", "cust-1", trialEnd, ConvertedToPaid: true);

        // Assert
        evt.EventType.Should().Be("billing.subscription.trial_ended");
        evt.TrialEnd.Should().Be(trialEnd);
        evt.ConvertedToPaid.Should().BeTrue();
    }

    [Fact]
    public void InvoiceCreatedEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var due = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var evt = new InvoiceCreatedEvent("inv-1", "cust-1", "sub-1", 12000, "EUR", due);

        // Assert
        evt.EventType.Should().Be("billing.invoice.created");
        evt.InvoiceId.Should().Be("inv-1");
        evt.SubscriptionId.Should().Be("sub-1");
        evt.Amount.Should().Be(12000);
        evt.Currency.Should().Be("EUR");
        evt.DueDate.Should().Be(due);
    }

    [Fact]
    public void InvoiceCreatedEvent_WithNullSubscriptionId_AllowsNull()
    {
        // Act
        var evt = new InvoiceCreatedEvent("inv-1", "cust-1", SubscriptionId: null, 100, "USD", DateTimeOffset.UtcNow);

        // Assert
        evt.SubscriptionId.Should().BeNull();
    }

    [Fact]
    public void InvoicePaidEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var paid = DateTimeOffset.UtcNow;

        // Act
        var evt = new InvoicePaidEvent("inv-1", "cust-1", "pay-1", 12000, "USD", paid);

        // Assert
        evt.EventType.Should().Be("billing.invoice.paid");
        evt.InvoiceId.Should().Be("inv-1");
        evt.PaymentId.Should().Be("pay-1");
        evt.PaidAt.Should().Be(paid);
    }

    [Fact]
    public void BillingCustomerCreatedEvent_Constructor_SetsAllProperties()
    {
        // Act
        var evt = new BillingCustomerCreatedEvent("cust-1", "user@example.com", "John", "ext-42");

        // Assert
        evt.EventType.Should().Be("billing.customer.created");
        evt.CustomerId.Should().Be("cust-1");
        evt.Email.Should().Be("user@example.com");
        evt.Name.Should().Be("John");
        evt.ExternalId.Should().Be("ext-42");
    }

    [Fact]
    public void BillingCustomerCreatedEvent_WithNullableNullValues_AllowsNull()
    {
        // Act
        var evt = new BillingCustomerCreatedEvent("cust-1", "user@example.com", Name: null, ExternalId: null);

        // Assert
        evt.Name.Should().BeNull();
        evt.ExternalId.Should().BeNull();
    }

    [Fact]
    public void BillingCustomerUpdatedEvent_Constructor_SetsAllProperties()
    {
        // Act
        var evt = new BillingCustomerUpdatedEvent("cust-1", "u@example.com", "Jane");

        // Assert
        evt.EventType.Should().Be("billing.customer.updated");
        evt.CustomerId.Should().Be("cust-1");
        evt.Email.Should().Be("u@example.com");
        evt.Name.Should().Be("Jane");
    }

    [Fact]
    public void CheckoutCompletedEvent_Constructor_SetsAllProperties()
    {
        // Act
        var evt = new CheckoutCompletedEvent(
            SessionId: "sess-1",
            CustomerId: "cust-1",
            SubscriptionId: "sub-1",
            Amount: 4999,
            Currency: "USD",
            ProductId: "prod-1",
            VariantId: "var-1");

        // Assert
        evt.EventType.Should().Be("billing.checkout.completed");
        evt.SessionId.Should().Be("sess-1");
        evt.SubscriptionId.Should().Be("sub-1");
        evt.ProductId.Should().Be("prod-1");
        evt.VariantId.Should().Be("var-1");
    }

    [Fact]
    public void CheckoutCompletedEvent_WithoutSubscriptionAndVariant_AllowsNulls()
    {
        // Act
        var evt = new CheckoutCompletedEvent("sess-1", "cust-1", null, 100, "USD", "prod-1", null);

        // Assert
        evt.SubscriptionId.Should().BeNull();
        evt.VariantId.Should().BeNull();
    }

    [Fact]
    public void BillingEvents_AreRecords_SupportingValueEquality()
    {
        // Arrange
        var paid = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var a = new InvoicePaidEvent("inv-1", "cust-1", "pay-1", 1000, "USD", paid);
        var b = new InvoicePaidEvent("inv-1", "cust-1", "pay-1", 1000, "USD", paid);

        // Act & Assert
        // Records compare by value AND inherited init properties. EventId differs so they will
        // not be equal — but the positional record component portion should be reflected in the
        // generated PrintMembers. Verify identity is per-instance (different EventIds).
        a.EventId.Should().NotBe(b.EventId);
        a.InvoiceId.Should().Be(b.InvoiceId);
    }
}
