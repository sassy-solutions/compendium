// -----------------------------------------------------------------------
// <copyright file="TransactionBehaviorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using Compendium.Application.CQRS.Behaviors;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.Tests.CQRS.Behaviors;

/// <summary>
/// Unit tests for the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class TransactionBehaviorTests
{
    public sealed class FakeCommand : ICommand
    {
        public string? Value { get; init; }
    }

    public sealed class FakeQuery : IQuery<string>
    {
    }

    public sealed class FakeResponse
    {
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new TransactionBehavior<FakeCommand, FakeResponse>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, FakeResponse>(connectionFactory: null);

        // Act
        var act = async () => await behavior.HandleAsync(
            null!,
            () => Task.FromResult(new FakeResponse()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public async Task HandleAsync_WhenNextIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, FakeResponse>(connectionFactory: null);

        // Act
        var act = async () => await behavior.HandleAsync(
            new FakeCommand(),
            null!,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsQuery_BypassesTransactionLogic()
    {
        // Arrange — build with a connection factory; if the behavior tried to open
        // a tx for the query it would fail because the factory is null-returning.
        var behavior = CreateBehavior<FakeQuery, FakeResponse>(connectionFactory: null);
        var expected = new FakeResponse();

        // Act
        var actual = await behavior.HandleAsync(
            new FakeQuery(),
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandAndNoConnectionFactory_BypassesTransactionLogic()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, FakeResponse>(connectionFactory: null);
        var expected = new FakeResponse();

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndSuccess_CommitsTransaction()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, Result>(() => conn);

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        actual.IsSuccess.Should().BeTrue();
        conn.LastTransaction!.Committed.Should().BeTrue();
        conn.LastTransaction!.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndFailure_RollsBackTransaction()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, Result>(() => conn);

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(Result.Failure(Error.Failure("X", "y"))),
            CancellationToken.None);

        // Assert
        actual.IsFailure.Should().BeTrue();
        conn.LastTransaction!.RolledBack.Should().BeTrue();
        conn.LastTransaction!.Committed.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndGenericResultFailure_RollsBack()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, Result<int>>(() => conn);

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(Result.Failure<int>(Error.Failure("X", "y"))),
            CancellationToken.None);

        // Assert
        actual.IsFailure.Should().BeTrue();
        conn.LastTransaction!.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndGenericResultSuccess_Commits()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, Result<int>>(() => conn);

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(Result.Success(7)),
            CancellationToken.None);

        // Assert
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().Be(7);
        conn.LastTransaction!.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndArbitraryResponse_Commits()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, FakeResponse>(() => conn);
        var expected = new FakeResponse();

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand(),
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert — non-Result responses default to "success" → commit.
        actual.Should().Be(expected);
        conn.LastTransaction!.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandWithFactoryAndNextThrows_RollsBackAndRethrows()
    {
        // Arrange
        var conn = new FakeDbConnection();
        var behavior = CreateBehavior<FakeCommand, Result>(() => conn);

        // Act
        var act = async () => await behavior.HandleAsync(
            new FakeCommand(),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        conn.LastTransaction!.RolledBack.Should().BeTrue();
    }

    private static TransactionBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        Func<DbConnection>? connectionFactory)
        where TRequest : class
        where TResponse : class
    {
        var logger = Substitute.For<ILogger<TransactionBehavior<TRequest, TResponse>>>();
        return new TransactionBehavior<TRequest, TResponse>(logger, connectionFactory);
    }

    private sealed class FakeDbConnection : DbConnection
    {
        public FakeDbTransaction? LastTransaction { get; private set; }

        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "test";

        public override string DataSource => "memory";

        public override string ServerVersion => "1.0";

        public override ConnectionState State { get; } = ConnectionState.Open;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            LastTransaction = new FakeDbTransaction(this, isolationLevel);
            return LastTransaction;
        }

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private sealed class FakeDbTransaction : DbTransaction
    {
        private readonly DbConnection _conn;
        private readonly IsolationLevel _isolationLevel;

        public FakeDbTransaction(DbConnection conn, IsolationLevel isolationLevel)
        {
            _conn = conn;
            _isolationLevel = isolationLevel;
        }

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public override IsolationLevel IsolationLevel => _isolationLevel;

        protected override DbConnection DbConnection => _conn;

        public override void Commit() => Committed = true;

        public override Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public override void Rollback() => RolledBack = true;

        public override Task RollbackAsync(CancellationToken cancellationToken)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }
    }
}
