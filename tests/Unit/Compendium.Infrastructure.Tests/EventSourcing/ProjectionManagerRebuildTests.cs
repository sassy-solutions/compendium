// -----------------------------------------------------------------------
// <copyright file="ProjectionManagerRebuildTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.EventSourcing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.EventSourcing;

/// <summary>
/// Additional unit tests for <see cref="ProjectionManager"/> covering rebuild semantics,
/// state retrieval, statistics, and the checkpoint store integration that the
/// existing suite does not exercise.
/// </summary>
public sealed class ProjectionManagerRebuildTests
{
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly ProjectionManager _sut;

    public ProjectionManagerRebuildTests()
    {
        // Arrange (shared)
        _sut = new ProjectionManager(_eventStore, NullLogger<ProjectionManager>.Instance);
    }

    [Fact]
    public async Task RebuildProjectionAsync_EmptyProjectionId_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.RebuildProjectionAsync("", "agg-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionManager.InvalidProjectionId");
    }

    [Fact]
    public async Task RebuildProjectionAsync_EmptyAggregateId_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.RebuildProjectionAsync("p", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionManager.InvalidAggregateId");
    }

    [Fact]
    public async Task RebuildProjectionAsync_UnknownProjection_ReturnsNotFound()
    {
        // Arrange / Act
        var result = await _sut.RebuildProjectionAsync("missing", "agg-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Projection.NotFound");
    }

    [Fact]
    public async Task RebuildProjectionAsync_EventStoreFailure_ReturnsFailure()
    {
        // Arrange
        var projection = new TestProjection();
        _sut.RegisterProjection(projection);
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<IDomainEvent>>(Error.Failure("es.fail", "boom")));

        // Act
        var result = await _sut.RebuildProjectionAsync(projection.ProjectionId, "agg-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("es.fail");
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var projection = new TestProjection();
        _sut.RegisterProjection(projection);
        IReadOnlyList<IDomainEvent> events = new[] { new TestEvent(), new TestEvent() };
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        var reports = new List<ProjectionRebuildProgress>();
        var progress = new Progress<ProjectionRebuildProgress>(p => reports.Add(p));

        // Act
        var result = await _sut.RebuildProjectionAsync(projection.ProjectionId, "agg-1", progress);
        await Task.Delay(50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        reports.Should().NotBeEmpty();
        reports.Last().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task RebuildProjectionAsync_WithCheckpointStore_SavesCheckpoint()
    {
        // Arrange
        var checkpointStore = Substitute.For<IProjectionCheckpointStore>();
        checkpointStore.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(0L));
        var sut = new ProjectionManager(_eventStore, NullLogger<ProjectionManager>.Instance, checkpointStore, checkpointInterval: 1);
        var projection = new TestProjection();
        sut.RegisterProjection(projection);

        IReadOnlyList<IDomainEvent> events = new[] { new TestEvent(), new TestEvent() };
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        // Act
        var result = await sut.RebuildProjectionAsync(projection.ProjectionId, "agg-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await checkpointStore.Received().SaveCheckpointAsync(
            projection.ProjectionId, "agg-1", Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RebuildProjectionAsync_CheckpointStoreReturnsNonZero_DoesNotResetState()
    {
        // Arrange
        var checkpointStore = Substitute.For<IProjectionCheckpointStore>();
        checkpointStore.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(50L));
        var sut = new ProjectionManager(_eventStore, NullLogger<ProjectionManager>.Instance, checkpointStore);
        var projection = new TestProjection();
        sut.RegisterProjection(projection);

        IReadOnlyList<IDomainEvent> events = Array.Empty<IDomainEvent>();
        _eventStore.GetEventsAsync(Arg.Any<string>(), 50L, Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        // Act
        var result = await sut.RebuildProjectionAsync(projection.ProjectionId, "agg-1");

        // Assert — no projection.Reset because checkpoint > 0
        result.IsSuccess.Should().BeTrue();
        projection.ResetCount.Should().Be(0);
    }

    [Fact]
    public async Task RebuildProjectionAsync_WhenProjectionApplyThrows_ReturnsFailure()
    {
        // Arrange
        var projection = new ThrowingProjection();
        _sut.RegisterProjection(projection);
        IReadOnlyList<IDomainEvent> events = new[] { new TestEvent() };
        _eventStore.GetEventsAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(events));

        // Act
        var result = await _sut.RebuildProjectionAsync(projection.ProjectionId, "agg-1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Projection.RebuildFailed");
    }

    [Fact]
    public async Task GetProjectionStateAsync_EmptyProjectionId_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.GetProjectionStateAsync<object>("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionManager.InvalidProjectionId");
    }

    [Fact]
    public async Task GetProjectionStateAsync_UnknownProjection_ReturnsNotFound()
    {
        // Arrange / Act
        var result = await _sut.GetProjectionStateAsync<object>("missing");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Projection.NotFound");
    }

    [Fact]
    public async Task GetProjectionStateAsync_WrongType_ReturnsTypeFailure()
    {
        // Arrange
        var projection = new TestProjection();
        _sut.RegisterProjection(projection);

        // Act
        var result = await _sut.GetProjectionStateAsync<string>(projection.ProjectionId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatisticsAsync_NoProjections_ReturnsEmpty()
    {
        // Arrange / Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProjections.Should().Be(0);
        result.Value.ProjectionDetails.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_AfterRegistration_ReportsCount()
    {
        // Arrange
        _sut.RegisterProjection(new TestProjection());

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProjections.Should().Be(1);
        result.Value.ProjectionDetails.Should().HaveCount(1);
    }

    [Fact]
    public void Dispose_TwiceIsIdempotent()
    {
        // Arrange / Act / Assert
        _sut.Dispose();
        _sut.Dispose();
    }

    [Fact]
    public async Task ProcessEventAsync_AfterDispose_Throws()
    {
        // Arrange
        _sut.Dispose();

        // Act
        Func<Task> act = async () => await _sut.ProcessEventAsync("a", new TestEvent());

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    private sealed class TestProjection : IProjection
    {
        public string ProjectionId => "TestProjection";
        public int Version { get; private set; }
        public int ResetCount { get; private set; }

        public Task ApplyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Version++;
            return Task.CompletedTask;
        }

        public Task<object> GetStateAsync(CancellationToken cancellationToken = default) => Task.FromResult<object>(this);

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            Version = 0;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingProjection : IProjection
    {
        public string ProjectionId => "ThrowingProjection";
        public int Version => 0;
        public Task ApplyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException("apply-fail"));
        public Task<object> GetStateAsync(CancellationToken cancellationToken = default) => Task.FromResult<object>(new());
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public string AggregateId { get; init; } = "agg";
        public string AggregateType { get; init; } = "Test";
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public long AggregateVersion { get; init; } = 1;
        public int EventVersion { get; init; } = 1;
    }
}
