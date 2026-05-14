// -----------------------------------------------------------------------
// <copyright file="ProjectionsServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Projections;
using Compendium.Infrastructure.Projections.Examples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Infrastructure.Tests.Projections;

/// <summary>
/// Unit tests for the projections-namespace <c>ServiceCollectionExtensions</c>.
/// </summary>
public sealed class ProjectionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProjections_DefaultOverload_RegistersManagerAndProcessor()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        var returned = services.AddProjections();
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetRequiredService<Compendium.Infrastructure.Projections.IProjectionManager>().Should().BeOfType<EnhancedProjectionManager>();
        provider.GetRequiredService<ILiveProjectionProcessor>().Should().BeOfType<LiveProjectionProcessor>();
        provider.GetServices<IHostedService>().Should().Contain(s => s is LiveProjectionProcessor);
    }

    [Fact]
    public void AddProjections_WithConfigureOptions_AppliesAction()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddProjections(opt => opt.RebuildBatchSize = 17);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ProjectionOptions>>().Value;

        // Assert
        options.RebuildBatchSize.Should().Be(17);
    }

    [Fact]
    public void AddPostgreSqlProjections_ReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddPostgreSqlProjections();

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddProjection_RegistersTypeAsSingleton()
    {
        // Arrange
        var services = CreateBaseServices();
        services.AddProjections();

        // Act
        services.AddProjection<OrderSummaryProjection>();
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<OrderSummaryProjection>().Should().NotBeNull();
        var first = provider.GetService<OrderSummaryProjection>();
        var second = provider.GetService<OrderSummaryProjection>();
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddProjection_DoesNotOverrideExistingRegistration()
    {
        // Arrange
        var services = CreateBaseServices();
        var customInstance = new OrderSummaryProjection();
        services.AddSingleton(customInstance);

        // Act
        services.AddProjection<OrderSummaryProjection>();
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<OrderSummaryProjection>().Should().BeSameAs(customInstance);
    }

    [Fact]
    public void AddProjection_AddsTypeToRegistrationOptions()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddProjection<OrderSummaryProjection>();
        services.AddProjection<CustomerStatsProjection>();
        using var provider = services.BuildServiceProvider();

        // Resolve internal options via reflection — type is internal but accessible at runtime via OptionsManager
        var optionsType = typeof(ServiceCollectionExtensions).Assembly
            .GetType("Compendium.Infrastructure.Projections.ProjectionRegistrationOptions");
        optionsType.Should().NotBeNull();
        var optionsResolverGeneric = typeof(IOptions<>).MakeGenericType(optionsType!);
        var optionsResolver = provider.GetRequiredService(optionsResolverGeneric);
        var optionsValue = optionsResolverGeneric.GetProperty("Value")!.GetValue(optionsResolver);
        var typesProperty = optionsType!.GetProperty("ProjectionTypes")!;
        var types = (List<Type>)typesProperty.GetValue(optionsValue)!;

        // Assert
        types.Should().Contain(typeof(OrderSummaryProjection));
        types.Should().Contain(typeof(CustomerStatsProjection));
    }

    private static IServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IStreamingEventStore>(_ => Substitute.For<IStreamingEventStore>());
        services.AddSingleton<IProjectionStore>(_ => Substitute.For<IProjectionStore>());
        return services;
    }
}
