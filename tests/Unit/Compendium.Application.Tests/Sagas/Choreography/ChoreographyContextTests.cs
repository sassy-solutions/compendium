// -----------------------------------------------------------------------
// <copyright file="ChoreographyContextTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Core.Domain.Events;

namespace Compendium.Application.Tests.Sagas.Choreography;

/// <summary>
/// Unit tests for the internal ChoreographyContext class. Reflection-based access
/// keeps the test in the public surface contract without coupling to a non-public type.
/// </summary>
public class ChoreographyContextTests
{
    private sealed class FakeIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        public string EventType => "fake.event";

        public int EventVersion => 1;

        public string? CorrelationId { get; init; }

        public string? CausationId { get; init; }
    }

    private static IChoreographyContext CreateContext(string corr, string cause, IIntegrationEventPublisher publisher)
    {
        var asm = typeof(Compendium.Application.Sagas.Choreography.ChoreographyRouter).Assembly;
        var ctxType = asm.GetType("Compendium.Application.Sagas.Choreography.ChoreographyContext", throwOnError: true)!;
        var ctor = ctxType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { typeof(string), typeof(string), typeof(IIntegrationEventPublisher) })!;
        return (IChoreographyContext)ctor.Invoke(new object[] { corr, cause, publisher });
    }

    [Fact]
    public void Constructor_WhenCorrelationIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();

        // Act
        var act = () => CreateContext(null!, "cause", publisher);

        // Assert
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>()
            .WithParameterName("correlationId");
    }

    [Fact]
    public void Constructor_WhenCausationIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();

        // Act
        var act = () => CreateContext("corr", null!, publisher);

        // Assert
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>()
            .WithParameterName("causationId");
    }

    [Fact]
    public void Constructor_WhenPublisherIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CreateContext("corr", "cause", null!);

        // Assert
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>()
            .WithParameterName("publisher");
    }

    [Fact]
    public async Task PublishAsync_DelegatesToInjectedPublisher()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        publisher.PublishAsync(Arg.Any<FakeIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var context = CreateContext("corr", "cause", publisher);
        context.CorrelationId.Should().Be("corr");
        context.CausationId.Should().Be("cause");

        var ev = new FakeIntegrationEvent();

        // Act
        var result = await context.PublishAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await publisher.Received(1).PublishAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishCompensationAsync_DelegatesToInjectedPublisher()
    {
        // Arrange
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        publisher.PublishAsync(Arg.Any<FakeIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var context = CreateContext("corr", "cause", publisher);
        var ev = new FakeIntegrationEvent();

        // Act
        var result = await context.PublishCompensationAsync(ev, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await publisher.Received(1).PublishAsync(ev, Arg.Any<CancellationToken>());
    }
}
