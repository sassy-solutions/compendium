// -----------------------------------------------------------------------
// <copyright file="BatchPushDeliveryResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class BatchPushDeliveryResultTests
{
    [Fact]
    public void BatchPushDeliveryResult_WithNoBatches_HasZeroCounts()
    {
        // Arrange / Act
        var result = new BatchPushDeliveryResult
        {
            SuccessCount = 0,
            FailureCount = 0,
            Results = Array.Empty<PushDeliveryResult>(),
        };

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public void BatchPushDeliveryResult_WithMultipleBatches_PreservesValues()
    {
        // Arrange
        var batches = new[]
        {
            new PushDeliveryResult { Sent = 3, Failed = Array.Empty<PushFailure>() },
            new PushDeliveryResult
            {
                Sent = 2,
                Failed = new[] { new PushFailure { Token = "t", Reason = "r" } },
            },
        };

        // Act
        var result = new BatchPushDeliveryResult
        {
            SuccessCount = 5,
            FailureCount = 1,
            Results = batches,
        };

        // Assert
        result.SuccessCount.Should().Be(5);
        result.FailureCount.Should().Be(1);
        result.Results.Should().HaveCount(2);
        result.Results.Should().BeEquivalentTo(batches);
    }

    [Fact]
    public void BatchPushDeliveryResult_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new BatchPushDeliveryResult
        {
            SuccessCount = 1,
            FailureCount = 0,
            Results = Array.Empty<PushDeliveryResult>(),
        };

        // Act
        var updated = original with { FailureCount = 2 };

        // Assert
        updated.FailureCount.Should().Be(2);
        original.FailureCount.Should().Be(0);
    }
}
