// -----------------------------------------------------------------------
// <copyright file="IRepositoryContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Persistence;
using Compendium.Core.Domain.Primitives;
using Compendium.Core.Domain.Specifications;

namespace Compendium.Abstractions.Tests.Persistence;

public class IRepositoryContractTests
{
    public sealed class FakeAggregate : AggregateRoot<Guid>
    {
        public FakeAggregate(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public async Task IRepository_Substitute_GetByIdAsync_ReturnsConfiguredAggregate()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var id = Guid.NewGuid();
        var agg = new FakeAggregate(id);
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Success(agg));

        // Act
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(agg);
    }

    [Fact]
    public async Task IRepository_Substitute_GetByIdAsync_PropagatesNotFoundError()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var id = Guid.NewGuid();
        var error = Error.NotFound("repo.not_found", "missing");
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Failure<FakeAggregate>(error));

        // Act
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task IRepository_Substitute_FindAsync_ReturnsConfiguredCollection()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var spec = Substitute.For<ISpecification<FakeAggregate>>();
        IEnumerable<FakeAggregate> aggregates = new[] { new FakeAggregate(Guid.NewGuid()) };
        repo.FindAsync(spec, Arg.Any<CancellationToken>()).Returns(Result.Success(aggregates));

        // Act
        var result = await repo.FindAsync(spec, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task IRepository_Substitute_AddAsync_ReturnsSuccess()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var agg = new FakeAggregate(Guid.NewGuid());
        repo.AddAsync(agg, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await repo.AddAsync(agg, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).AddAsync(agg, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IRepository_Substitute_UpdateAsync_PropagatesConflictError()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var agg = new FakeAggregate(Guid.NewGuid());
        var error = Error.Conflict("repo.concurrency", "version mismatch");
        repo.UpdateAsync(agg, Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        // Act
        var result = await repo.UpdateAsync(agg, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("repo.concurrency");
    }

    [Fact]
    public async Task IRepository_Substitute_RemoveAsync_ReturnsSuccess()
    {
        // Arrange
        var repo = Substitute.For<IRepository<FakeAggregate, Guid>>();
        var agg = new FakeAggregate(Guid.NewGuid());
        repo.RemoveAsync(agg, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await repo.RemoveAsync(agg, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
