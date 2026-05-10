// -----------------------------------------------------------------------
// <copyright file="SagaServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Choreography;
using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Application.Sagas.DependencyInjection;
using Compendium.Application.Sagas.ProcessManagers;
using Compendium.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Application.Tests.Sagas.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="SagaServiceCollectionExtensions"/>.
/// </summary>
public class SagaServiceCollectionExtensionsTests
{
    public sealed class FakeAssemblyEvent : IIntegrationEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

        public string EventType => "fake.event";

        public int EventVersion => 1;

        public string? CorrelationId { get; init; }

        public string? CausationId { get; init; }
    }

    public sealed class AssemblyHandler : IHandle<FakeAssemblyEvent>
    {
        public Task<Result> HandleAsync(FakeAssemblyEvent @event, IChoreographyContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Success());
    }

    [Fact]
    public void AddProcessManagers_WhenServicesIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => SagaServiceCollectionExtensions.AddProcessManagers(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddProcessManagers_RegistersDefaultRepositoryAndOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();
        // Step executor is supplied by the consumer; provide a stub so the orchestrator can resolve.
        services.AddSingleton(Substitute.For<IProcessManagerStepExecutor>());

        // Act
        services.AddProcessManagers();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IProcessManagerRepository>().Should().BeOfType<InMemoryProcessManagerRepository>();
        sp.GetService<IProcessManagerOrchestrator>().Should().BeOfType<ProcessManagerOrchestrator>();
    }

    [Fact]
    public void AddProcessManagers_DoesNotOverridePreviousRepositoryRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var repo = Substitute.For<IProcessManagerRepository>();
        services.AddSingleton(repo);

        // Act
        services.AddProcessManagers();
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IProcessManagerRepository>().Should().BeSameAs(repo);
    }

    [Fact]
    public void AddEventChoreography_WhenServicesIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act
        var act = () => SagaServiceCollectionExtensions.AddEventChoreography(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEventChoreography_WithExplicitAssembly_RegistersHandlersFoundInIt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventChoreography(typeof(SagaServiceCollectionExtensionsTests).Assembly);
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IChoreographyRouter>().Should().NotBeNull();
        sp.GetService<IIntegrationEventPublisher>().Should().NotBeNull();
        var handlers = sp.GetServices<IHandle<FakeAssemblyEvent>>().ToList();
        handlers.Should().Contain(h => h.GetType() == typeof(AssemblyHandler));
    }

    [Fact]
    public void AddEventChoreography_WithoutAssembly_FallsBackToEntryOrCallingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act — must not throw even when no entry assembly is set in test runner.
        var act = () => services.AddEventChoreography();

        // Assert
        act.Should().NotThrow();
    }
}
