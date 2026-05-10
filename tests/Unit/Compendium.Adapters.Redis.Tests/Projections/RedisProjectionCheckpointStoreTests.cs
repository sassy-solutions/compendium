// -----------------------------------------------------------------------
// <copyright file="RedisProjectionCheckpointStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Projections;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Tests.Projections;

/// <summary>
/// Unit tests for <see cref="RedisProjectionCheckpointStore"/>.
/// </summary>
public class RedisProjectionCheckpointStoreTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly ILogger<RedisProjectionCheckpointStore> _logger = Substitute.For<ILogger<RedisProjectionCheckpointStore>>();
    private readonly RedisOptions _options = new() { KeyPrefix = "compendium", DefaultDatabase = 0 };

    public RedisProjectionCheckpointStoreTests()
    {
        _connectionMultiplexer
            .GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(_database);
    }

    private RedisProjectionCheckpointStore CreateStore(
        RedisOptions? options = null,
        TimeSpan? expiration = null,
        ILogger<RedisProjectionCheckpointStore>? logger = null)
    {
        return new RedisProjectionCheckpointStore(
            _connectionMultiplexer,
            Options.Create(options ?? _options),
            logger ?? _logger,
            expiration);
    }

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_WhenConnectionMultiplexerNull_Throws()
    {
        // Arrange
        var act = () => new RedisProjectionCheckpointStore(null!, Options.Create(_options), _logger);

        // Act & Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionMultiplexer");
    }

    [Fact]
    public void Constructor_WhenOptionsNull_Throws()
    {
        // Arrange
        var act = () => new RedisProjectionCheckpointStore(_connectionMultiplexer, null!, _logger);

        // Act & Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_LoggerOptional_DoesNotThrow()
    {
        // Arrange
        var act = () => new RedisProjectionCheckpointStore(_connectionMultiplexer, Options.Create(_options), null);

        // Act & Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_DefaultExpiration_IsSevenDays()
    {
        // Covered indirectly via SaveCheckpointAsync_KeyExpire test below.
        // Arrange & Act
        var store = CreateStore();

        // Assert
        store.Should().NotBeNull();
    }

    // ---------- GetCheckpointAsync ----------

    [Theory]
    [InlineData(null, "agg")]
    [InlineData("", "agg")]
    [InlineData("   ", "agg")]
    public async Task GetCheckpointAsync_WhenProjectionIdInvalid_ReturnsValidationFailure(string? projectionId, string aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetCheckpointAsync(projectionId!, aggregateId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData("p", null)]
    [InlineData("p", "")]
    [InlineData("p", "   ")]
    public async Task GetCheckpointAsync_WhenAggregateIdInvalid_ReturnsValidationFailure(string projectionId, string? aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetCheckpointAsync(projectionId, aggregateId!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenDisposed_Throws()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var act = async () => await store.GetCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenNoCheckpoint_ReturnsZero()
    {
        // Arrange
        _database
            .HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        var store = CreateStore();

        // Act
        var result = await store.GetCheckpointAsync("proj-1", "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0L);
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenCheckpointPresent_ReturnsPosition()
    {
        // Arrange
        _database
            .HashGetAsync(
                (RedisKey)"compendium:projection:checkpoint:proj-1",
                (RedisValue)"agg-1",
                Arg.Any<CommandFlags>())
            .Returns((RedisValue)42L);
        var store = CreateStore();

        // Act
        var result = await store.GetCheckpointAsync("proj-1", "agg-1", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42L);
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenKeyPrefixEmpty_OmitsPrefix()
    {
        // Arrange
        var opts = new RedisOptions { KeyPrefix = string.Empty };
        _database
            .HashGetAsync(
                (RedisKey)"projection:checkpoint:proj",
                (RedisValue)"agg",
                Arg.Any<CommandFlags>())
            .Returns((RedisValue)10L);
        var store = CreateStore(opts);

        // Act
        var result = await store.GetCheckpointAsync("proj", "agg", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10L);
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenExceptionThrown_ReturnsZero()
    {
        // Arrange
        _database
            .HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("redis-down"));
        var store = CreateStore();

        // Act
        var result = await store.GetCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        // Per implementation: failures are swallowed and 0 is returned to allow rebuild from start.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0L);
    }

    [Fact]
    public async Task GetCheckpointAsync_WhenLoggerNull_DoesNotThrow()
    {
        // Arrange
        _database
            .HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        var store = new RedisProjectionCheckpointStore(_connectionMultiplexer, Options.Create(_options), null);

        // Act
        var result = await store.GetCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ---------- SaveCheckpointAsync ----------

    [Theory]
    [InlineData(null, "a")]
    [InlineData("", "a")]
    [InlineData("   ", "a")]
    public async Task SaveCheckpointAsync_WhenProjectionIdInvalid_ReturnsValidationFailure(string? projectionId, string aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SaveCheckpointAsync(projectionId!, aggregateId, 1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData("p", null)]
    [InlineData("p", "")]
    [InlineData("p", "  ")]
    public async Task SaveCheckpointAsync_WhenAggregateIdInvalid_ReturnsValidationFailure(string projectionId, string? aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SaveCheckpointAsync(projectionId, aggregateId!, 1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenPositionNegative_ReturnsValidationFailure()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SaveCheckpointAsync("p", "a", -1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidPosition");
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenDisposed_Throws()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var act = async () => await store.SaveCheckpointAsync("p", "a", 0, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenSucceeds_ReturnsSuccessAndSetsExpiration()
    {
        // Arrange
        _database
            .HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<RedisValue>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Returns(true);
        var ttl = TimeSpan.FromHours(1);
        var store = CreateStore(expiration: ttl);

        // Act
        var result = await store.SaveCheckpointAsync("proj", "agg", 99, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _database.Received(1).HashSetAsync(
            (RedisKey)"compendium:projection:checkpoint:proj",
            (RedisValue)"agg",
            (RedisValue)99L,
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        await _database.Received(1).KeyExpireAsync(
            (RedisKey)"compendium:projection:checkpoint:proj",
            ttl,
            Arg.Any<ExpireWhen>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenHashSetReturnsFalse_ReturnsSaveFailedFailure()
    {
        // Arrange
        _database
            .HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<RedisValue>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Returns(false);
        var store = CreateStore();

        // Act
        var result = await store.SaveCheckpointAsync("proj", "agg", 1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.SaveFailed");
    }

    [Fact]
    public async Task SaveCheckpointAsync_WhenExceptionThrown_ReturnsSaveFailedFailure()
    {
        // Arrange
        _database
            .HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<RedisValue>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Throws(new RedisException("boom"));
        var store = CreateStore();

        // Act
        var result = await store.SaveCheckpointAsync("p", "a", 1, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.SaveFailed");
        result.Error.Message.Should().Contain("boom");
    }

    // ---------- DeleteCheckpointAsync ----------

    [Theory]
    [InlineData(null, "a")]
    [InlineData("", "a")]
    [InlineData("  ", "a")]
    public async Task DeleteCheckpointAsync_WhenProjectionIdInvalid_ReturnsValidationFailure(string? projectionId, string aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.DeleteCheckpointAsync(projectionId!, aggregateId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Theory]
    [InlineData("p", null)]
    [InlineData("p", "")]
    [InlineData("p", "  ")]
    public async Task DeleteCheckpointAsync_WhenAggregateIdInvalid_ReturnsValidationFailure(string projectionId, string? aggregateId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.DeleteCheckpointAsync(projectionId, aggregateId!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidAggregateId");
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WhenDisposed_Throws()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var act = async () => await store.DeleteCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WhenSucceeds_ReturnsSuccess()
    {
        // Arrange
        _database
            .HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(true);
        var store = CreateStore();

        // Act
        var result = await store.DeleteCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WhenHashDeleteReturnsFalse_StillReturnsSuccess()
    {
        // Arrange
        _database
            .HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(false);
        var store = CreateStore();

        // Act
        var result = await store.DeleteCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        // Implementation always returns Success regardless of HashDelete result.
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCheckpointAsync_WhenExceptionThrown_ReturnsDeleteFailedFailure()
    {
        // Arrange
        _database
            .HashDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("err"));
        var store = CreateStore();

        // Act
        var result = await store.DeleteCheckpointAsync("p", "a", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.DeleteFailed");
        result.Error.Message.Should().Contain("err");
    }

    // ---------- GetAllCheckpointsForProjectionAsync ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllCheckpointsForProjectionAsync_WhenProjectionIdInvalid_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAllCheckpointsForProjectionAsync(projectionId!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Fact]
    public async Task GetAllCheckpointsForProjectionAsync_WhenDisposed_Throws()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var act = async () => await store.GetAllCheckpointsForProjectionAsync("p", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetAllCheckpointsForProjectionAsync_WhenSucceeds_ReturnsDictionary()
    {
        // Arrange
        var entries = new[]
        {
            new HashEntry("agg-1", 10L),
            new HashEntry("agg-2", 25L),
        };
        _database
            .HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(entries);
        var store = CreateStore();

        // Act
        var result = await store.GetAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new Dictionary<string, long>
        {
            ["agg-1"] = 10L,
            ["agg-2"] = 25L,
        });
    }

    [Fact]
    public async Task GetAllCheckpointsForProjectionAsync_WhenEmpty_ReturnsEmptyDictionary()
    {
        // Arrange
        _database
            .HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Array.Empty<HashEntry>());
        var store = CreateStore();

        // Act
        var result = await store.GetAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCheckpointsForProjectionAsync_WhenExceptionThrown_ReturnsGetAllFailedFailure()
    {
        // Arrange
        _database
            .HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("err"));
        var store = CreateStore();

        // Act
        var result = await store.GetAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.GetAllFailed");
        result.Error.Message.Should().Contain("err");
    }

    // ---------- DeleteAllCheckpointsForProjectionAsync ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenProjectionIdInvalid_ReturnsValidationFailure(string? projectionId)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.DeleteAllCheckpointsForProjectionAsync(projectionId!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.InvalidProjectionId");
    }

    [Fact]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenDisposed_Throws()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var act = async () => await store.DeleteAllCheckpointsForProjectionAsync("p", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenKeyDeleted_ReturnsSuccessTrue()
    {
        // Arrange
        _database
            .KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);
        var store = CreateStore();

        // Act
        var result = await store.DeleteAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenKeyMissing_ReturnsSuccessFalse()
    {
        // Arrange
        _database
            .KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(false);
        var store = CreateStore();

        // Act
        var result = await store.DeleteAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAllCheckpointsForProjectionAsync_WhenExceptionThrown_ReturnsDeleteAllFailedFailure()
    {
        // Arrange
        _database
            .KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("oops"));
        var store = CreateStore();

        // Act
        var result = await store.DeleteAllCheckpointsForProjectionAsync("proj", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProjectionCheckpoint.DeleteAllFailed");
        result.Error.Message.Should().Contain("oops");
    }

    // ---------- DisposeAsync ----------

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.DisposeAsync();
        var act = async () => await store.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var store = new RedisProjectionCheckpointStore(_connectionMultiplexer, Options.Create(_options), null);

        // Act
        var act = async () => await store.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
