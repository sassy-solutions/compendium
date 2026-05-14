// -----------------------------------------------------------------------
// <copyright file="ChoreographyRouterTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Application.Sagas.Choreography;
using Compendium.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Application.Tests.Sagas.Choreography;

/// <summary>
/// Unit tests for the <see cref="ChoreographyRouter"/> class.
/// </summary>
public class ChoreographyRouterTests
{
    public sealed class TestEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

        public string EventType => "test.event";

        public int EventVersion => 1;

        public string? CorrelationId { get; init; }

        public string? CausationId { get; init; }
    }

    public sealed class SuccessHandler : IHandle<TestEvent>
    {
        public bool Called { get; private set; }

        public IChoreographyContext? CapturedContext { get; private set; }

        public Task<Result> HandleAsync(TestEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedContext = context;
            return Task.FromResult(Result.Success());
        }
    }

    public sealed class FailureHandler : IHandle<TestEvent>
    {
        public Task<Result> HandleAsync(TestEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Failure(Error.Failure("Handler.Bad", "failed")));
        }
    }

    public sealed class ThrowingHandler : IHandle<TestEvent>
    {
        public Task<Result> HandleAsync(TestEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("kaboom");
        }
    }

    public sealed class CancelHandler : IHandle<TestEvent>
    {
        public Task<Result> HandleAsync(TestEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    [Fact]
    public void Constructor_WhenServiceProviderIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => new ChoreographyRouter(null!, Substitute.For<IIntegrationEventPublisher>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WhenPublisherIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var sp = new ServiceCollection().BuildServiceProvider();
        var act = () => new ChoreographyRouter(sp, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("publisher");
    }

    [Fact]
    public async Task DispatchAsync_WhenEventIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        // Act
        var act = async () => await router.DispatchAsync<TestEvent>(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlersRegistered_ReturnsSuccess()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        // Act
        var result = await router.DispatchAsync(new TestEvent(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerSucceeds_ReturnsSuccessAndForwardsContext()
    {
        // Arrange
        var handler = new SuccessHandler();
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(handler)
            .BuildServiceProvider();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var router = new ChoreographyRouter(sp, publisher);

        var ev = new TestEvent { CorrelationId = "corr-1" };

        // Act
        var result = await router.DispatchAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.Called.Should().BeTrue();
        handler.CapturedContext!.CorrelationId.Should().Be("corr-1");
        handler.CapturedContext!.CausationId.Should().Be(ev.EventId.ToString());
    }

    [Fact]
    public async Task DispatchAsync_WhenCorrelationIdMissing_FallsBackToEventId()
    {
        // Arrange
        var handler = new SuccessHandler();
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(handler)
            .BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        var ev = new TestEvent { CorrelationId = null };

        // Act
        var result = await router.DispatchAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.CapturedContext!.CorrelationId.Should().Be(ev.EventId.ToString());
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerReturnsFailure_AggregatesError()
    {
        // Arrange
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(new FailureHandler())
            .BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        // Act
        var result = await router.DispatchAsync(new TestEvent(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Choreography.HandlerFailures");
        result.Error.Message.Should().Contain("Handler.Bad");
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_AggregatesAsHandlerFailure()
    {
        // Arrange
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(new ThrowingHandler())
            .BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        // Act
        var result = await router.DispatchAsync(new TestEvent(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Choreography.HandlerFailures");
        result.Error.Message.Should().Contain("kaboom");
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerCancelled_PropagatesOperationCanceled()
    {
        // Arrange
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(new CancelHandler())
            .BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await router.DispatchAsync(new TestEvent(), cts.Token);

        // Assert — TargetInvocationException wraps OperationCanceledException; the router rethrows the inner.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_AggregatesAllFailures()
    {
        // Arrange
        var sp = new ServiceCollection()
            .AddSingleton<IHandle<TestEvent>>(new FailureHandler())
            .AddSingleton<IHandle<TestEvent>>(new ThrowingHandler())
            .BuildServiceProvider();
        var router = new ChoreographyRouter(sp, Substitute.For<IIntegrationEventPublisher>());

        // Act
        var result = await router.DispatchAsync(new TestEvent(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Handler.Bad");
        result.Error.Message.Should().Contain("kaboom");
    }
}
