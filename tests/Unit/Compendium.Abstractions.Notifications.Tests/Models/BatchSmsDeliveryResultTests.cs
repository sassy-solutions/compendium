// -----------------------------------------------------------------------
// <copyright file="BatchSmsDeliveryResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class BatchSmsDeliveryResultTests
{
    [Fact]
    public void BatchSmsDeliveryResult_WithNoMessages_HasZeroCounts()
    {
        // Arrange / Act
        var result = new BatchSmsDeliveryResult
        {
            SuccessCount = 0,
            FailureCount = 0,
            Results = Array.Empty<SmsDeliveryResult>(),
        };

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public void BatchSmsDeliveryResult_WithMixedResults_PreservesValues()
    {
        // Arrange
        var results = new[]
        {
            new SmsDeliveryResult { ProviderMessageId = "SM1", Status = SmsStatus.Sent, SegmentCount = 1 },
            new SmsDeliveryResult { ProviderMessageId = "SM2", Status = SmsStatus.Failed, SegmentCount = 0 },
        };

        // Act
        var batch = new BatchSmsDeliveryResult
        {
            SuccessCount = 1,
            FailureCount = 1,
            Results = results,
        };

        // Assert
        batch.SuccessCount.Should().Be(1);
        batch.FailureCount.Should().Be(1);
        batch.Results.Should().HaveCount(2);
        batch.Results.Should().BeEquivalentTo(results);
    }

    [Fact]
    public void BatchSmsDeliveryResult_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new BatchSmsDeliveryResult
        {
            SuccessCount = 1,
            FailureCount = 0,
            Results = Array.Empty<SmsDeliveryResult>(),
        };

        // Act
        var updated = original with { FailureCount = 3 };

        // Assert
        updated.FailureCount.Should().Be(3);
        original.FailureCount.Should().Be(0);
    }

    [Fact]
    public void BatchSmsDeliveryResult_RecordEquality_TwoIdenticalBatches_AreEqual()
    {
        // Arrange
        var results = new[] { new SmsDeliveryResult { ProviderMessageId = "SM", Status = SmsStatus.Sent, SegmentCount = 1 } };
        var first = new BatchSmsDeliveryResult { SuccessCount = 1, FailureCount = 0, Results = results };
        var second = new BatchSmsDeliveryResult { SuccessCount = 1, FailureCount = 0, Results = results };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
