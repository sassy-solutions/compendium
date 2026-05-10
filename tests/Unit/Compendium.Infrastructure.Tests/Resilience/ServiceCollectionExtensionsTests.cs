// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace Compendium.Infrastructure.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> in the Resilience namespace.
/// </summary>
public sealed class ResilienceServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPollyResilience_DefaultOverload_RegistersPipelineFactoryAndPipeline()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        var returned = services.AddPollyResilience();
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<PollyResiliencePipelineFactory>().Should().NotBeNull();
        provider.GetService<ResilienceTelemetryListener>().Should().NotBeNull();
        provider.GetService<ResiliencePipeline>().Should().NotBeNull();
    }

    [Fact]
    public void AddPollyResilience_WithConfigurePostgreSqlAction_AppliesConfiguration()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configured = false;

        // Act
        services.AddPollyResilience(configurePostgreSql: _ => configured = true);
        using var provider = services.BuildServiceProvider();
        // Force resolution to invoke factory delegate
        _ = provider.GetService<ResiliencePipeline>();

        // Assert
        configured.Should().BeTrue();
    }

    [Fact]
    public void AddPollyResilience_WithCustomOptionsOverload_RegistersFactoryAndOptions()
    {
        // Arrange
        var services = CreateServiceCollection();
        var pgOptions = PollyResilienceOptions.PostgreSqlDefaults();
        var redisOptions = PollyResilienceOptions.RedisDefaults();

        // Act
        var returned = services.AddPollyResilience(pgOptions, redisOptions);
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<PollyResiliencePipelineFactory>().Should().NotBeNull();
        provider.GetService<ResilienceTelemetryListener>().Should().NotBeNull();
        provider.GetServices<PollyResilienceOptions>().Should().Contain(new[] { pgOptions, redisOptions });
    }

    private static IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }
}
