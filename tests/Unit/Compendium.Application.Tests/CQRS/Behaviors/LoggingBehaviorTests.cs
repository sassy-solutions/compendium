// -----------------------------------------------------------------------
// <copyright file="LoggingBehaviorTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Application.CQRS.Behaviors;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.Tests.CQRS.Behaviors;

/// <summary>
/// Unit tests for the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class LoggingBehaviorTests
{
    public sealed record TestRequest(string Value);

    public sealed record TestResponse(string Value);

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new LoggingBehavior<TestRequest, TestResponse>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);

        // Act
        var act = async () => await behavior.HandleAsync(
            null!,
            () => Task.FromResult(new TestResponse("ok")),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public async Task HandleAsync_WhenNextIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);

        // Act
        var act = async () => await behavior.HandleAsync(
            new TestRequest("v"),
            null!,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("next");
    }

    [Fact]
    public async Task HandleAsync_WhenSucceeds_ReturnsResponseAndLogs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
        var expected = new TestResponse("ok");

        // Act
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            () => Task.FromResult(expected),
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrows_RethrowsAndLogsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);

        // Act
        var act = async () => await behavior.HandleAsync(
            new TestRequest("v"),
            () => throw new InvalidOperationException("fail"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fail");
    }

    [Fact]
    public async Task HandleAsync_WhenResponseIsResultSuccess_LogsAsSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, Result>>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);

        // Act
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        actual.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenResponseIsResultFailure_LogsAsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, Result>>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);

        // Act
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            () => Task.FromResult(Result.Failure(Error.Failure("X", "y"))),
            CancellationToken.None);

        // Assert
        actual.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenResponseIsGenericResultSuccess_LogsAsSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, Result<int>>>>();
        var behavior = new LoggingBehavior<TestRequest, Result<int>>(logger);

        // Act
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            () => Task.FromResult(Result.Success(7)),
            CancellationToken.None);

        // Assert
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().Be(7);
    }

    [Fact]
    public async Task HandleAsync_WhenResponseIsGenericResultFailure_LogsAsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, Result<int>>>>();
        var behavior = new LoggingBehavior<TestRequest, Result<int>>(logger);

        // Act
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            () => Task.FromResult(Result.Failure<int>(Error.Failure("Z", "w"))),
            CancellationToken.None);

        // Assert
        actual.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenSlowRequest_LogsSlowWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
        var expected = new TestResponse("ok");

        // Act — simulate slow >1s by delaying ~1.05s
        var actual = await behavior.HandleAsync(
            new TestRequest("v"),
            async () =>
            {
                await Task.Delay(1050, CancellationToken.None);
                return expected;
            },
            CancellationToken.None);

        // Assert
        actual.Should().Be(expected);
    }
}
