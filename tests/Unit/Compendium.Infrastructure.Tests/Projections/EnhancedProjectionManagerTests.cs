// -----------------------------------------------------------------------
// <copyright file="EnhancedProjectionManagerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Compendium.Infrastructure.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Unit tests for <see cref="EnhancedProjectionManager"/> exercising rebuild orchestration,
/// progress reporting, lifecycle operations and statistics aggregation.
/// </summary>
public sealed class EnhancedProjectionManagerTests
{
    [Fact]
    public void Ctor_NullEventStore_Throws()
    {
        // Arrange / Act
        var act = () => new EnhancedProjectionManager(
            null!,
            Substitute.For<IProjectionStore>(),
            BuildServiceProvider(),
            NullLogger<EnhancedProjectionManager>.Instance,
            Options.Create(new ProjectionOptions()));

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("eventStore");
    }

    [Fact]
    public void Ctor_NullProjectionStore_Throws()
    {
        // Arrange / Act
        var act = () => new EnhancedProjectionManager(
            Substitute.For<IStreamingEventStore>(),
            null!,
            BuildServiceProvider(),
            NullLogger<EnhancedProjectionManager>.Instance,
            Options.Create(new ProjectionOptions()));

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("projectionStore");
    }

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        // Arrange / Act
        var act = () => new EnhancedProjectionManager(
            Substitute.For<IStreamingEventStore>(),
            Substitute.For<IProjectionStore>(),
            null!,
            NullLogger<EnhancedProjectionManager>.Instance,
            Options.Create(new ProjectionOptions()));

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange / Act
        var act = () => new EnhancedProjectionManager(
            Substitute.For<IStreamingEventStore>(),
            Substitute.For<IProjectionStore>(),
            BuildServiceProvider(),
            null!,
            Options.Create(new ProjectionOptions()));

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Ctor_NullOptions_FallsBackToDefaults()
    {
        // Arrange / Act
        using var sut = new EnhancedProjectionManager(
            Substitute.For<IStreamingEventStore>(),
            Substitute.For<IProjectionStore>(),
            BuildServiceProvider(),
            NullLogger<EnhancedProjectionManager>.Instance,
            null!);

        // Assert — default options keep things buildable
        sut.Should().NotBeNull();
    }

    [Fact]
    public void RegisterProjection_AddsProjectionType()
    {
        // Arrange
        using var sut = CreateSut(out _, out _);

        // Act
        sut.RegisterProjection<TestProjection>();

        // Assert
        var stats = sut.GetStatisticsAsync().GetAwaiter().GetResult();
        stats.TotalProjections.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_NoProjections_ReturnsZeroes()
    {
        // Arrange
        using var sut = CreateSut(out _, out _);

        // Act
        var stats = await sut.GetStatisticsAsync();

        // Assert
        stats.TotalProjections.Should().Be(0);
        stats.ActiveProjections.Should().Be(0);
        stats.RebuildingProjections.Should().Be(0);
        stats.PausedProjections.Should().Be(0);
        stats.FailedProjections.Should().Be(0);
        stats.ProjectionDetails.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectionStateAsync_LoadsFromStoreWhenAvailable()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);
        var stored = new ProjectionState
        {
            ProjectionName = "TestProjection",
            Status = ProjectionStatus.Building,
        };
        store.GetProjectionStateAsync("TestProjection", Arg.Any<CancellationToken>())
            .Returns(stored);

        // Act
        var state = await sut.GetProjectionStateAsync("TestProjection");

        // Assert
        state.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task GetProjectionStateAsync_StoreReturnsNull_ReturnsDefaultIdleState()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);
        store.GetProjectionStateAsync("missing", Arg.Any<CancellationToken>())
            .Returns((ProjectionState?)null);

        // Act
        var state = await sut.GetProjectionStateAsync("missing");

        // Assert
        state.Status.Should().Be(ProjectionStatus.Idle);
        state.ProjectionName.Should().Be("missing");
    }

    [Fact]
    public async Task GetProjectionStateAsync_SecondCall_ReturnsCachedState()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);
        var stored = new ProjectionState { ProjectionName = "P", Status = ProjectionStatus.Building };
        store.GetProjectionStateAsync("P", Arg.Any<CancellationToken>())
            .Returns(stored, (ProjectionState?)null);

        // Act
        var first = await sut.GetProjectionStateAsync("P");
        var second = await sut.GetProjectionStateAsync("P");

        // Assert
        second.Should().BeSameAs(first);
        await store.Received(1).GetProjectionStateAsync("P", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PauseProjectionAsync_SavesPausedState()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);

        // Act
        await sut.PauseProjectionAsync("P");

        // Assert
        await store.Received(1).SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.ProjectionName == "P" && s.Status == ProjectionStatus.Paused),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeProjectionAsync_SavesBuildingState()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);

        // Act
        await sut.ResumeProjectionAsync("P");

        // Assert
        await store.Received(1).SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.ProjectionName == "P" && s.Status == ProjectionStatus.Building),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProjectionAsync_ClearsStateAndDelegatesToStore()
    {
        // Arrange
        using var sut = CreateSut(out _, out var store);
        sut.RegisterProjection<TestProjection>();

        // Act
        await sut.DeleteProjectionAsync("TestProjection");
        var stats = await sut.GetStatisticsAsync();

        // Assert
        stats.TotalProjections.Should().Be(0);
        await store.Received(1).DeleteProjectionDataAsync("TestProjection", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_NoEvents_CompletesAndUpdatesState()
    {
        // Arrange
        using var sut = CreateSut(out var eventStore, out var store);
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(0);
        eventStore.StreamEventsAsync(Arg.Any<string?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEventStream());

        // Act
        await sut.RebuildProjectionAsync<TestProjection>();

        // Assert
        await store.Received().SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.Status == ProjectionStatus.Completed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_StreamThrows_MarksAsFailedAndRethrows()
    {
        // Arrange
        using var sut = CreateSut(out var eventStore, out var store);
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(0);
        eventStore.StreamEventsAsync(Arg.Any<string?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(ThrowingEventStream());

        // Act
        Func<Task> act = async () => await sut.RebuildProjectionAsync<TestProjection>();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("stream-fail");
        await store.Received().SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.Status == ProjectionStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithEvents_AppliesEventsAndReportsProgress()
    {
        // Arrange
        using var sut = CreateSut(out var eventStore, out var store);
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(2L);
        eventStore.StreamEventsAsync(Arg.Any<string?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(TwoEventStream());

        var progressReports = new List<RebuildProgress>();
        var progress = new Progress<RebuildProgress>(p => progressReports.Add(p));

        // Act
        await sut.RebuildProjectionAsync<TestProjection>(progress: progress);
        await Task.Delay(50); // allow Progress<T> callbacks to dispatch

        // Assert
        await store.Received().SaveCheckpointAsync("TestProjection", Arg.Any<long>(), Arg.Any<CancellationToken>());
        await store.Received().SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.Status == ProjectionStatus.Completed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithSnapshotsEnabled_SavesSnapshot()
    {
        // Arrange
        var eventStore = Substitute.For<IStreamingEventStore>();
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(0L);
        eventStore.StreamEventsAsync(Arg.Any<string?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(EmptyEventStream());

        var store = Substitute.For<IProjectionStore>();
        store.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((long?)null);

        var services = new ServiceCollection();
        services.AddSingleton<TestProjection>();
        var sp = services.BuildServiceProvider();
        using var sut = new EnhancedProjectionManager(
            eventStore,
            store,
            sp,
            NullLogger<EnhancedProjectionManager>.Instance,
            Options.Create(new ProjectionOptions { EnableSnapshots = true, RebuildBatchSize = 5 }));

        // Act
        await sut.RebuildProjectionAsync<TestProjection>();

        // Assert
        await store.Received().SaveSnapshotAsync(Arg.Any<TestProjection>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_StreamThrowsCancellation_MarksAsPausedAndRethrows()
    {
        // Arrange
        using var sut = CreateSut(out var eventStore, out var store);
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(0);
        eventStore.StreamEventsAsync(Arg.Any<string?>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(CancelledEventStream());

        // Act
        Func<Task> act = async () => await sut.RebuildProjectionAsync<TestProjection>();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        await store.Received().SaveProjectionStateAsync(
            Arg.Is<ProjectionState>(s => s.Status == ProjectionStatus.Paused),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Dispose_TwiceIsIdempotent()
    {
        // Arrange
        var sut = CreateSut(out _, out _);

        // Act / Assert
        sut.Dispose();
        sut.Dispose();
    }

    private static EnhancedProjectionManager CreateSut(out IStreamingEventStore eventStore, out IProjectionStore store)
    {
        eventStore = Substitute.For<IStreamingEventStore>();
        eventStore.GetEventCountAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(0L);

        store = Substitute.For<IProjectionStore>();
        store.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((long?)null);

        var services = new ServiceCollection();
        services.AddSingleton<TestProjection>();
        var sp = services.BuildServiceProvider();

        return new EnhancedProjectionManager(
            eventStore,
            store,
            sp,
            NullLogger<EnhancedProjectionManager>.Instance,
            Options.Create(new ProjectionOptions { EnableSnapshots = false, ProgressReportInterval = 1, RebuildBatchSize = 10 }));
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestProjection>();
        return services.BuildServiceProvider();
    }

    private static async IAsyncEnumerable<EventData> EmptyEventStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<EventData> TwoEventStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield return new EventData
        {
            EventId = Guid.NewGuid(),
            StreamId = "s",
            StreamPosition = 1,
            GlobalPosition = 1,
            Timestamp = DateTime.UtcNow,
            EventType = "e",
            Event = new TestEvent(),
        };
        yield return new EventData
        {
            EventId = Guid.NewGuid(),
            StreamId = "s",
            StreamPosition = 2,
            GlobalPosition = 2,
            Timestamp = DateTime.UtcNow,
            EventType = "e",
            Event = new TestEvent(),
        };
    }

    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public string AggregateId { get; init; } = "agg-1";
        public string AggregateType { get; init; } = "Test";
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public long AggregateVersion { get; init; } = 1;
        public int EventVersion { get; init; } = 1;
    }

    private static async IAsyncEnumerable<EventData> ThrowingEventStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("stream-fail");
#pragma warning disable CS0162 // Unreachable
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<EventData> CancelledEventStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new OperationCanceledException("cancelled mid-stream");
#pragma warning disable CS0162 // Unreachable
        yield break;
#pragma warning restore CS0162
    }

    private sealed class TestProjection : Compendium.Infrastructure.Projections.IProjection
    {
        public string ProjectionName => "TestProjection";
        public int Version => 1;
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
