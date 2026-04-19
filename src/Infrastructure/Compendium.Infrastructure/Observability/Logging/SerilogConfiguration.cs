// -----------------------------------------------------------------------
// <copyright file="SerilogConfiguration.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Compendium.Infrastructure.Observability.Logging;

/// <summary>
/// Provides configuration for Serilog structured logging with support for:
/// - Console output (development)
/// - File output with rotation (production)
/// - Seq sink for centralized logging
/// - Thread, environment, and process enrichers
/// - Correlation ID enrichment
/// - Event sourcing context enrichment
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog for the application with recommended settings.
    /// </summary>
    /// <param name="hostBuilderContext">The host builder context containing configuration and environment.</param>
    /// <param name="loggerConfiguration">The logger configuration to apply settings to.</param>
    public static void ConfigureSerilog(HostBuilderContext hostBuilderContext, LoggerConfiguration loggerConfiguration)
    {
        var configuration = hostBuilderContext.Configuration;
        var environment = hostBuilderContext.HostingEnvironment;

        ConfigureSerilog(loggerConfiguration, configuration, environment);
    }

    /// <summary>
    /// Configures Serilog with enrichers, sinks, and formatting.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to apply settings to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    public static void ConfigureSerilog(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var minimumLevel = GetMinimumLogLevel(configuration, environment);
        var seqUrl = configuration["Logging:Seq:Url"] ?? "http://localhost:5341";
        var seqApiKey = configuration["Logging:Seq:ApiKey"];

        loggerConfiguration
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Compendium")
            .Enrich.WithProperty("Environment", environment.EnvironmentName);

        // Console sink (always enabled)
        if (environment.IsDevelopment())
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
        else
        {
            // Use compact JSON formatting for production console
            loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
        }

        // File sink (production and staging)
        if (!environment.IsDevelopment())
        {
            var logPath = configuration["Logging:File:Path"] ?? "logs/compendium-.log";
            loggerConfiguration.WriteTo.File(
                new CompactJsonFormatter(),
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
        }

        // Seq sink (if configured)
        if (!string.IsNullOrEmpty(seqUrl))
        {
            var seqConfig = loggerConfiguration.WriteTo.Seq(
                serverUrl: seqUrl,
                restrictedToMinimumLevel: LogEventLevel.Verbose);

            if (!string.IsNullOrEmpty(seqApiKey))
            {
                seqConfig = loggerConfiguration.WriteTo.Seq(
                    serverUrl: seqUrl,
                    apiKey: seqApiKey,
                    restrictedToMinimumLevel: LogEventLevel.Verbose);
            }
        }

        // Read from configuration
        loggerConfiguration.ReadFrom.Configuration(configuration);
    }

    /// <summary>
    /// Gets the minimum log level based on configuration and environment.
    /// </summary>
    private static LogEventLevel GetMinimumLogLevel(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredLevel = configuration["Logging:LogLevel:Default"];

        if (!string.IsNullOrEmpty(configuredLevel) &&
            Enum.TryParse<LogEventLevel>(configuredLevel, ignoreCase: true, out var level))
        {
            return level;
        }

        // Default based on environment
        return environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;
    }
}

/// <summary>
/// Extension methods for configuring Serilog on IHostBuilder.
/// </summary>
public static class SerilogHostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider with recommended settings.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseCompendiumSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog(SerilogConfiguration.ConfigureSerilog);
    }
}
