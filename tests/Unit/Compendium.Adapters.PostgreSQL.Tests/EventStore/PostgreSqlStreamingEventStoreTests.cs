// -----------------------------------------------------------------------
// <copyright file="PostgreSqlStreamingEventStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Core.Domain.Events;
using Compendium.Core.EventSourcing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.EventStore;

/// <summary>
/// Unit tests for the constructor and delegation surface of <see cref="PostgreSqlStreamingEventStore"/>.
/// Methods that hit a real database (streaming, count, max position) are exercised in the integration suite.
/// </summary>
public class PostgreSqlStreamingEventStoreTests
{
    private const string ValidConnectionString = "Host=localhost;Username=u;Password=p;Database=d";

    private static IOptions<PostgreSqlOptions> Options(PostgreSqlOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    private static PostgreSqlEventStore BaseStore()
    {
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        return new PostgreSqlEventStore(options, Substitute.For<IEventDeserializer>(), Substitute.For<ILogger<PostgreSqlEventStore>>());
    }

    [Fact]
    public void Ctor_WhenBaseStoreNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlStreamingEventStore(null!, options, deserializer, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var baseStore = BaseStore();
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlStreamingEventStore(baseStore, null!, deserializer, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenDeserializerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var baseStore = BaseStore();
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlStreamingEventStore(baseStore, options, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenLoggerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var baseStore = BaseStore();
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();

        // Act
        Action act = () => _ = new PostgreSqlStreamingEventStore(baseStore, options, deserializer, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenValidArguments_DoesNotThrow()
    {
        // Arrange
        var baseStore = BaseStore();
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlStreamingEventStore(baseStore, options, deserializer, logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.DisposeAsync();
        Func<Task> act = async () => await sut.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AppendEventsAsync_DelegatesToBase_ReturnsValidationFailureForEmptyAggregateId()
    {
        // Arrange — delegation to the base store happens directly. We exercise a deterministic
        // path that does not need a database connection.
        var sut = CreateSut();

        // Act
        var result = await sut.AppendEventsAsync(
            string.Empty,
            new List<IDomainEvent> { Substitute.For<IDomainEvent>() },
            -1,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsAsync_DelegatesToBase_ReturnsValidationFailureForEmptyAggregateId()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync(string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsAsync_FromVersion_DelegatesToBase_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync(string.Empty, fromVersion: 0, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsInRangeAsync_DelegatesToBase_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsInRangeAsync(string.Empty, fromVersion: 0, toVersion: 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetLastEventAsync_DelegatesToBase_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetLastEventAsync(string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetVersionAsync_DelegatesToBase_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetVersionAsync(string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToBase_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ExistsAsync(string.Empty, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToBase_AfterBaseDisposed_ThrowsObjectDisposedException()
    {
        // Arrange — disposing the base store first forces the delegation to surface ObjectDisposedException.
        var baseStore = BaseStore();
        await baseStore.DisposeAsync();

        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();
        var sut = new PostgreSqlStreamingEventStore(baseStore, options, deserializer, logger);

        // Act
        Func<Task> act = async () =>
            await sut.GetStatisticsAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task StreamEventsAsync_WhenCancelled_StopsImmediately()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var enumerator = sut.StreamEventsAsync(streamId: null, fromPosition: 0, cts.Token).GetAsyncEnumerator(cts.Token);

        // Assert — the loop sees cancellation before opening any connection.
        // We expect the operation to exit (yield break) without throwing for the initial hasMoreEvents check.
        // Some Npgsql versions throw OperationCanceledException; either is acceptable, both prove
        // cancellation propagates without doing real work.
        try
        {
            var hasNext = await enumerator.MoveNextAsync();
            hasNext.Should().BeFalse();
        }
        catch (OperationCanceledException)
        {
            // Acceptable — cancellation was honoured.
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private static PostgreSqlStreamingEventStore CreateSut()
    {
        var baseStore = BaseStore();
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlStreamingEventStore>>();
        return new PostgreSqlStreamingEventStore(baseStore, options, deserializer, logger);
    }
}
