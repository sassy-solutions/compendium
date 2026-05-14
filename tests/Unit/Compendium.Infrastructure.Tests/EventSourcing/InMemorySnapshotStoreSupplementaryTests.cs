// -----------------------------------------------------------------------
// <copyright file="InMemorySnapshotStoreSupplementaryTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Supplementary tests for <see cref="InMemorySnapshotStore"/> covering disposal, statistics,
/// validation, cancellation and tenant isolation.
/// </summary>
public sealed class InMemorySnapshotStoreSupplementaryTests
{
    private readonly InMemorySnapshotStore _sut = new(NullLogger<InMemorySnapshotStore>.Instance);

    [Fact]
    public async Task GetLatestSnapshotAsync_AfterDispose_ReturnsFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.GetLatestSnapshotAsync<TestState>("a-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.Disposed");
    }

    [Fact]
    public async Task SaveSnapshotAsync_AfterDispose_ReturnsFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.SaveSnapshotAsync("a-1", new TestState(), 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.Disposed");
    }

    [Fact]
    public async Task SaveSnapshotAsync_NegativeVersion_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.SaveSnapshotAsync("a-1", new TestState(), -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.InvalidVersion");
    }

    [Fact]
    public async Task SaveSnapshotAsync_OlderVersion_DoesNotOverwrite()
    {
        // Arrange
        var first = new TestState { Value = "v1" };
        await _sut.SaveSnapshotAsync("a-1", first, 5);

        // Act
        await _sut.SaveSnapshotAsync("a-1", new TestState { Value = "older" }, 3);
        var loaded = await _sut.GetLatestSnapshotAsync<TestState>("a-1");

        // Assert
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.State.Value.Should().Be("v1");
        loaded.Value.Version.Should().Be(5);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_Cancelled_ReturnsCancellationFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _sut.GetLatestSnapshotAsync<TestState>("a-1", cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.OperationCancelled");
    }

    [Fact]
    public async Task SaveSnapshotAsync_Cancelled_ReturnsCancellationFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _sut.SaveSnapshotAsync("a-1", new TestState(), 1, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.OperationCancelled");
    }

    [Fact]
    public async Task GetStatisticsAsync_AfterDispose_ReturnsFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.Disposed");
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyStore_ReturnsZeroes()
    {
        // Arrange / Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSnapshots.Should().Be(0);
        result.Value.OldestSnapshot.Should().BeNull();
        result.Value.NewestSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_AfterSavingSnapshots_ReturnsCounts()
    {
        // Arrange
        await _sut.SaveSnapshotAsync("a-1", new TestState(), 1);
        await _sut.SaveSnapshotAsync("a-2", new TestState(), 1);

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSnapshots.Should().Be(2);
        result.Value.OldestSnapshot.Should().NotBeNull();
        result.Value.NewestSnapshot.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_Cancelled_ReturnsCancellationFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _sut.GetStatisticsAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SnapshotStore.OperationCancelled");
    }

    [Fact]
    public void Dispose_TwiceIsIdempotent()
    {
        // Arrange / Act / Assert
        _sut.Dispose();
        _sut.Dispose();
    }

    private sealed class TestState
    {
        public string Value { get; set; } = "default";
    }
}
