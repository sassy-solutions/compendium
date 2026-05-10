// -----------------------------------------------------------------------
// <copyright file="QueryDispatcherTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Application.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Application.Tests.CQRS;

/// <summary>
/// Unit tests for the <see cref="QueryDispatcher"/> class.
/// </summary>
public class QueryDispatcherTests
{
    public sealed class TestQuery : IQuery<string>
    {
        public string? Search { get; init; }
    }

    [Fact]
    public void Constructor_WhenServiceProviderIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new QueryDispatcher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ReturnsValidationFailure()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(null!, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Query.Null");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlerRegistered_ReturnsNotFoundFailure()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(
            new TestQuery(),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Handler.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain(nameof(TestQuery));
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerSucceeds_ReturnsHandlerValue()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<TestQuery, string>>();
        handler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success("hello")));

        var sp = new ServiceCollection()
            .AddSingleton(handler)
            .BuildServiceProvider();
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(
            new TestQuery(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerReturnsFailure_ReturnsFailure()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<TestQuery, string>>();
        handler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<string>(Error.NotFound("Q.NotFound", "no result"))));

        var sp = new ServiceCollection()
            .AddSingleton(handler)
            .BuildServiceProvider();
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(
            new TestQuery(),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Q.NotFound");
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_ReturnsExecutionFailedFailure()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<TestQuery, string>>();
        handler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<string>>>(_ => throw new InvalidOperationException("query bad"));

        var sp = new ServiceCollection()
            .AddSingleton(handler)
            .BuildServiceProvider();
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.DispatchAsync<TestQuery, string>(
            new TestQuery(),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Query.ExecutionFailed");
        result.Error.Message.Should().Contain("query bad");
    }
}
