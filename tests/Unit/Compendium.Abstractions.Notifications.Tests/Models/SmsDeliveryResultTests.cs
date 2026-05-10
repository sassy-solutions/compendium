// -----------------------------------------------------------------------
// <copyright file="SmsDeliveryResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class SmsDeliveryResultTests
{
    [Fact]
    public void SmsDeliveryResult_WithRequiredOnly_DefaultsCostHintToNull()
    {
        // Arrange / Act
        var result = new SmsDeliveryResult
        {
            ProviderMessageId = "SM123",
            Status = SmsStatus.Queued,
            SegmentCount = 1,
        };

        // Assert
        result.ProviderMessageId.Should().Be("SM123");
        result.Status.Should().Be(SmsStatus.Queued);
        result.SegmentCount.Should().Be(1);
        result.CostHint.Should().BeNull();
    }

    [Theory]
    [InlineData(SmsStatus.Queued, 1, "0.0075 USD")]
    [InlineData(SmsStatus.Sent, 2, "0.015 USD")]
    [InlineData(SmsStatus.Delivered, 3, null)]
    [InlineData(SmsStatus.Failed, 0, null)]
    [InlineData(SmsStatus.Undelivered, 1, "0.0075 USD")]
    public void SmsDeliveryResult_WithVariousStatuses_PreservesValues(SmsStatus status, int segments, string? costHint)
    {
        // Act
        var result = new SmsDeliveryResult
        {
            ProviderMessageId = "SM-abc",
            Status = status,
            SegmentCount = segments,
            CostHint = costHint,
        };

        // Assert
        result.Status.Should().Be(status);
        result.SegmentCount.Should().Be(segments);
        result.CostHint.Should().Be(costHint);
    }

    [Fact]
    public void SmsDeliveryResult_RecordEquality_TwoIdenticalResults_AreEqual()
    {
        // Arrange
        var first = new SmsDeliveryResult
        {
            ProviderMessageId = "SM1",
            Status = SmsStatus.Sent,
            SegmentCount = 2,
            CostHint = "0.015 USD",
        };
        var second = new SmsDeliveryResult
        {
            ProviderMessageId = "SM1",
            Status = SmsStatus.Sent,
            SegmentCount = 2,
            CostHint = "0.015 USD",
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void SmsDeliveryResult_RecordEquality_DifferingStatus_AreNotEqual()
    {
        // Arrange
        var first = new SmsDeliveryResult { ProviderMessageId = "SM1", Status = SmsStatus.Sent, SegmentCount = 1 };
        var second = new SmsDeliveryResult { ProviderMessageId = "SM1", Status = SmsStatus.Failed, SegmentCount = 1 };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void SmsDeliveryResult_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new SmsDeliveryResult
        {
            ProviderMessageId = "SM1",
            Status = SmsStatus.Queued,
            SegmentCount = 1,
        };

        // Act
        var updated = original with { Status = SmsStatus.Delivered };

        // Assert
        updated.Status.Should().Be(SmsStatus.Delivered);
        original.Status.Should().Be(SmsStatus.Queued);
        updated.ProviderMessageId.Should().Be(original.ProviderMessageId);
    }
}
