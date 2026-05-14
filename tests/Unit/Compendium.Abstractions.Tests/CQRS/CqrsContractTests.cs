// -----------------------------------------------------------------------
// <copyright file="CqrsContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.CQRS.Commands;
using Compendium.Abstractions.CQRS.Handlers;
using Compendium.Abstractions.CQRS.Queries;

namespace Compendium.Abstractions.Tests.CQRS;

public class CqrsContractTests
{
    public sealed record FakeCommand(string Payload) : ICommand;

    public sealed record FakeCommandWithResponse(string Payload) : ICommand<int>;

    public sealed record FakeQuery(string Filter) : IQuery<string>;

    [Fact]
    public void ICommand_IsImplementedByConcreteType()
    {
        // Arrange / Act
        var cmd = new FakeCommand("hello");

        // Assert
        cmd.Should().BeAssignableTo<ICommand>();
    }

    [Fact]
    public void ICommandWithResponse_AlsoImplementsBaseICommand()
    {
        // Arrange / Act
        var cmd = new FakeCommandWithResponse("hello");

        // Assert
        cmd.Should().BeAssignableTo<ICommand>();
        cmd.Should().BeAssignableTo<ICommand<int>>();
    }

    [Fact]
    public void IQuery_IsImplementedByConcreteType()
    {
        // Arrange / Act
        var query = new FakeQuery("filter");

        // Assert
        query.Should().BeAssignableTo<IQuery<string>>();
    }

    [Fact]
    public async Task ICommandHandler_Substitute_ReturnsConfiguredSuccessResult()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<FakeCommand>>();
        var cmd = new FakeCommand("noop");
        handler.HandleAsync(cmd, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await handler.Received(1).HandleAsync(cmd, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ICommandHandler_Substitute_PropagatesFailureResult()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<FakeCommand>>();
        var cmd = new FakeCommand("fail");
        var error = Error.Validation("cmd.invalid", "bad payload");
        handler.HandleAsync(cmd, Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task ICommandHandlerWithResponse_Substitute_ReturnsConfiguredValue()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<FakeCommandWithResponse, int>>();
        var cmd = new FakeCommandWithResponse("answer");
        handler.HandleAsync(cmd, Arg.Any<CancellationToken>()).Returns(Result.Success(42));

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task IQueryHandler_Substitute_ReturnsConfiguredValue()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<FakeQuery, string>>();
        var query = new FakeQuery("anything");
        handler.HandleAsync(query, Arg.Any<CancellationToken>()).Returns(Result.Success("ok"));

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task IQueryHandler_Substitute_PropagatesNotFoundError()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<FakeQuery, string>>();
        var query = new FakeQuery("missing");
        var error = Error.NotFound("query.not_found", "not here");
        handler.HandleAsync(query, Arg.Any<CancellationToken>()).Returns(Result.Failure<string>(error));

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
