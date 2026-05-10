// -----------------------------------------------------------------------
// <copyright file="IdempotencyBehaviorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Application.CQRS.Behaviors;
using Compendium.Application.Idempotency;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.Tests.CQRS.Behaviors;

/// <summary>
/// Unit tests for the <see cref="IdempotencyBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class IdempotencyBehaviorTests
{
    public sealed class FakeCommand : ICommand
    {
        public string? Id { get; init; }
    }

    public sealed class FakeQuery : IQuery<string>
    {
        public string? Search { get; init; }
    }

    public sealed class TestResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    [Fact]
    public void Constructor_WhenIdempotencyServiceIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new IdempotencyBehavior<FakeCommand, TestResponse>(
            null!,
            Substitute.For<ILogger<IdempotencyBehavior<FakeCommand, TestResponse>>>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("idempotencyService");
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new IdempotencyBehavior<FakeCommand, TestResponse>(
            Substitute.For<IIdempotencyService>(),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out _);

        // Act
        var act = async () => await behavior.HandleAsync(
            null!,
            () => Task.FromResult(new TestResponse()),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public async Task HandleAsync_WhenNextIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out _);

        // Act
        var act = async () => await behavior.HandleAsync(
            new FakeCommand(),
            null!,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsQueryNotCommand_BypassesIdempotency()
    {
        // Arrange
        var behavior = CreateBehavior<FakeQuery, TestResponse>(out var service);
        var expected = new TestResponse { Value = "raw" };

        // Act
        var actual = await behavior.HandleAsync(
            new FakeQuery(),
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
        await service.DidNotReceive().IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenCommandNotProcessed_InvokesNextAndCachesResult()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out var service);
        service.IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var expected = new TestResponse { Value = "first-run" };

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand { Id = "k" },
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
        await service.Received(1).SetResultAsync(
            Arg.Any<string>(),
            expected,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenCommandAlreadyProcessed_ReturnsCachedResult()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out var service);
        var cached = new TestResponse { Value = "cached" };

        service.IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        service.GetResultAsync<TestResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestResponse?>(cached));

        var nextCalled = false;

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand { Id = "k" },
            () =>
            {
                nextCalled = true;
                return Task.FromResult(new TestResponse { Value = "fresh" });
            },
            CancellationToken.None);

        // Assert
        actual.Should().Be(cached);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenProcessedButCacheMissing_FallsBackToNext()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out var service);

        service.IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        service.GetResultAsync<TestResponse>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestResponse?>(null));

        var fresh = new TestResponse { Value = "fresh" };

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand { Id = "k" },
            () => Task.FromResult(fresh),
            CancellationToken.None);

        // Assert
        actual.Should().Be(fresh);
    }

    [Fact]
    public async Task HandleAsync_WhenCacheSetThrows_LogsAndStillReturnsResponse()
    {
        // Arrange
        var behavior = CreateBehavior<FakeCommand, TestResponse>(out var service);
        service.IsProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        service.SetResultAsync(Arg.Any<string>(), Arg.Any<TestResponse>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var expected = new TestResponse { Value = "v" };

        // Act
        var actual = await behavior.HandleAsync(
            new FakeCommand { Id = "k" },
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert — cache failure must not propagate.
        actual.Should().Be(expected);
    }

    private static IdempotencyBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        out IIdempotencyService service)
        where TRequest : class
        where TResponse : class
    {
        service = Substitute.For<IIdempotencyService>();
        var logger = Substitute.For<ILogger<IdempotencyBehavior<TRequest, TResponse>>>();
        return new IdempotencyBehavior<TRequest, TResponse>(service, logger);
    }
}
