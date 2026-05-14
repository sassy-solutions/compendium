// -----------------------------------------------------------------------
// <copyright file="PushDeliveryResultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class PushDeliveryResultTests
{
    [Fact]
    public void PushDeliveryResult_AllSucceeded_HasEmptyFailedList()
    {
        // Arrange / Act
        var result = new PushDeliveryResult
        {
            Sent = 10,
            Failed = Array.Empty<PushFailure>(),
        };

        // Assert
        result.Sent.Should().Be(10);
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public void PushDeliveryResult_WithMixedOutcomes_PreservesValues()
    {
        // Arrange
        var failed = new[]
        {
            new PushFailure { Token = "bad-1", Reason = "InvalidRegistration" },
            new PushFailure { Token = "bad-2", Reason = "Unregistered" },
        };

        // Act
        var result = new PushDeliveryResult { Sent = 8, Failed = failed };

        // Assert
        result.Sent.Should().Be(8);
        result.Failed.Should().BeEquivalentTo(failed);
    }

    [Fact]
    public void PushDeliveryResult_RecordEquality_SameContent_NotEqualAcrossDistinctLists()
    {
        // Arrange — record equality on IReadOnlyList uses reference equality, not structural equality
        var failed = new[] { new PushFailure { Token = "t", Reason = "r" } };
        var first = new PushDeliveryResult { Sent = 1, Failed = failed };
        var second = new PushDeliveryResult { Sent = 1, Failed = failed };

        // Act / Assert
        first.Should().Be(second);
    }
}
