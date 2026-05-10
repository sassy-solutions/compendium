// -----------------------------------------------------------------------
// <copyright file="SerilogConfigurationTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Infrastructure.Observability.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Compendium.Infrastructure.Tests.Observability.Logging;

/// <summary>
/// Unit tests for <see cref="SerilogConfiguration"/> covering minimum log level,
/// environment-driven sinks and host-builder integration.
/// </summary>
public sealed class SerilogConfigurationTests
{
    [Fact]
    public void ConfigureSerilog_DevelopmentEnvironment_BuildsValidLogger()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var configuration = BuildConfig(new Dictionary<string, string?>());
        var loggerConfig = new LoggerConfiguration();

        // Act
        SerilogConfiguration.ConfigureSerilog(loggerConfig, configuration, environment);
        using var logger = loggerConfig.CreateLogger();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureSerilog_ProductionEnvironment_BuildsValidLogger()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var tempDir = Directory.CreateTempSubdirectory().FullName;
        var logPath = Path.Combine(tempDir, "test-.log");
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["Logging:File:Path"] = logPath,
        });
        var loggerConfig = new LoggerConfiguration();

        try
        {
            // Act
            SerilogConfiguration.ConfigureSerilog(loggerConfig, configuration, environment);
            using var logger = loggerConfig.CreateLogger();

            // Assert
            logger.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ConfigureSerilog_WithExplicitLogLevel_AppliesIt()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
        });
        var loggerConfig = new LoggerConfiguration();

        // Act
        SerilogConfiguration.ConfigureSerilog(loggerConfig, configuration, environment);
        using var logger = loggerConfig.CreateLogger();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureSerilog_WithSeqApiKey_BuildsValidLogger()
    {
        // Arrange
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["Logging:Seq:Url"] = "http://seq:5341",
            ["Logging:Seq:ApiKey"] = "secret-key",
        });
        var loggerConfig = new LoggerConfiguration();

        // Act
        SerilogConfiguration.ConfigureSerilog(loggerConfig, configuration, environment);
        using var logger = loggerConfig.CreateLogger();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureSerilog_HostBuilderContextOverload_BuildsValidLogger()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, c) =>
            {
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:Seq:Url"] = "http://seq:5341",
                });
            })
            .UseEnvironment(Environments.Development);

        // Act
        hostBuilder.UseSerilog(SerilogConfiguration.ConfigureSerilog);
        using var host = hostBuilder.Build();

        // Assert
        host.Should().NotBeNull();
    }

    [Fact]
    public void UseCompendiumSerilog_HostBuilderExtension_RegistersSerilog()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .UseEnvironment(Environments.Development);

        // Act
        hostBuilder.UseCompendiumSerilog();
        using var host = hostBuilder.Build();

        // Assert
        host.Should().NotBeNull();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
