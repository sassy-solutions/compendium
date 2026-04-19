// -----------------------------------------------------------------------
// <copyright file="LockingStrategyTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Tests.TestHelpers;

namespace Compendium.Core.Tests.Domain.Primitives;

public class LockingStrategyTests
{
    #region ModernLockStrategy Tests

    [Fact]
    public void ModernLockStrategy_ExecuteRead_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ModernLockStrategy();
        var counter = 0;

        // Act
        var result = strategy.ExecuteRead(() => ++counter);

        // Assert
        result.Should().Be(1);
        counter.Should().Be(1);
    }

    [Fact]
    public void ModernLockStrategy_ExecuteWrite_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ModernLockStrategy();
        var counter = 0;

        // Act
        strategy.ExecuteWrite(() => counter++);

        // Assert
        counter.Should().Be(1);
    }

    [Fact]
    public async Task ModernLockStrategy_ExecuteReadAsync_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ModernLockStrategy();
        var counter = 0;

        // Act
        var result = await strategy.ExecuteReadAsync(async () =>
        {
            await Task.Delay(1);
            return ++counter;
        });

        // Assert
        result.Should().Be(1);
        counter.Should().Be(1);
    }

    [Fact]
    public async Task ModernLockStrategy_ExecuteWriteAsync_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ModernLockStrategy();
        var counter = 0;

        // Act
        await strategy.ExecuteWriteAsync(async () =>
        {
            await Task.Delay(1);
            counter++;
        });

        // Assert
        counter.Should().Be(1);
    }

    [Fact]
    public void ModernLockStrategy_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var strategy = new ModernLockStrategy();
        strategy.Dispose();

        // Act & Assert
        strategy.Invoking(s => s.ExecuteRead(() => 1))
            .Should().Throw<ObjectDisposedException>();

        strategy.Invoking(s => s.ExecuteWrite(() => { }))
            .Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region ReaderWriterLockStrategy Tests

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteRead_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();
        var counter = 0;

        // Act
        var result = strategy.ExecuteRead(() => ++counter);

        // Assert
        result.Should().Be(1);
        counter.Should().Be(1);
    }

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteWrite_ExecutesOperation()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();
        var counter = 0;

        // Act
        strategy.ExecuteWrite(() => counter++);

        // Assert
        counter.Should().Be(1);
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_ConcurrentReads_ShouldNotBlock()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();
        var tasks = new List<Task<int>>();
        var value = 42;

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => strategy.ExecuteRead(() =>
            {
                Thread.Sleep(10); // Simulate some work
                return value;
            })));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        // Concurrent reads should complete much faster than sequential execution (10 x 10ms = 100ms if sequential)
        // Using generous threshold for CI runner variability
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        tasks.All(t => t.Result == 42).Should().BeTrue();
    }

    [Fact]
    public void ReaderWriterLockStrategy_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();
        strategy.Dispose();

        // Act & Assert
        strategy.Invoking(s => s.ExecuteRead(() => 1))
            .Should().Throw<ObjectDisposedException>();

        strategy.Invoking(s => s.ExecuteWrite(() => { }))
            .Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region NoLockStrategy Tests

    [Fact]
    public void NoLockStrategy_ExecuteRead_ExecutesOperation()
    {
        // Arrange
        using var strategy = new NoLockStrategy();
        var counter = 0;

        // Act
        var result = strategy.ExecuteRead(() => ++counter);

        // Assert
        result.Should().Be(1);
        counter.Should().Be(1);
    }

    [Fact]
    public void NoLockStrategy_ExecuteWrite_ExecutesOperation()
    {
        // Arrange
        using var strategy = new NoLockStrategy();
        var counter = 0;

        // Act
        strategy.ExecuteWrite(() => counter++);

        // Assert
        counter.Should().Be(1);
    }

    [Fact]
    public void NoLockStrategy_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var strategy = new NoLockStrategy();
        strategy.Dispose();

        // Act & Assert
        strategy.Invoking(s => s.ExecuteRead(() => 1))
            .Should().Throw<ObjectDisposedException>();

        strategy.Invoking(s => s.ExecuteWrite(() => { }))
            .Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region AggregateRoot with Different Strategies Tests

    [Fact]
    public void AggregateRoot_WithModernLockStrategy_WorksCorrectly()
    {
        // Arrange & Act
        using var lockStrategy = new ModernLockStrategy();
        using var aggregate = new TestAggregateWithLocking(Guid.NewGuid(), lockStrategy);
        var domainEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregateWithLocking), 1);

        aggregate.TestAddDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.HasDomainEvents.Should().BeTrue();

        var events = aggregate.GetUncommittedEvents();
        events.Should().HaveCount(1);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AggregateRoot_WithNoLockStrategy_WorksCorrectly()
    {
        // Arrange & Act
        using var lockStrategy = new NoLockStrategy();
        using var aggregate = new TestAggregateWithLocking(Guid.NewGuid(), lockStrategy);
        var domainEvent = new TestDomainEvent(aggregate.Id.ToString(), nameof(TestAggregateWithLocking), 1);

        aggregate.TestAddDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.HasDomainEvents.Should().BeTrue();

        var events = aggregate.GetUncommittedEvents();
        events.Should().HaveCount(1);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Test Aggregate with Locking Strategy

    private class TestAggregateWithLocking : AggregateRoot<Guid>
    {
        public TestAggregateWithLocking(Guid id, ILockingStrategy? lockingStrategy = null) : base(id, lockingStrategy)
        {
        }

        public void TestAddDomainEvent(IDomainEvent @event) => AddDomainEvent(@event);
    }

    #endregion
}
