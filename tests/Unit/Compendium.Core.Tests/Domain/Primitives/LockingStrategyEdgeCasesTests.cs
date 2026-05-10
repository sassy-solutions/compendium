// -----------------------------------------------------------------------
// <copyright file="LockingStrategyEdgeCasesTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Tests.Domain.Primitives;

/// <summary>
/// Coverage for guard-clauses, async overloads, and disposed-state behaviour of every
/// <see cref="ILockingStrategy"/> implementation. Complements the broader
/// <see cref="LockingStrategyTests"/> by exercising the lesser-used paths.
/// </summary>
public class LockingStrategyEdgeCasesTests
{
    #region NoLockStrategy

    [Fact]
    public void NoLockStrategy_ExecuteRead_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new NoLockStrategy();

        // Act
        var act = () => strategy.ExecuteRead<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NoLockStrategy_ExecuteWrite_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new NoLockStrategy();

        // Act
        var act = () => strategy.ExecuteWrite(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task NoLockStrategy_ExecuteReadAsync_RunsOperationAndReturnsValue()
    {
        // Arrange
        using var strategy = new NoLockStrategy();

        // Act
        var result = await strategy.ExecuteReadAsync(() => Task.FromResult(42));

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task NoLockStrategy_ExecuteReadAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new NoLockStrategy();

        // Act
        var act = async () => await strategy.ExecuteReadAsync<int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task NoLockStrategy_ExecuteWriteAsync_RunsOperation()
    {
        // Arrange
        using var strategy = new NoLockStrategy();
        var sentinel = false;

        // Act
        await strategy.ExecuteWriteAsync(() =>
        {
            sentinel = true;
            return Task.CompletedTask;
        });

        // Assert
        sentinel.Should().BeTrue();
    }

    [Fact]
    public async Task NoLockStrategy_ExecuteWriteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new NoLockStrategy();

        // Act
        var act = async () => await strategy.ExecuteWriteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void NoLockStrategy_AfterDispose_ExecuteRead_Throws()
    {
        // Arrange
        var strategy = new NoLockStrategy();
        strategy.Dispose();

        // Act
        var act = () => strategy.ExecuteRead(() => 1);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void NoLockStrategy_AfterDispose_ExecuteWrite_Throws()
    {
        // Arrange
        var strategy = new NoLockStrategy();
        strategy.Dispose();

        // Act
        var act = () => strategy.ExecuteWrite(() => { });

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task NoLockStrategy_AfterDispose_ExecuteReadAsync_Throws()
    {
        // Arrange
        var strategy = new NoLockStrategy();
        strategy.Dispose();

        // Act
        var act = async () => await strategy.ExecuteReadAsync(() => Task.FromResult(1));

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task NoLockStrategy_AfterDispose_ExecuteWriteAsync_Throws()
    {
        // Arrange
        var strategy = new NoLockStrategy();
        strategy.Dispose();

        // Act
        var act = async () => await strategy.ExecuteWriteAsync(() => Task.CompletedTask);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void NoLockStrategy_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        var strategy = new NoLockStrategy();

        // Act
        strategy.Dispose();
        var act = () => strategy.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ReaderWriterLockStrategy

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteRead_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var act = () => strategy.ExecuteRead<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteWrite_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var act = () => strategy.ExecuteWrite(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_ExecuteReadAsync_RunsOperationAndReleasesLock()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var result1 = await strategy.ExecuteReadAsync(() => Task.FromResult("a"));
        var result2 = await strategy.ExecuteReadAsync(() => Task.FromResult("b"));

        // Assert
        result1.Should().Be("a");
        result2.Should().Be("b");
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_ExecuteReadAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var act = async () => await strategy.ExecuteReadAsync<int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_ExecuteWriteAsync_RunsOperation()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();
        var sentinel = 0;

        // Act
        await strategy.ExecuteWriteAsync(() =>
        {
            Interlocked.Increment(ref sentinel);
            return Task.CompletedTask;
        });

        // Assert
        sentinel.Should().Be(1);
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_ExecuteWriteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var act = async () => await strategy.ExecuteWriteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteRead_WhenOperationThrows_ReleasesReadLock()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var firstAttempt = () => strategy.ExecuteRead<int>(() => throw new InvalidOperationException("boom"));

        // Assert
        firstAttempt.Should().Throw<InvalidOperationException>().WithMessage("boom");

        // After the throw, the lock must have been released — a follow-up read succeeds.
        var followUp = strategy.ExecuteRead(() => 1);
        followUp.Should().Be(1);
    }

    [Fact]
    public void ReaderWriterLockStrategy_ExecuteWrite_WhenOperationThrows_ReleasesWriteLock()
    {
        // Arrange
        using var strategy = new ReaderWriterLockStrategy();

        // Act
        var firstAttempt = () => strategy.ExecuteWrite(() => throw new InvalidOperationException("boom"));

        // Assert
        firstAttempt.Should().Throw<InvalidOperationException>().WithMessage("boom");

        // Subsequent write succeeds, proving the previous lock was released.
        var followUp = () => strategy.ExecuteWrite(() => { });
        followUp.Should().NotThrow();
    }

    [Fact]
    public void ReaderWriterLockStrategy_AfterDispose_ExecuteRead_Throws()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();
        strategy.Dispose();

        // Act
        var act = () => strategy.ExecuteRead(() => 1);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ReaderWriterLockStrategy_AfterDispose_ExecuteWrite_Throws()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();
        strategy.Dispose();

        // Act
        var act = () => strategy.ExecuteWrite(() => { });

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_AfterDispose_ExecuteReadAsync_Throws()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();
        strategy.Dispose();

        // Act
        var act = async () => await strategy.ExecuteReadAsync(() => Task.FromResult(1));

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReaderWriterLockStrategy_AfterDispose_ExecuteWriteAsync_Throws()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();
        strategy.Dispose();

        // Act
        var act = async () => await strategy.ExecuteWriteAsync(() => Task.CompletedTask);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void ReaderWriterLockStrategy_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        var strategy = new ReaderWriterLockStrategy();

        // Act
        strategy.Dispose();
        var act = () => strategy.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
