// -----------------------------------------------------------------------
// <copyright file="LiveProjectionProcessorBackfillTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Projections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Verifies the cold-start position picked by <see cref="LiveProjectionProcessor"/>
/// based on <see cref="ProjectionOptions.BackfillFromBeginningOnEmptyCheckpoint"/>.
/// </summary>
public sealed class LiveProjectionProcessorBackfillTests
{
    [Fact]
    public async Task EmptyCheckpoints_DefaultOptions_JumpsToHead()
    {
        var (eventStore, projectionStore) = SetupStores(headPosition: 250L, checkpoint: null);

        var processor = CreateProcessor(eventStore, projectionStore, backfill: false);
        processor.RegisterProjection<TestProjection>();

        await processor.InitializeProjectionsAsync(CancellationToken.None);

        processor.GetStatus().LastProcessedPosition.Should().Be(250L,
            "default behaviour avoids replaying weeks of history on cold restart");
        await eventStore.Received().GetMaxGlobalPositionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmptyCheckpoints_BackfillFlagOn_StaysAtZero()
    {
        var (eventStore, projectionStore) = SetupStores(headPosition: 250L, checkpoint: null);

        var processor = CreateProcessor(eventStore, projectionStore, backfill: true);
        processor.RegisterProjection<TestProjection>();

        await processor.InitializeProjectionsAsync(CancellationToken.None);

        processor.GetStatus().LastProcessedPosition.Should().Be(0L,
            "BackfillFromBeginningOnEmptyCheckpoint=true keeps the cursor at 0 so the polling loop reads from position > 0");
        await eventStore.DidNotReceive().GetMaxGlobalPositionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingCheckpoint_TakesPrecedence_RegardlessOfBackfillFlag()
    {
        var (eventStore, projectionStore) = SetupStores(headPosition: 999L, checkpoint: 100L);

        // Even with backfill=true, a real checkpoint always wins.
        var processor = CreateProcessor(eventStore, projectionStore, backfill: true);
        processor.RegisterProjection<TestProjection>();

        await processor.InitializeProjectionsAsync(CancellationToken.None);

        processor.GetStatus().LastProcessedPosition.Should().Be(100L);
        await eventStore.DidNotReceive().GetMaxGlobalPositionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingCheckpoint_DefaultOptions_DoesNotJumpToHead()
    {
        var (eventStore, projectionStore) = SetupStores(headPosition: 999L, checkpoint: 100L);

        var processor = CreateProcessor(eventStore, projectionStore, backfill: false);
        processor.RegisterProjection<TestProjection>();

        await processor.InitializeProjectionsAsync(CancellationToken.None);

        processor.GetStatus().LastProcessedPosition.Should().Be(100L);
        await eventStore.DidNotReceive().GetMaxGlobalPositionAsync(Arg.Any<CancellationToken>());
    }

    private static (IStreamingEventStore eventStore, IProjectionStore projectionStore) SetupStores(
        long headPosition,
        long? checkpoint)
    {
        var eventStore = Substitute.For<IStreamingEventStore>();
        eventStore.GetMaxGlobalPositionAsync(Arg.Any<CancellationToken>())
            .Returns(headPosition);

        var projectionStore = Substitute.For<IProjectionStore>();
        projectionStore.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(checkpoint);
        // Snapshot path is intentionally not exercised — EnableSnapshots=false in CreateProcessor.

        return (eventStore, projectionStore);
    }

    private static LiveProjectionProcessor CreateProcessor(
        IStreamingEventStore eventStore,
        IProjectionStore projectionStore,
        bool backfill)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestProjection>();
        var sp = services.BuildServiceProvider();

        var options = Options.Create(new ProjectionOptions
        {
            BackfillFromBeginningOnEmptyCheckpoint = backfill,
            EnableSnapshots = false,
        });

        return new LiveProjectionProcessor(
            eventStore,
            projectionStore,
            sp,
            NullLogger<LiveProjectionProcessor>.Instance,
            options);
    }

    /// <summary>Minimal projection used purely so the processor has at least one registered.</summary>
    private sealed class TestProjection : Compendium.Infrastructure.Projections.IProjection
    {
        public string ProjectionName => "Test";
        public int Version => 1;
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
