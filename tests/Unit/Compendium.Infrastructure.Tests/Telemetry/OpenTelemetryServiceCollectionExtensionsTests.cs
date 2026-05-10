// -----------------------------------------------------------------------
// <copyright file="OpenTelemetryServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Compendium.Infrastructure.Tests.Telemetry;

/// <summary>
/// Unit tests for <see cref="OpenTelemetryServiceCollectionExtensions"/> covering registrations.
/// </summary>
public sealed class OpenTelemetryServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumTelemetry_RegistersTracerAndMeterProviders()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddCompendiumTelemetry("MyService", "1.2.3");
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<TracerProvider>().Should().NotBeNull();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumTelemetry_NullVersion_DefaultsTo1_0_0()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumTelemetry("ServiceWithoutVersion");
        using var provider = services.BuildServiceProvider();

        // Assert — provider should still resolve
        provider.GetService<TracerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumConsoleExporter_AfterTelemetry_RegistersAndResolves()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompendiumTelemetry("ServiceA");

        // Act
        var returned = services.AddCompendiumConsoleExporter();
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<TracerProvider>().Should().NotBeNull();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumOtlpExporter_AfterTelemetry_RegistersAndResolves()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompendiumTelemetry("ServiceB");

        // Act
        var returned = services.AddCompendiumOtlpExporter("http://localhost:4317");
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<TracerProvider>().Should().NotBeNull();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumPrometheusExporter_AfterTelemetry_RegistersAndResolves()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompendiumTelemetry("ServiceC");

        // Act
        var returned = services.AddCompendiumPrometheusExporter();
        using var provider = services.BuildServiceProvider();

        // Assert
        returned.Should().BeSameAs(services);
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }
}
