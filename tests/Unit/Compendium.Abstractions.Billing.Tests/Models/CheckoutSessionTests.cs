// -----------------------------------------------------------------------
// <copyright file="CheckoutSessionTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests.Models;

public class CheckoutSessionTests
{
    [Fact]
    public void CheckoutSession_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var session = new CheckoutSession
        {
            Id = "ck-1",
            CheckoutUrl = "https://checkout.example/pay/ck-1"
        };

        // Assert
        session.Id.Should().Be("ck-1");
        session.CheckoutUrl.Should().Be("https://checkout.example/pay/ck-1");
        session.StoreId.Should().BeNull();
        session.VariantId.Should().BeNull();
        session.ExpiresAt.Should().BeNull();
        session.CustomData.Should().BeNull();
    }

    [Fact]
    public void CheckoutSession_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(1);
        var customData = new Dictionary<string, object> { ["referrer"] = "newsletter" };

        // Act
        var session = new CheckoutSession
        {
            Id = "ck-2",
            CheckoutUrl = "https://checkout.example/pay/ck-2",
            StoreId = "store-1",
            VariantId = "var-1",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            CustomData = customData
        };

        // Assert
        session.Id.Should().Be("ck-2");
        session.StoreId.Should().Be("store-1");
        session.VariantId.Should().Be("var-1");
        session.CreatedAt.Should().Be(createdAt);
        session.ExpiresAt.Should().Be(expiresAt);
        session.CustomData.Should().BeSameAs(customData);
    }

    [Fact]
    public void CheckoutSession_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = new CheckoutSession { Id = "ck-1", CheckoutUrl = "https://x" };
        var second = new CheckoutSession { Id = "ck-1", CheckoutUrl = "https://x" };

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void CheckoutSession_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = new CheckoutSession { Id = "ck-1", CheckoutUrl = "https://old" };

        // Act
        var modified = original with { CheckoutUrl = "https://new" };

        // Assert
        modified.CheckoutUrl.Should().Be("https://new");
        original.CheckoutUrl.Should().Be("https://old");
    }
}

public class CreateCheckoutRequestTests
{
    [Fact]
    public void CreateCheckoutRequest_WithRequiredProperties_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var request = new CreateCheckoutRequest
        {
            VariantId = "var-1"
        };

        // Assert
        request.VariantId.Should().Be("var-1");
        request.Email.Should().BeNull();
        request.Name.Should().BeNull();
        request.SuccessUrl.Should().BeNull();
        request.CancelUrl.Should().BeNull();
        request.Embed.Should().BeNull();
        request.DiscountCode.Should().BeNull();
        request.UserId.Should().BeNull();
        request.CustomData.Should().BeNull();
    }

    [Fact]
    public void CreateCheckoutRequest_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var customData = new Dictionary<string, object> { ["promo"] = "summer" };

        // Act
        var request = new CreateCheckoutRequest
        {
            VariantId = "var-1",
            Email = "buyer@example.com",
            Name = "Buyer",
            SuccessUrl = "https://app/success",
            CancelUrl = "https://app/cancel",
            Embed = true,
            DiscountCode = "SUMMER20",
            UserId = "user-99",
            CustomData = customData
        };

        // Assert
        request.VariantId.Should().Be("var-1");
        request.Email.Should().Be("buyer@example.com");
        request.Name.Should().Be("Buyer");
        request.SuccessUrl.Should().Be("https://app/success");
        request.CancelUrl.Should().Be("https://app/cancel");
        request.Embed.Should().BeTrue();
        request.DiscountCode.Should().Be("SUMMER20");
        request.UserId.Should().Be("user-99");
        request.CustomData.Should().BeSameAs(customData);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void CreateCheckoutRequest_EmbedFlag_AcceptsAllNullableBoolValues(bool? embed)
    {
        // Arrange & Act
        var request = new CreateCheckoutRequest
        {
            VariantId = "var-1",
            Embed = embed
        };

        // Assert
        request.Embed.Should().Be(embed);
    }

    [Fact]
    public void CreateCheckoutRequest_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = new CreateCheckoutRequest { VariantId = "v-1", Email = "e@x" };
        var second = new CreateCheckoutRequest { VariantId = "v-1", Email = "e@x" };

        // Act & Assert
        first.Should().Be(second);
    }

    [Fact]
    public void CreateCheckoutRequest_TwoInstancesDifferingByVariant_AreNotEqual()
    {
        // Arrange
        var first = new CreateCheckoutRequest { VariantId = "v-1" };
        var second = new CreateCheckoutRequest { VariantId = "v-2" };

        // Act & Assert
        first.Should().NotBe(second);
    }
}
