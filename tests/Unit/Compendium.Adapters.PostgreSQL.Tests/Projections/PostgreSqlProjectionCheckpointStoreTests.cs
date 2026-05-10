// -----------------------------------------------------------------------
// <copyright file="PostgreSqlProjectionCheckpointStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.Projections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.Projections;

/// <summary>
/// Unit tests for the constructor and validation surface of <see cref="PostgreSqlProjectionCheckpointStore"/>.
/// Schema and CRUD against a real database are covered in the integration suite.
/// </summary>
public class PostgreSqlProjectionCheckpointStoreTests
{
    private const string ValidConnectionString = "Host=localhost;Database=db;Username=u;Password=p";

    private static IOptions<PostgreSqlOptions> Options(PostgreSqlOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        Action act = () => _ = new PostgreSqlProjectionCheckpointStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenConnectionStringNull_ThrowsInvalidOperationException()
    {
        // Arrange — Options.Value returns a PostgreSqlOptions with null ConnectionString
        var stub = Substitute.For<IOptions<PostgreSqlOptions>>();
        stub.Value.Returns((PostgreSqlOptions?)null);

        // Act
        Action act = () => _ = new PostgreSqlProjectionCheckpointStore(stub);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*connection string*");
    }

    [Fact]
    public void Ctor_WhenValidOptionsWithoutLogger_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });

        // Act
        Action act = () => _ = new PostgreSqlProjectionCheckpointStore(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Ctor_WithLogger_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var logger = Substitute.For<ILogger<PostgreSqlProjectionCheckpointStore>>();

        // Act
        Action act = () => _ = new PostgreSqlProjectionCheckpointStore(options, logger);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCheckpointAsync_WhenProjectionIdEmpty_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetCheckpointAsync(projectionId!, "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCheckpointAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string? aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetCheckpointAsync("proj-1", aggregateId!, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveCheckpointAsync_WhenProjectionIdEmpty_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.SaveCheckpointAsync(projectionId!, "agg-1", position: 0, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveCheckpointAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string? aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.SaveCheckpointAsync("proj-1", aggregateId!, position: 0, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenPositionNegative_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.SaveCheckpointAsync("proj-1", "agg-1", position: -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidPosition");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteCheckpointAsync_WhenProjectionIdEmpty_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCheckpointAsync(projectionId!, "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteCheckpointAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string? aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCheckpointAsync("proj-1", aggregateId!, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllCheckpointsForProjectionAsync_WhenProjectionIdEmpty_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllCheckpointsForProjectionAsync(projectionId!, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenProjectionIdEmpty_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAllCheckpointsForProjectionAsync(projectionId!, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenDatabaseUnavailable_ReturnsFailure()
    {
        // Arrange — pass valid arguments so we exercise the catch branch (no DB available)
        var sut = CreateSut();

        // Act
        var result = await sut.GetCheckpointAsync("proj-1", "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.GetFailed");
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenDatabaseUnavailable_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.SaveCheckpointAsync("proj-1", "agg-1", position: 5, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.SaveFailed");
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WhenDatabaseUnavailable_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCheckpointAsync("proj-1", "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.DeleteFailed");
    }

    [Fact]
    public async Task GetAllCheckpointsForProjectionAsync_WhenDatabaseUnavailable_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllCheckpointsForProjectionAsync("proj-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.GetAllFailed");
    }

    [Fact]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenDatabaseUnavailable_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAllCheckpointsForProjectionAsync("proj-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectionCheckpoint.DeleteAllFailed");
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseUnavailable_RethrowsException()
    {
        // Arrange — InitializeAsync rethrows; we just assert it bubbles.
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.InitializeAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    private static PostgreSqlProjectionCheckpointStore CreateSut()
    {
        // Use a connection string that points to a port nothing is listening on so
        // the methods exit through the catch branch quickly (no real DB needed).
        var options = Options(new PostgreSqlOptions
        {
            ConnectionString = "Host=127.0.0.1;Port=1;Database=db;Username=u;Password=p;Timeout=1;Command Timeout=1",
        });
        return new PostgreSqlProjectionCheckpointStore(options, Substitute.For<ILogger<PostgreSqlProjectionCheckpointStore>>());
    }
}
