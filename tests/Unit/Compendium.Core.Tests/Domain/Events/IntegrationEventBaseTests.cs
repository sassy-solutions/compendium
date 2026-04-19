// -----------------------------------------------------------------------
// <copyright file="IntegrationEventBaseTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Events.Integration;

namespace Compendium.Core.Tests.Domain.Events;

/// <summary>
/// Unit tests for the <see cref="IntegrationEventBase"/> class and derived events.
/// </summary>
public class IntegrationEventBaseTests
{
    #region IntegrationEventBase Tests

    private sealed record TestIntegrationEvent : IntegrationEventBase
    {
        public override string EventType => "test.event";
        public string TestData { get; init; } = string.Empty;
    }

    [Fact]
    public void IntegrationEventBase_Constructor_WithDefaults_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var beforeCreation = DateTimeOffset.UtcNow;
        var integrationEvent = new TestIntegrationEvent();
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        integrationEvent.EventId.Should().NotBe(Guid.Empty);
        integrationEvent.EventType.Should().Be("test.event");
        integrationEvent.EventVersion.Should().Be(1);
        integrationEvent.OccurredOn.Should().BeOnOrAfter(beforeCreation);
        integrationEvent.OccurredOn.Should().BeOnOrBefore(afterCreation);
        integrationEvent.CorrelationId.Should().BeNull();
        integrationEvent.CausationId.Should().BeNull();
        integrationEvent.TenantId.Should().BeNull();
        integrationEvent.SourceSystem.Should().BeNull();
    }

    [Fact]
    public void IntegrationEventBase_WithTenantId_SetsPropertyCorrectly()
    {
        // Arrange & Act
        var integrationEvent = new TestIntegrationEvent
        {
            TenantId = "tenant-123"
        };

        // Assert
        integrationEvent.TenantId.Should().Be("tenant-123");
    }

    [Fact]
    public void IntegrationEventBase_WithSourceSystem_SetsPropertyCorrectly()
    {
        // Arrange & Act
        var integrationEvent = new TestIntegrationEvent
        {
            SourceSystem = "billing-service"
        };

        // Assert
        integrationEvent.SourceSystem.Should().Be("billing-service");
    }

    [Fact]
    public void IntegrationEventBase_EventId_IsUnique()
    {
        // Act
        var event1 = new TestIntegrationEvent();
        var event2 = new TestIntegrationEvent();

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void IntegrationEventBase_OccurredOn_IsUtc()
    {
        // Act
        var integrationEvent = new TestIntegrationEvent();

        // Assert
        integrationEvent.OccurredOn.Offset.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Billing Integration Events Tests

    [Fact]
    public void SubscriptionCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new SubscriptionCreatedEvent(
            SubscriptionId: "sub-123",
            CustomerId: "cust-456",
            PlanId: "plan-789",
            Status: "active",
            BillingPeriodStart: DateTimeOffset.UtcNow,
            BillingPeriodEnd: DateTimeOffset.UtcNow.AddMonths(1));

        // Assert
        evt.EventType.Should().Be("billing.subscription.created");
        evt.SubscriptionId.Should().Be("sub-123");
        evt.CustomerId.Should().Be("cust-456");
        evt.PlanId.Should().Be("plan-789");
        evt.Status.Should().Be("active");
    }

    [Fact]
    public void SubscriptionCancelledEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new SubscriptionCancelledEvent(
            SubscriptionId: "sub-123",
            CustomerId: "cust-456",
            CancellationReason: "User requested",
            EffectiveDate: DateTimeOffset.UtcNow.AddDays(30),
            ImmediateCancel: false);

        // Assert
        evt.EventType.Should().Be("billing.subscription.cancelled");
        evt.CancellationReason.Should().Be("User requested");
        evt.ImmediateCancel.Should().BeFalse();
    }

    [Fact]
    public void PaymentSucceededEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new PaymentSucceededEvent(
            PaymentId: "pay-123",
            CustomerId: "cust-456",
            SubscriptionId: "sub-789",
            Amount: 9999,
            Currency: "USD",
            PaymentMethod: "card",
            InvoiceId: "inv-001");

        // Assert
        evt.EventType.Should().Be("billing.payment.succeeded");
        evt.Amount.Should().Be(9999);
        evt.Currency.Should().Be("USD");
    }

    [Fact]
    public void PaymentFailedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new PaymentFailedEvent(
            PaymentId: "pay-123",
            CustomerId: "cust-456",
            SubscriptionId: "sub-789",
            Amount: 9999,
            Currency: "USD",
            FailureCode: "card_declined",
            FailureMessage: "Your card was declined",
            AttemptCount: 1);

        // Assert
        evt.EventType.Should().Be("billing.payment.failed");
        evt.FailureCode.Should().Be("card_declined");
        evt.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void RefundIssuedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new RefundIssuedEvent(
            RefundId: "ref-123",
            PaymentId: "pay-456",
            CustomerId: "cust-789",
            Amount: 5000,
            Currency: "USD",
            Reason: "Customer request",
            IsPartial: true);

        // Assert
        evt.EventType.Should().Be("billing.refund.issued");
        evt.IsPartial.Should().BeTrue();
    }

    #endregion

    #region License Integration Events Tests

    [Fact]
    public void LicenseCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new LicenseCreatedEvent(
            LicenseId: "lic-123",
            LicenseKey: "XXXX-YYYY-ZZZZ",
            CustomerId: "cust-456",
            ProductId: "prod-789",
            Status: "active",
            ExpiresAt: DateTimeOffset.UtcNow.AddYears(1),
            ActivationLimit: 5);

        // Assert
        evt.EventType.Should().Be("license.created");
        evt.LicenseKey.Should().Be("XXXX-YYYY-ZZZZ");
        evt.ActivationLimit.Should().Be(5);
    }

    [Fact]
    public void LicenseActivatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new LicenseActivatedEvent(
            LicenseId: "lic-123",
            LicenseKey: "XXXX-YYYY-ZZZZ",
            InstanceId: "inst-001",
            InstanceName: "Work PC",
            ActivationCount: 1,
            ActivationLimit: 5,
            ActivatedAt: DateTimeOffset.UtcNow);

        // Assert
        evt.EventType.Should().Be("license.activated");
        evt.InstanceId.Should().Be("inst-001");
        evt.InstanceName.Should().Be("Work PC");
    }

    [Fact]
    public void LicenseDeactivatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new LicenseDeactivatedEvent(
            LicenseId: "lic-123",
            LicenseKey: "XXXX-YYYY-ZZZZ",
            InstanceId: "inst-001",
            ActivationCount: 0,
            DeactivatedAt: DateTimeOffset.UtcNow);

        // Assert
        evt.EventType.Should().Be("license.deactivated");
    }

    #endregion

    #region Identity Integration Events Tests

    [Fact]
    public void UserCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new UserCreatedEvent(
            UserId: "user-123",
            Email: "test@example.com",
            Username: "testuser",
            FirstName: "Test",
            LastName: "User",
            IsEmailVerified: false);

        // Assert
        evt.EventType.Should().Be("identity.user.created");
        evt.Email.Should().Be("test@example.com");
        evt.Username.Should().Be("testuser");
    }

    [Fact]
    public void UserLoggedInEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new UserLoggedInEvent(
            UserId: "user-123",
            Email: "test@example.com",
            LoginAt: DateTimeOffset.UtcNow,
            IpAddress: "192.168.1.1",
            UserAgent: "Mozilla/5.0",
            AuthMethod: "password");

        // Assert
        evt.EventType.Should().Be("identity.user.logged_in");
        evt.AuthMethod.Should().Be("password");
    }

    [Fact]
    public void UserRoleAssignedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new UserRoleAssignedEvent(
            UserId: "user-123",
            Email: "test@example.com",
            RoleId: "role-456",
            RoleName: "Admin",
            AssignedAt: DateTimeOffset.UtcNow,
            AssignedBy: "admin-user");

        // Assert
        evt.EventType.Should().Be("identity.user.role_assigned");
        evt.RoleName.Should().Be("Admin");
    }

    [Fact]
    public void OrganizationCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new OrganizationCreatedEvent(
            OrganizationId: "org-123",
            Name: "Acme Corp",
            Domain: "acme.com",
            OwnerId: "user-456");

        // Assert
        evt.EventType.Should().Be("identity.organization.created");
        evt.Name.Should().Be("Acme Corp");
    }

    #endregion

    #region Marketing Integration Events Tests

    [Fact]
    public void SubscriberSubscribedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new SubscriberSubscribedEvent(
            SubscriberId: "sub-123",
            Email: "test@example.com",
            ListId: "list-456",
            ListName: "Newsletter",
            SubscribedAt: DateTimeOffset.UtcNow,
            SubscriptionMethod: "web_form",
            IsDoubleOptIn: true);

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.subscribed");
        evt.SubscriptionMethod.Should().Be("web_form");
        evt.IsDoubleOptIn.Should().BeTrue();
    }

    [Fact]
    public void SubscriberUnsubscribedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new SubscriberUnsubscribedEvent(
            SubscriberId: "sub-123",
            Email: "test@example.com",
            ListId: "list-456",
            ListName: "Newsletter",
            UnsubscribedAt: DateTimeOffset.UtcNow,
            UnsubscribeReason: "No longer interested",
            UnsubscribeMethod: "link_click");

        // Assert
        evt.EventType.Should().Be("marketing.subscriber.unsubscribed");
        evt.UnsubscribeReason.Should().Be("No longer interested");
    }

    [Fact]
    public void CampaignSentEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new CampaignSentEvent(
            CampaignId: "camp-123",
            CampaignName: "Summer Sale",
            Subject: "Don't miss our summer sale!",
            ListIds: new[] { "list-1", "list-2" },
            RecipientCount: 10000,
            SentAt: DateTimeOffset.UtcNow);

        // Assert
        evt.EventType.Should().Be("marketing.campaign.sent");
        evt.RecipientCount.Should().Be(10000);
        evt.ListIds.Should().HaveCount(2);
    }

    #endregion

    #region Tenancy Integration Events Tests

    [Fact]
    public void TenantCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new TenantCreatedEvent(
            TenantId: "tenant-123",
            Name: "Acme Corp",
            Identifier: "acme",
            OwnerId: "user-456",
            Plan: "pro",
            IsActive: true);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.created");
        evt.Identifier.Should().Be("acme");
        evt.Plan.Should().Be("pro");
    }

    [Fact]
    public void TenantPlanChangedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new TenantPlanChangedEvent(
            TenantId: "tenant-123",
            Name: "Acme Corp",
            OldPlan: "starter",
            NewPlan: "pro",
            ChangeType: "upgrade",
            ChangedAt: DateTimeOffset.UtcNow);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.plan_changed");
        evt.ChangeType.Should().Be("upgrade");
    }

    [Fact]
    public void TenantQuotaExceededEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var evt = new TenantQuotaExceededEvent(
            TenantId: "tenant-123",
            Name: "Acme Corp",
            QuotaType: "storage",
            CurrentUsage: 11000000000,
            Limit: 10000000000,
            ExceededAt: DateTimeOffset.UtcNow);

        // Assert
        evt.EventType.Should().Be("tenancy.tenant.quota_exceeded");
        evt.QuotaType.Should().Be("storage");
        evt.CurrentUsage.Should().BeGreaterThan(evt.Limit);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void IntegrationEvents_CanBeSerializedToJson()
    {
        // Arrange
        var evt = new SubscriptionCreatedEvent(
            SubscriptionId: "sub-123",
            CustomerId: "cust-456",
            PlanId: "plan-789",
            Status: "active",
            BillingPeriodStart: DateTimeOffset.UtcNow,
            BillingPeriodEnd: DateTimeOffset.UtcNow.AddMonths(1))
        {
            TenantId = "tenant-001",
            SourceSystem = "billing"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(evt);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SubscriptionCreatedEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.SubscriptionId.Should().Be("sub-123");
        deserialized.CustomerId.Should().Be("cust-456");
        deserialized.TenantId.Should().Be("tenant-001");
        deserialized.SourceSystem.Should().Be("billing");
    }

    #endregion

    #region Performance Tests

    [Theory]
    [InlineData(1000)]
    public void IntegrationEvent_Creation_PerformanceTest(int iterations)
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = new SubscriptionCreatedEvent(
                SubscriptionId: $"sub-{i}",
                CustomerId: $"cust-{i}",
                PlanId: "plan-123",
                Status: "active",
                BillingPeriodStart: DateTimeOffset.UtcNow,
                BillingPeriodEnd: DateTimeOffset.UtcNow.AddMonths(1));
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Integration event creation should be fast");
    }

    [Fact]
    public void IntegrationEvent_ConcurrentCreation_ThreadSafe()
    {
        // Arrange
        var events = new List<SubscriptionCreatedEvent>();
        var lockObject = new object();

        // Act
        Parallel.For(0, 100, i =>
        {
            var evt = new SubscriptionCreatedEvent(
                SubscriptionId: $"sub-{i}",
                CustomerId: $"cust-{i}",
                PlanId: "plan-123",
                Status: "active",
                BillingPeriodStart: DateTimeOffset.UtcNow,
                BillingPeriodEnd: DateTimeOffset.UtcNow.AddMonths(1));
            lock (lockObject)
            {
                events.Add(evt);
            }
        });

        // Assert
        events.Should().HaveCount(100);
        events.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
    }

    #endregion
}
