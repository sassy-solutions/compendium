// -----------------------------------------------------------------------
// <copyright file="RedisIdempotencyStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.Idempotency;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Tests.Idempotency;

/// <summary>
/// Unit tests for <see cref="RedisIdempotencyStore"/>.
/// </summary>
public class RedisIdempotencyStoreTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly ILogger<RedisIdempotencyStore> _logger = Substitute.For<ILogger<RedisIdempotencyStore>>();
    private readonly RedisOptions _options = new() { KeyPrefix = "compendium", DefaultDatabase = 0 };

    public RedisIdempotencyStoreTests()
    {
        _connectionMultiplexer
            .GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(_database);
    }

    private RedisIdempotencyStore CreateStore(RedisOptions? options = null)
    {
        return new RedisIdempotencyStore(
            _connectionMultiplexer,
            Options.Create(options ?? _options),
            _logger);
    }

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_WhenConnectionMultiplexerNull_Throws()
    {
        // Arrange
        var act = () => new RedisIdempotencyStore(null!, Options.Create(_options), _logger);

        // Act & Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionMultiplexer");
    }

    [Fact]
    public void Constructor_WhenOptionsNull_Throws()
    {
        // Arrange
        var act = () => new RedisIdempotencyStore(_connectionMultiplexer, null!, _logger);

        // Act & Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WhenLoggerNull_Throws()
    {
        // Arrange
        var act = () => new RedisIdempotencyStore(_connectionMultiplexer, Options.Create(_options), null!);

        // Act & Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_UsesConfiguredDatabaseNumber()
    {
        // Arrange
        var opts = new RedisOptions { DefaultDatabase = 7 };

        // Act
        _ = CreateStore(opts);

        // Assert
        _connectionMultiplexer.Received(1).GetDatabase(7, Arg.Any<object?>());
    }

    // ---------- ExistsAsync ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_WhenKeyNullOrEmptyOrWhitespace_ReturnsValidationFailure(string? key)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync(key!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.InvalidKey");
    }

    [Fact]
    public async Task ExistsAsync_WhenDisposed_ReturnsFailure()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var result = await store.ExistsAsync("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Disposed");
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyPresent_ReturnsTrueAndPrefixedKey()
    {
        // Arrange
        _database
            .KeyExistsAsync("compendium:my-key", Arg.Any<CommandFlags>())
            .Returns(true);
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync("my-key", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await _database.Received(1).KeyExistsAsync("compendium:my-key", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyAbsent_ReturnsFalse()
    {
        // Arrange
        _database
            .KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(false);
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync("absent", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyPrefixEmpty_DoesNotPrefix()
    {
        // Arrange
        var opts = new RedisOptions { KeyPrefix = string.Empty };
        _database
            .KeyExistsAsync("raw-key", Arg.Any<CommandFlags>())
            .Returns(true);
        var store = CreateStore(opts);

        // Act
        var result = await store.ExistsAsync("raw-key", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _database.Received(1).KeyExistsAsync("raw-key", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ExistsAsync_WhenRedisExceptionThrown_ReturnsFailure()
    {
        // Arrange
        _database
            .KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("redis-down"));
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.ExistsFailed");
        result.Error.Message.Should().Be("redis-down");
    }

    [Fact]
    public async Task ExistsAsync_WhenTimeoutExceptionThrown_ReturnsTimeoutFailure()
    {
        // Arrange
        _database
            .KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new TimeoutException("slow"));
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Timeout");
        result.Error.Message.Should().Be("slow");
    }

    [Fact]
    public async Task ExistsAsync_WhenUnknownExceptionThrown_ReturnsExistsFailedFailure()
    {
        // Arrange
        _database
            .KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("boom"));
        var store = CreateStore();

        // Act
        var result = await store.ExistsAsync("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.ExistsFailed");
        result.Error.Message.Should().Be("boom");
    }

    // ---------- GetAsync ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_WhenKeyNullOrWhitespace_ReturnsSuccessWithDefault(string? key)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<string>(key!, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenDisposed_ReturnsFailure()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var result = await store.GetAsync<string>("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Disposed");
    }

    [Fact]
    public async Task GetAsync_WhenValueNotFound_ReturnsSuccessWithDefault()
    {
        // Arrange
        _database
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<int>("k", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_WhenValueFound_ReturnsDeserializedValue()
    {
        // Arrange
        var payload = new { Name = "abc", Value = 42 };
        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _database
            .StringGetAsync("compendium:k", Arg.Any<CommandFlags>())
            .Returns(json);
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<TestPayload>("k", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("abc");
        result.Value.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_WhenJsonInvalid_ReturnsSerializationFailure()
    {
        // Arrange
        _database
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns("not-json{");
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<TestPayload>("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.SerializationFailed");
    }

    [Fact]
    public async Task GetAsync_WhenRedisExceptionThrown_ReturnsGetFailedFailure()
    {
        // Arrange
        _database
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisException("redis-err"));
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<string>("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.GetFailed");
        result.Error.Message.Should().Be("redis-err");
    }

    [Fact]
    public async Task GetAsync_WhenTimeoutExceptionThrown_ReturnsTimeoutFailure()
    {
        // Arrange
        _database
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new TimeoutException("slow"));
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<string>("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Timeout");
    }

    [Fact]
    public async Task GetAsync_WhenUnknownExceptionThrown_ReturnsGetFailedFailure()
    {
        // Arrange
        _database
            .StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("oops"));
        var store = CreateStore();

        // Act
        var result = await store.GetAsync<string>("k", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.GetFailed");
        result.Error.Message.Should().Be("oops");
    }

    // ---------- SetAsync ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetAsync_WhenKeyNullOrWhitespace_ReturnsValidationFailure(string? key)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SetAsync(key!, "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.InvalidKey");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task SetAsync_WhenExpirationNotPositive_ReturnsValidationFailure(int seconds)
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromSeconds(seconds), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.InvalidExpiration");
    }

    [Fact]
    public async Task SetAsync_WhenDisposed_ReturnsFailure()
    {
        // Arrange
        var store = CreateStore();
        await store.DisposeAsync();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Disposed");
    }

    [Fact]
    public async Task SetAsync_WhenSucceeds_ReturnsSuccessAndUsesPrefixedKey()
    {
        // Arrange
        _database
            .StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Returns(true);
        var store = CreateStore();
        var ttl = TimeSpan.FromMinutes(5);

        // Act
        var result = await store.SetAsync("my-key", new TestPayload { Name = "x", Value = 1 }, ttl, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _database.Received(1).StringSetAsync(
            (RedisKey)"compendium:my-key",
            Arg.Is<RedisValue>(v => ((string?)v)!.Contains("\"name\":\"x\"")),
            ttl,
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WhenStringSetReturnsFalse_ReturnsSetFailedFailure()
    {
        // Arrange
        _database
            .StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Returns(false);
        var store = CreateStore();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.SetFailed");
        result.Error.Message.Should().Contain("k");
    }

    [Fact]
    public async Task SetAsync_WhenRedisExceptionThrown_ReturnsSetFailedFailure()
    {
        // Arrange
        _database
            .StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Throws(new RedisException("net-fail"));
        var store = CreateStore();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.SetFailed");
        result.Error.Message.Should().Be("net-fail");
    }

    [Fact]
    public async Task SetAsync_WhenTimeoutThrown_ReturnsTimeoutFailure()
    {
        // Arrange
        _database
            .StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Throws(new TimeoutException("slow"));
        var store = CreateStore();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.Timeout");
    }

    [Fact]
    public async Task SetAsync_WhenUnknownExceptionThrown_ReturnsSetFailedFailure()
    {
        // Arrange
        _database
            .StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("oops"));
        var store = CreateStore();

        // Act
        var result = await store.SetAsync("k", "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Redis.Idempotency.SetFailed");
        result.Error.Message.Should().Be("oops");
    }

    // ---------- Dispose ----------

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var store = CreateStore();

        // Act
        await store.DisposeAsync();
        var second = async () => await store.DisposeAsync();

        // Assert
        await second.Should().NotThrowAsync();
    }

    private sealed class TestPayload
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }
}
