// -----------------------------------------------------------------------
// <copyright file="PushFailureTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Tests.Models;

public class PushFailureTests
{
    [Fact]
    public void PushFailure_WithRequired_PreservesValues()
    {
        // Arrange / Act
        var failure = new PushFailure { Token = "tok-1", Reason = "InvalidRegistration" };

        // Assert
        failure.Token.Should().Be("tok-1");
        failure.Reason.Should().Be("InvalidRegistration");
    }

    [Fact]
    public void PushFailure_RecordEquality_TwoIdenticalFailures_AreEqual()
    {
        // Arrange
        var first = new PushFailure { Token = "t", Reason = "r" };
        var second = new PushFailure { Token = "t", Reason = "r" };

        // Act / Assert
        first.Should().Be(second);
    }

    [Fact]
    public void PushFailure_RecordEquality_DifferingReason_AreNotEqual()
    {
        // Arrange
        var first = new PushFailure { Token = "t", Reason = "InvalidToken" };
        var second = new PushFailure { Token = "t", Reason = "RateLimited" };

        // Act / Assert
        first.Should().NotBe(second);
    }
}
