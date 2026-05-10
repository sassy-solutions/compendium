// -----------------------------------------------------------------------
// <copyright file="PostgreSqlEventStoreTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Core.EventSourcing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.EventStore;

/// <summary>
/// Unit tests for the constructor and validation surface of <see cref="PostgreSqlEventStore"/>.
/// Methods that touch a real <see cref="Npgsql.NpgsqlConnection"/> are covered in the integration suite
/// (<c>tests/Integration/Compendium.IntegrationTests/EventStore/PostgreSqlEventStoreIntegrationTests.cs</c>).
/// </summary>
public class PostgreSqlEventStoreTests
{
    private const string ValidConnectionString = "Host=localhost;Username=u;Password=p;Database=d";

    private static IOptions<PostgreSqlOptions> Options(PostgreSqlOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    [Fact]
    public void Ctor_WhenOptionsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(null!, deserializer, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenDeserializerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(options, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_WhenLoggerNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(options, deserializer, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WhenConnectionStringMissing_ThrowsArgumentException(string connectionString)
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = connectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(options, deserializer, logger);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Connection string is required*");
    }

    [Fact]
    public void Ctor_WhenInvalidPoolingConfiguration_ThrowsArgumentException()
    {
        // Arrange — MaxPoolSize lower than MaximumPoolSize triggers a validation failure
        var options = Options(new PostgreSqlOptions
        {
            ConnectionString = ValidConnectionString,
            MaxPoolSize = 10,
            MaximumPoolSize = 50,
            MinimumPoolSize = 5,
        });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(options, deserializer, logger);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Invalid PostgreSQL configuration*");
    }

    [Fact]
    public void Ctor_WhenValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();

        // Act
        Action act = () => _ = new PostgreSqlEventStore(options, deserializer, logger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        var sut = new PostgreSqlEventStore(options, deserializer, logger);

        // Act
        await sut.DisposeAsync();
        Func<Task> act = async () => await sut.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AppendEventsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        var sut = new PostgreSqlEventStore(options, deserializer, logger);
        await sut.DisposeAsync();

        // Act
        Func<Task> act = async () => await sut.AppendEventsAsync(
            "agg-1",
            new List<Compendium.Core.Domain.Events.IDomainEvent>
            {
                Substitute.For<Compendium.Core.Domain.Events.IDomainEvent>(),
            },
            -1,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AppendEventsAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        var events = new List<Compendium.Core.Domain.Events.IDomainEvent>
        {
            Substitute.For<Compendium.Core.Domain.Events.IDomainEvent>(),
        };

        // Act
        var result = await sut.AppendEventsAsync(aggregateId, events, -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task AppendEventsAsync_WhenEventListEmpty_ReturnsSuccessWithoutTouchingDb()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.AppendEventsAsync(
            "agg-1",
            Array.Empty<Compendium.Core.Domain.Events.IDomainEvent>(),
            -1,
            CancellationToken.None);

        // Assert — the method short-circuits before opening a connection.
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventsAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventsAsync_FromVersion_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync(aggregateId, fromVersion: 0, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsAsync_FromVersion_WhenFromVersionNegative_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync("agg-1", fromVersion: -1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidFromVersion");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventsAsync_Paginated_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync(aggregateId, skip: 0, take: 10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task GetEventsAsync_Paginated_WhenSkipNegative_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync("agg-1", skip: -1, take: 10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidSkip");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public async Task GetEventsAsync_Paginated_WhenTakeOutOfRange_ReturnsValidationFailure(int take)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsAsync("agg-1", skip: 0, take: take, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidTake");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEventsInRangeAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsInRangeAsync(aggregateId, fromVersion: 0, toVersion: 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    public async Task GetEventsInRangeAsync_WhenVersionsNegative_ReturnsValidationFailure(long from, long to)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsInRangeAsync("agg-1", fromVersion: from, toVersion: to, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidVersionRange");
    }

    [Fact]
    public async Task GetEventsInRangeAsync_WhenFromGreaterThanTo_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetEventsInRangeAsync("agg-1", fromVersion: 5, toVersion: 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidVersionRange");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLastEventAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetLastEventAsync(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetVersionAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetVersionAsync(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsAsync_WhenAggregateIdEmpty_ReturnsValidationFailure(string aggregateId)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ExistsAsync(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EventStore.InvalidAggregateId");
    }

    [Fact]
    public async Task InitializeSchemaAsync_WhenAutoCreateDisabled_ReturnsSuccessImmediately()
    {
        // Arrange — default options have AutoCreateSchema = false
        var sut = CreateSut();

        // Act
        var result = await sut.InitializeSchemaAsync(CancellationToken.None);

        // Assert — the method short-circuits without opening a connection
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeSchemaAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange — enable auto-create so it would actually try to open a connection
        var options = Options(new PostgreSqlOptions
        {
            ConnectionString = ValidConnectionString,
            AutoCreateSchema = true,
        });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        var sut = new PostgreSqlEventStore(options, deserializer, logger);
        await sut.DisposeAsync();

        // Act
        Func<Task> act = async () => await sut.InitializeSchemaAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    private static PostgreSqlEventStore CreateSut()
    {
        var options = Options(new PostgreSqlOptions { ConnectionString = ValidConnectionString });
        var deserializer = Substitute.For<IEventDeserializer>();
        var logger = Substitute.For<ILogger<PostgreSqlEventStore>>();
        return new PostgreSqlEventStore(options, deserializer, logger);
    }
}
