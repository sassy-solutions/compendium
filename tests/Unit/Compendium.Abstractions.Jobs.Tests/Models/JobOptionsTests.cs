// -----------------------------------------------------------------------
// <copyright file="JobOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Jobs.Tests.Models;

public class JobOptionsTests
{
    [Fact]
    public void JobOptions_WithTenantOnly_LeavesOptionalsAtDefaults()
    {
        // Arrange / Act
        var opts = new JobOptions(TenantId: "tenant-1");

        // Assert
        opts.TenantId.Should().Be("tenant-1");
        opts.Retry.Should().BeNull();
        opts.Queue.Should().BeNull();
        opts.Priority.Should().Be(JobPriority.Normal);
        opts.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public void JobOptions_WithAllProperties_PreservesValues()
    {
        // Arrange
        var retry = new RetryPolicy(MaxAttempts: 5, InitialBackoff: TimeSpan.FromSeconds(2), MaxBackoff: TimeSpan.FromMinutes(1));
        var scheduledAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var opts = new JobOptions(
            TenantId: "tenant-2",
            Retry: retry,
            Queue: "critical",
            Priority: JobPriority.Critical,
            ScheduledAt: scheduledAt);

        // Assert
        opts.TenantId.Should().Be("tenant-2");
        opts.Retry.Should().BeSameAs(retry);
        opts.Queue.Should().Be("critical");
        opts.Priority.Should().Be(JobPriority.Critical);
        opts.ScheduledAt.Should().Be(scheduledAt);
    }

    [Theory]
    [InlineData(JobPriority.Low)]
    [InlineData(JobPriority.Normal)]
    [InlineData(JobPriority.High)]
    [InlineData(JobPriority.Critical)]
    public void JobOptions_WithEachPriority_PreservesPriority(JobPriority priority)
    {
        // Arrange / Act
        var opts = new JobOptions(TenantId: "t", Priority: priority);

        // Assert
        opts.Priority.Should().Be(priority);
    }

    [Fact]
    public void JobOptions_RecordEquality_TwoIdenticalOptions_AreEqual()
    {
        // Arrange
        var first = new JobOptions("t", new RetryPolicy(3, TimeSpan.FromSeconds(1)), "q", JobPriority.High);
        var second = new JobOptions("t", new RetryPolicy(3, TimeSpan.FromSeconds(1)), "q", JobPriority.High);

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void JobOptions_RecordEquality_DifferingTenant_AreNotEqual()
    {
        // Arrange
        var first = new JobOptions("a");
        var second = new JobOptions("b");

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void JobOptions_With_ProducesModifiedCopy()
    {
        // Arrange
        var original = new JobOptions("t");

        // Act
        var updated = original with { Queue = "high", Priority = JobPriority.High };

        // Assert
        updated.Queue.Should().Be("high");
        updated.Priority.Should().Be(JobPriority.High);
        original.Queue.Should().BeNull();
        original.Priority.Should().Be(JobPriority.Normal);
    }

    [Fact]
    public void JobPriority_EnumValues_AreOrderedFromLowToCritical()
    {
        // Act / Assert
        ((int)JobPriority.Low).Should().BeLessThan((int)JobPriority.Normal);
        ((int)JobPriority.Normal).Should().BeLessThan((int)JobPriority.High);
        ((int)JobPriority.High).Should().BeLessThan((int)JobPriority.Critical);
    }

    [Fact]
    public void RetryPolicy_WithRequiredOnly_LeavesMaxBackoffNull()
    {
        // Arrange / Act
        var policy = new RetryPolicy(MaxAttempts: 3, InitialBackoff: TimeSpan.FromSeconds(1));

        // Assert
        policy.MaxAttempts.Should().Be(3);
        policy.InitialBackoff.Should().Be(TimeSpan.FromSeconds(1));
        policy.MaxBackoff.Should().BeNull();
    }

    [Fact]
    public void RetryPolicy_WithMaxBackoff_PreservesValue()
    {
        // Arrange / Act
        var policy = new RetryPolicy(MaxAttempts: 10, InitialBackoff: TimeSpan.FromMilliseconds(500), MaxBackoff: TimeSpan.FromMinutes(5));

        // Assert
        policy.MaxAttempts.Should().Be(10);
        policy.InitialBackoff.Should().Be(TimeSpan.FromMilliseconds(500));
        policy.MaxBackoff.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void RetryPolicy_RecordEquality_TwoIdenticalPolicies_AreEqual()
    {
        // Arrange
        var first = new RetryPolicy(3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1));
        var second = new RetryPolicy(3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1));

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
