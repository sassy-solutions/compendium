// -----------------------------------------------------------------------
// <copyright file="OpenTelemetryServiceCollectionExtensions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Compendium.Infrastructure.Telemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry in Compendium framework.
/// Provides fluent API for adding traces, metrics, and exporters.
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry instrumentation for Compendium framework with traces and metrics.
    /// Configures ActivitySource and Meter for EventStore, CQRS, and Projections.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The service name for telemetry (e.g., "MyApp").</param>
    /// <param name="serviceVersion">The service version (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumTelemetry(
        this IServiceCollection services,
        string serviceName,
        string? serviceVersion = null)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion ?? "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddSource(CompendiumTelemetry.SourceName)
                .SetSampler(new AlwaysOnSampler())) // Adjust for production (e.g., ParentBasedSampler with 0.1 ratio)
            .WithMetrics(metrics => metrics
                .AddMeter(CompendiumTelemetry.SourceName)
                .AddRuntimeInstrumentation());

        return services;
    }

    /// <summary>
    /// Adds console exporter for development/debugging.
    /// Writes traces and metrics to console output.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumConsoleExporter(this IServiceCollection services)
    {
        services.ConfigureOpenTelemetryTracerProvider(tracing =>
            tracing.AddConsoleExporter());

        services.ConfigureOpenTelemetryMeterProvider(metrics =>
            metrics.AddConsoleExporter());

        return services;
    }

    /// <summary>
    /// Adds OTLP (OpenTelemetry Protocol) exporter for production use.
    /// Compatible with Jaeger, Grafana Tempo, Honeycomb, and other OTLP-compatible backends.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">OTLP endpoint (e.g., "http://localhost:4317").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompendiumOtlpExporter(
        this IServiceCollection services,
        string endpoint)
    {
        services.ConfigureOpenTelemetryTracerProvider(tracing =>
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
            }));

        services.ConfigureOpenTelemetryMeterProvider(metrics =>
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
            }));

        return services;
    }

    /// <summary>
    /// Adds Prometheus exporter for metrics (ASP.NET Core only).
    /// Exposes metrics at /metrics endpoint for Prometheus scraping.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Requires ASP.NET Core application. Metrics exposed at /metrics by default.
    /// Configure Prometheus to scrape this endpoint.
    /// </remarks>
    public static IServiceCollection AddCompendiumPrometheusExporter(this IServiceCollection services)
    {
        // Note: Prometheus exporter is configured differently for ASP.NET Core
        // Use: app.UseOpenTelemetryPrometheusScrapingEndpoint() in Program.cs
        services.ConfigureOpenTelemetryMeterProvider(metrics =>
            metrics.AddPrometheusExporter());

        return services;
    }
}
