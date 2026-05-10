// -----------------------------------------------------------------------
// <copyright file="WebhookProcessingResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Tests.Models;

public class WebhookProcessingResultTests
{
    [Fact]
    public void WebhookProcessingResult_WithRequiredProperty_CreatesInstanceSuccessfully()
    {
        // Arrange & Act
        var result = new WebhookProcessingResult { Processed = true };

        // Assert
        result.Processed.Should().BeTrue();
        result.EventType.Should().BeNull();
        result.EventId.Should().BeNull();
        result.ResourceType.Should().BeNull();
        result.ResourceId.Should().BeNull();
        result.TenantId.Should().BeNull();
        result.WasDuplicate.Should().BeFalse();
        result.ExtractedData.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void WebhookProcessingResult_WithAllProperties_PreservesAllValues()
    {
        // Arrange
        var extracted = new Dictionary<string, object> { ["amount"] = 1000 };

        // Act
        var result = new WebhookProcessingResult
        {
            Processed = true,
            EventType = "subscription.created",
            EventId = "evt-1",
            ResourceType = "subscription",
            ResourceId = "sub-1",
            TenantId = "tenant-1",
            WasDuplicate = false,
            ExtractedData = extracted,
            ErrorMessage = null
        };

        // Assert
        result.EventType.Should().Be("subscription.created");
        result.EventId.Should().Be("evt-1");
        result.ResourceType.Should().Be("subscription");
        result.ResourceId.Should().Be("sub-1");
        result.TenantId.Should().Be("tenant-1");
        result.ExtractedData.Should().BeSameAs(extracted);
    }

    [Fact]
    public void Success_WithEventTypeOnly_ReturnsProcessedResultWithEventType()
    {
        // Act
        var result = WebhookProcessingResult.Success("order.created");

        // Assert
        result.Processed.Should().BeTrue();
        result.WasDuplicate.Should().BeFalse();
        result.EventType.Should().Be("order.created");
        result.EventId.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Success_WithEventTypeAndId_ReturnsProcessedResultWithBoth()
    {
        // Act
        var result = WebhookProcessingResult.Success("order.created", "evt-42");

        // Assert
        result.Processed.Should().BeTrue();
        result.WasDuplicate.Should().BeFalse();
        result.EventType.Should().Be("order.created");
        result.EventId.Should().Be("evt-42");
    }

    [Fact]
    public void Duplicate_WithEventTypeOnly_ReturnsProcessedDuplicateResult()
    {
        // Act
        var result = WebhookProcessingResult.Duplicate("order.created");

        // Assert
        result.Processed.Should().BeTrue();
        result.WasDuplicate.Should().BeTrue();
        result.EventType.Should().Be("order.created");
        result.EventId.Should().BeNull();
    }

    [Fact]
    public void Duplicate_WithEventTypeAndId_ReturnsProcessedDuplicateResultWithBoth()
    {
        // Act
        var result = WebhookProcessingResult.Duplicate("order.created", "evt-99");

        // Assert
        result.Processed.Should().BeTrue();
        result.WasDuplicate.Should().BeTrue();
        result.EventType.Should().Be("order.created");
        result.EventId.Should().Be("evt-99");
    }

    [Fact]
    public void Failure_WithErrorMessage_ReturnsUnprocessedResultWithError()
    {
        // Act
        var result = WebhookProcessingResult.Failure("Bad signature");

        // Assert
        result.Processed.Should().BeFalse();
        result.WasDuplicate.Should().BeFalse();
        result.ErrorMessage.Should().Be("Bad signature");
        result.EventType.Should().BeNull();
        result.EventId.Should().BeNull();
    }

    [Fact]
    public void WebhookProcessingResult_TwoIdenticalInstances_AreEqualByValue()
    {
        // Arrange
        var first = WebhookProcessingResult.Success("e", "id");
        var second = WebhookProcessingResult.Success("e", "id");

        // Act & Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void WebhookProcessingResult_SuccessVsDuplicate_AreNotEqual()
    {
        // Arrange
        var success = WebhookProcessingResult.Success("e");
        var duplicate = WebhookProcessingResult.Duplicate("e");

        // Act & Assert
        success.Should().NotBe(duplicate);
    }

    [Fact]
    public void WebhookProcessingResult_WithExpression_ProducesNewInstanceWithUpdatedValue()
    {
        // Arrange
        var original = WebhookProcessingResult.Success("e1");

        // Act
        var modified = original with { EventType = "e2" };

        // Assert
        modified.EventType.Should().Be("e2");
        original.EventType.Should().Be("e1");
    }

    [Theory]
    [InlineData("subscription.created")]
    [InlineData("invoice.paid")]
    [InlineData("order.refunded")]
    public void Success_AcceptsArbitraryEventTypes(string eventType)
    {
        // Act
        var result = WebhookProcessingResult.Success(eventType);

        // Assert
        result.Processed.Should().BeTrue();
        result.EventType.Should().Be(eventType);
    }
}
