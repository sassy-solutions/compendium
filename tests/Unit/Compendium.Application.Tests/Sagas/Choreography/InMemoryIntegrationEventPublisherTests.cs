// -----------------------------------------------------------------------
// <copyright file="InMemoryIntegrationEventPublisherTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Application.Sagas.Choreography;
using Compendium.Core.Domain.Events;

namespace Compendium.Application.Tests.Sagas.Choreography;

/// <summary>
/// Unit tests for the <see cref="InMemoryIntegrationEventPublisher"/> class.
/// </summary>
public class InMemoryIntegrationEventPublisherTests
{
    public sealed class FakeEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        public string EventType => "fake.event";

        public int EventVersion => 1;

        public string? CorrelationId { get; init; }

        public string? CausationId { get; init; }
    }

    [Fact]
    public async Task PublishAsync_WhenEventIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = new InMemoryIntegrationEventPublisher();

        // Act
        var act = async () => await publisher.PublishAsync<FakeEvent>(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithoutRouter_BuffersEventAndReturnsSuccess()
    {
        // Arrange
        var publisher = new InMemoryIntegrationEventPublisher();
        var ev = new FakeEvent();

        // Act
        var result = await publisher.PublishAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        publisher.Published.Should().ContainSingle().Which.Should().BeSameAs(ev);
    }

    [Fact]
    public async Task PublishAsync_WhenRouterIsNull_BuffersEventAndReturnsSuccess()
    {
        // Arrange
        var publisher = new InMemoryIntegrationEventPublisher(() => null);
        var ev = new FakeEvent();

        // Act
        var result = await publisher.PublishAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        publisher.Published.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_WhenRouterAvailable_AlsoDispatchesThroughRouter()
    {
        // Arrange
        var router = Substitute.For<IChoreographyRouter>();
        router.DispatchAsync(Arg.Any<FakeEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var publisher = new InMemoryIntegrationEventPublisher(() => router);
        var ev = new FakeEvent();

        // Act
        var result = await publisher.PublishAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        publisher.Published.Should().ContainSingle();
        await router.Received(1).DispatchAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenRouterFails_ReturnsRouterFailure()
    {
        // Arrange
        var router = Substitute.For<IChoreographyRouter>();
        router.DispatchAsync(Arg.Any<FakeEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Router.Down", "fail"))));

        var publisher = new InMemoryIntegrationEventPublisher(() => router);
        var ev = new FakeEvent();

        // Act
        var result = await publisher.PublishAsync(ev, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Router.Down");
    }
}
