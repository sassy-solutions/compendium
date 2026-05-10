// -----------------------------------------------------------------------
// <copyright file="ChoreographyContractTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Choreography;

namespace Compendium.Abstractions.Tests.Sagas.Choreography;

public class ChoreographyContractTests
{
    public sealed record FakeIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public string EventType { get; init; } = nameof(FakeIntegrationEvent);
        public int EventVersion { get; init; } = 1;
        public string? CorrelationId { get; init; }
        public string? CausationId { get; init; }
    }

    [Fact]
    public async Task IIntegrationEventPublisher_Substitute_PublishAsync_ReturnsSuccess()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var evt = new FakeIntegrationEvent();
        publisher.PublishAsync(evt, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await publisher.PublishAsync(evt, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await publisher.Received(1).PublishAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IIntegrationEventPublisher_Substitute_PublishAsync_PropagatesFailure()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var evt = new FakeIntegrationEvent();
        var error = Error.Unavailable("broker.down", "kaboom");
        publisher.PublishAsync(evt, Arg.Any<CancellationToken>()).Returns(Result.Failure(error));

        // Act
        var result = await publisher.PublishAsync(evt, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task IChoreographyContext_Substitute_PublishAsync_ForwardsEventAndReturnsSuccess()
    {
        // Arrange
        var context = Substitute.For<IChoreographyContext>();
        context.CorrelationId.Returns("corr-1");
        context.CausationId.Returns("caus-1");
        var evt = new FakeIntegrationEvent();
        context.PublishAsync(evt, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var correlation = context.CorrelationId;
        var causation = context.CausationId;
        var result = await context.PublishAsync(evt, CancellationToken.None);

        // Assert
        correlation.Should().Be("corr-1");
        causation.Should().Be("caus-1");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IChoreographyContext_Substitute_PublishCompensationAsync_ReturnsConfiguredResult()
    {
        // Arrange
        var context = Substitute.For<IChoreographyContext>();
        var evt = new FakeIntegrationEvent();
        context.PublishCompensationAsync(evt, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await context.PublishCompensationAsync(evt, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await context.Received(1).PublishCompensationAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IChoreographyRouter_Substitute_DispatchAsync_ForwardsEventAndReturnsSuccess()
    {
        // Arrange
        var router = Substitute.For<IChoreographyRouter>();
        var evt = new FakeIntegrationEvent();
        router.DispatchAsync(evt, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await router.DispatchAsync(evt, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await router.Received(1).DispatchAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IHandle_Substitute_HandleAsync_ReturnsConfiguredResult()
    {
        // Arrange
        var handler = Substitute.For<IHandle<FakeIntegrationEvent>>();
        var ctx = Substitute.For<IChoreographyContext>();
        var evt = new FakeIntegrationEvent();
        handler.HandleAsync(evt, ctx, Arg.Any<CancellationToken>()).Returns(Result.Success());

        // Act
        var result = await handler.HandleAsync(evt, ctx, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await handler.Received(1).HandleAsync(evt, ctx, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IHandle_DerivesFromIEventChoreography()
    {
        // Arrange / Act / Assert
        typeof(IEventChoreography).IsAssignableFrom(typeof(IHandle<FakeIntegrationEvent>)).Should().BeTrue();
    }

    private sealed class FakeChoreographyParticipant : IEventChoreography
    {
    }

    [Fact]
    public void IEventChoreography_IsImplementableAsMarker()
    {
        // Arrange / Act
        IEventChoreography marker = new FakeChoreographyParticipant();

        // Assert — pure marker interface
        marker.Should().NotBeNull();
    }
}
