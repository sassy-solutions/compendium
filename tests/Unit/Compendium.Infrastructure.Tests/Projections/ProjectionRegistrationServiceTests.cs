// -----------------------------------------------------------------------
// <copyright file="ProjectionRegistrationServiceTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Infrastructure.Projections;
using Microsoft.Extensions.Hosting;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Unit tests for the internal <c>ProjectionRegistrationService</c> hosted service that registers
/// projections at startup. Reflection is used to instantiate it because it lives in an internal
/// namespace, but its surface is small (StartAsync, StopAsync) and is exercised here.
/// </summary>
public sealed class ProjectionRegistrationServiceTests
{
    private static readonly Assembly InfraAssembly = typeof(ProjectionOptions).Assembly;

    [Fact]
    public async Task StartAsync_NoProjections_DoesNothing()
    {
        // Arrange
        var manager = Substitute.For<Compendium.Infrastructure.Projections.IProjectionManager>();
        var liveProcessor = Substitute.For<ILiveProjectionProcessor>();
        var sut = CreateService(manager, liveProcessor);

        // Act
        await ((IHostedService)sut).StartAsync(CancellationToken.None);
        await ((IHostedService)sut).StopAsync(CancellationToken.None);

        // Assert
        manager.ReceivedCalls().Where(c => c.GetMethodInfo().Name == "RegisterProjection").Should().BeEmpty();
        liveProcessor.ReceivedCalls().Where(c => c.GetMethodInfo().Name == "RegisterProjection").Should().BeEmpty();
    }

    [Fact]
    public async Task StopAsync_AlwaysReturnsCompletedTask()
    {
        // Arrange
        var manager = Substitute.For<Compendium.Infrastructure.Projections.IProjectionManager>();
        var liveProcessor = Substitute.For<ILiveProjectionProcessor>();
        var sut = CreateService(manager, liveProcessor);

        // Act
        Func<Task> act = () => ((IHostedService)sut).StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static object CreateService(
        Compendium.Infrastructure.Projections.IProjectionManager manager,
        ILiveProjectionProcessor liveProcessor)
    {
        var serviceType = InfraAssembly.GetType("Compendium.Infrastructure.Projections.ProjectionRegistrationService")!;
        var optionsType = InfraAssembly.GetType("Compendium.Infrastructure.Projections.ProjectionRegistrationOptions")!;
        var optionsValue = Activator.CreateInstance(optionsType)!;
        var iOptionsType = typeof(Microsoft.Extensions.Options.Options)
            .GetMethod(nameof(Microsoft.Extensions.Options.Options.Create))!
            .MakeGenericMethod(optionsType);
        var iOptions = iOptionsType.Invoke(null, new[] { optionsValue })!;

        var ctor = serviceType.GetConstructors().Single();
        return ctor.Invoke(new[] { manager, liveProcessor, iOptions });
    }
}
