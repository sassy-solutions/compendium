// -----------------------------------------------------------------------
// <copyright file="IUnitOfWorkContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Infrastructure;

namespace Compendium.Abstractions.Tests.Infrastructure;

public class IUnitOfWorkContractTests
{
    [Fact]
    public async Task IUnitOfWork_Substitute_SaveChangesAsync_ReturnsAffectedCount()
    {
        // Arrange
        using var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(3));

        // Act
        var result = await uow.SaveChangesAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public async Task IUnitOfWork_Substitute_BeginCommitRollback_ReturnSuccessByDefault()
    {
        // Arrange
        using var uow = Substitute.For<IUnitOfWork>();
        uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        uow.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        uow.RollbackTransactionAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var begin = await uow.BeginTransactionAsync(CancellationToken.None);
        var commit = await uow.CommitTransactionAsync(CancellationToken.None);
        var rollback = await uow.RollbackTransactionAsync(CancellationToken.None);

        // Assert
        begin.IsSuccess.Should().BeTrue();
        commit.IsSuccess.Should().BeTrue();
        rollback.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IUnitOfWork_Substitute_PropagatesFailureFromCommit()
    {
        // Arrange
        using var uow = Substitute.For<IUnitOfWork>();
        var error = Error.Conflict("uow.commit_failed", "concurrency");
        uow.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        // Act
        var result = await uow.CommitTransactionAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void IUnitOfWork_DerivesFromIDisposable()
    {
        // Arrange / Act / Assert — important contract: callers can dispose
        typeof(IDisposable).IsAssignableFrom(typeof(IUnitOfWork)).Should().BeTrue();
    }

    [Fact]
    public void IUnitOfWork_Substitute_DisposeCanBeCalledWithoutThrowing()
    {
        // Arrange
        var uow = Substitute.For<IUnitOfWork>();

        // Act
        var act = () => uow.Dispose();

        // Assert
        act.Should().NotThrow();
        uow.Received(1).Dispose();
    }
}
