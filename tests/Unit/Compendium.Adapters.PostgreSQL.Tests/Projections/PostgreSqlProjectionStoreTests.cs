// -----------------------------------------------------------------------
// <copyright file="PostgreSqlProjectionStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Infrastructure.Projections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.Projections;

/// <summary>
/// Unit tests for the constructor and exception-bubbling surface of <see cref="PostgreSqlProjectionStore"/>.
/// CRUD against a real PostgreSQL instance is covered in the integration suite.
/// </summary>
public class PostgreSqlProjectionStoreTests
{
    private const string ValidConnectionString = "Host=localhost;Database=db;Username=u;Password=p";

    private static IOptions<PostgreSqlOptions> Options(PostgreSqlOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<PostgreSqlProjectionStore>>();

        // Act
        Action act = () => _ = new PostgreSqlProjectionStore(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenOptionsValueNull_ThrowsArgumentNullException()
    {
        // Arrange — IOptions.Value returns null
        var stub = Substitute.For<IOptions<PostgreSqlOptions>>();
        stub.Value.Returns((PostgreSqlOptions?)null);
        var logger = Substitute.For<ILogger<PostgreSqlProjectionStore>>();

        // Act
        Action act = () => _ = new PostgreSqlProjectionStore(stub, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenLoggerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });

        // Act
        Action act = () => _ = new PostgreSqlProjectionStore(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenValidArguments_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var logger = Substitute.For<ILogger<PostgreSqlProjectionStore>>();

        // Act
        Action act = () => _ = new PostgreSqlProjectionStore(options, logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.SaveCheckpointAsync("proj-1", position: 0, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.GetCheckpointAsync("proj-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SaveSnapshotAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var projection = Substitute.For<IProjection>();
        projection.ProjectionName.Returns("proj-1");
        projection.Version.Returns(1);

        // Act
        Func<Task> act = async () => await sut.SaveSnapshotAsync(projection, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadSnapshotAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.LoadSnapshotAsync<FakeProjection>("proj-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DeleteProjectionDataAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.DeleteProjectionDataAsync("proj-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetProjectionStateAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.GetProjectionStateAsync("proj-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SaveProjectionStateAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        var state = new ProjectionState
        {
            ProjectionName = "proj-1",
            Version = 1,
            LastProcessedPosition = 0,
            LastProcessedAt = DateTime.UtcNow,
            Status = ProjectionStatus.Idle,
        };

        // Act
        Func<Task> act = async () => await sut.SaveProjectionStateAsync(state, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetProjectionStatisticsAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.GetProjectionStatisticsAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CleanupOldSnapshotsAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.CleanupOldSnapshotsAsync(retentionDays: 30, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseUnavailable_ThrowsException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.InitializeAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    private static PostgreSqlProjectionStore CreateSut()
    {
        var options = Options(new PostgreSqlOptions
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=db;Username=u;Password=p;Timeout=1;Command Timeout=1",
        });
        return new PostgreSqlProjectionStore(options, Substitute.For<ILogger<PostgreSqlProjectionStore>>());
    }

    /// <summary>Stub IProjection for snapshot serialization tests.</summary>
    private sealed class FakeProjection : IProjection
    {
        public string ProjectionName { get; init; } = "fake";

        public int Version { get; init; } = 1;

        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
