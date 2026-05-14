// -----------------------------------------------------------------------
// <copyright file="ISnapshotStoreContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;

namespace Compendium.Abstractions.Tests.EventSourcing;

public class ISnapshotStoreContractTests
{
    private sealed class FakeState
    {
        public string? Name { get; init; }
    }

    [Fact]
    public async Task ISnapshotStore_Substitute_GetLatestSnapshotAsync_ReturnsConfiguredSnapshot()
    {
        // Arrange
        var store = Substitute.For<ISnapshotStore>();
        var state = new FakeState { Name = "snapshot" };
        var snapshot = new Snapshot<FakeState>(state, 10L, DateTimeOffset.UtcNow, "tenant-1");
        store.GetLatestSnapshotAsync<FakeState>("agg-1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(snapshot));

        // Act
        var result = await store.GetLatestSnapshotAsync<FakeState>("agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.State.Should().BeSameAs(state);
        result.Value.Version.Should().Be(10L);
    }

    [Fact]
    public async Task ISnapshotStore_Substitute_GetLatestSnapshotAsync_PropagatesNotFound()
    {
        // Arrange
        var store = Substitute.For<ISnapshotStore>();
        var error = Error.NotFound("snapshot.not_found", "no snapshot");
        store.GetLatestSnapshotAsync<FakeState>("agg-x", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Snapshot<FakeState>>(error));

        // Act
        var result = await store.GetLatestSnapshotAsync<FakeState>("agg-x", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("snapshot.not_found");
    }

    [Fact]
    public async Task ISnapshotStore_Substitute_SaveSnapshotAsync_ReturnsSuccess()
    {
        // Arrange
        var store = Substitute.For<ISnapshotStore>();
        var state = new FakeState { Name = "to-save" };
        store.SaveSnapshotAsync("agg-1", state, 5L, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await store.SaveSnapshotAsync("agg-1", state, 5L, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await store.Received(1).SaveSnapshotAsync("agg-1", state, 5L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ISnapshotStrategy_Substitute_ShouldTakeSnapshot_ReturnsConfiguredValue()
    {
        // Arrange
        var strategy = Substitute.For<ISnapshotStrategy>();
        strategy.ShouldTakeSnapshot("agg-1", 100L, 100).Returns(true);
        strategy.ShouldTakeSnapshot("agg-1", 5L, 5).Returns(false);

        // Act
        var atThreshold = strategy.ShouldTakeSnapshot("agg-1", 100L, 100);
        var below = strategy.ShouldTakeSnapshot("agg-1", 5L, 5);

        // Assert
        atThreshold.Should().BeTrue();
        below.Should().BeFalse();
    }
}
