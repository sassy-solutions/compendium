// -----------------------------------------------------------------------
// <copyright file="BillingCustomerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests.Models;

public class BillingCustomerTests
{
    [Fact]
    public void BillingCustomer_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var customer = new BillingCustomer
        {
            Id = "cust-1",
            Email = "user@example.com"
        };

        // Assert
        customer.Id.Should().Be("cust-1");
        customer.Email.Should().Be("user@example.com");
        customer.StoreId.Should().BeNull();
        customer.Name.Should().BeNull();
        customer.City.Should().BeNull();
        customer.Region.Should().BeNull();
        customer.Country.Should().BeNull();
        customer.TotalRevenueCents.Should().BeNull();
        customer.Currency.Should().BeNull();
        customer.TenantId.Should().BeNull();
        customer.UserId.Should().BeNull();
        customer.CustomData.Should().BeNull();
        customer.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void BillingCustomer_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var customData = new Dictionary<string, object> { ["plan"] = "pro" };

        // Act
        var customer = new BillingCustomer
        {
            Id = "cust-2",
            StoreId = "store-1",
            Name = "Alice",
            Email = "alice@example.com",
            City = "Paris",
            Region = "IDF",
            Country = "FR",
            TotalRevenueCents = 12500,
            Currency = "EUR",
            TenantId = "tenant-1",
            UserId = "user-1",
            CustomData = customData,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        customer.Id.Should().Be("cust-2");
        customer.StoreId.Should().Be("store-1");
        customer.Name.Should().Be("Alice");
        customer.Email.Should().Be("alice@example.com");
        customer.City.Should().Be("Paris");
        customer.Region.Should().Be("IDF");
        customer.Country.Should().Be("FR");
        customer.TotalRevenueCents.Should().Be(12500);
        customer.Currency.Should().Be("EUR");
        customer.TenantId.Should().Be("tenant-1");
        customer.UserId.Should().Be("user-1");
        customer.CustomData.Should().BeSameAs(customData);
        customer.CreatedAt.Should().Be(createdAt);
        customer.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void BillingCustomer_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var first = new BillingCustomer { Id = "cust-1", Email = "u@e.com" };
        var second = new BillingCustomer { Id = "cust-1", Email = "u@e.com" };

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void BillingCustomer_TwoInstancesWithDifferentIds_AreNotEqual()
    {
        // Arrange
        var first = new BillingCustomer { Id = "cust-1", Email = "u@e.com" };
        var second = new BillingCustomer { Id = "cust-2", Email = "u@e.com" };

        // Act & Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void BillingCustomer_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = new BillingCustomer { Id = "cust-1", Email = "old@e.com" };

        // Act
        var modified = original with { Email = "new@e.com" };

        // Assert
        modified.Should().NotBeSameAs(original);
        modified.Email.Should().Be("new@e.com");
        original.Email.Should().Be("old@e.com");
    }
}

public class UpsertCustomerRequestTests
{
    [Fact]
    public void UpsertCustomerRequest_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var request = new UpsertCustomerRequest
        {
            Email = "user@example.com"
        };

        // Assert
        request.Email.Should().Be("user@example.com");
        request.Name.Should().BeNull();
        request.City.Should().BeNull();
        request.Region.Should().BeNull();
        request.Country.Should().BeNull();
        request.TenantId.Should().BeNull();
        request.UserId.Should().BeNull();
        request.CustomData.Should().BeNull();
    }

    [Fact]
    public void UpsertCustomerRequest_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var customData = new Dictionary<string, object> { ["source"] = "app" };

        // Act
        var request = new UpsertCustomerRequest
        {
            Email = "alice@example.com",
            Name = "Alice",
            City = "Paris",
            Region = "IDF",
            Country = "FR",
            TenantId = "tenant-1",
            UserId = "user-1",
            CustomData = customData
        };

        // Assert
        request.Email.Should().Be("alice@example.com");
        request.Name.Should().Be("Alice");
        request.City.Should().Be("Paris");
        request.Region.Should().Be("IDF");
        request.Country.Should().Be("FR");
        request.TenantId.Should().Be("tenant-1");
        request.UserId.Should().Be("user-1");
        request.CustomData.Should().BeSameAs(customData);
    }

    [Fact]
    public void UpsertCustomerRequest_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = new UpsertCustomerRequest { Email = "u@e.com", Name = "U" };
        var second = new UpsertCustomerRequest { Email = "u@e.com", Name = "U" };

        // Act & Assert
        first.Should().Be(second);
    }

    [Fact]
    public void UpsertCustomerRequest_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = new UpsertCustomerRequest { Email = "old@e.com" };

        // Act
        var modified = original with { Email = "new@e.com" };

        // Assert
        modified.Email.Should().Be("new@e.com");
        original.Email.Should().Be("old@e.com");
    }
}
