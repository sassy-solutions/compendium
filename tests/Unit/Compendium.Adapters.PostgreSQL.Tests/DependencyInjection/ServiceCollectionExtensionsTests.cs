// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.EventSourcing;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.DependencyInjection;
using Compendium.Adapters.PostgreSQL.EventStore;
using Compendium.Adapters.PostgreSQL.Projections;
using Compendium.Core.EventSourcing;
using Compendium.Infrastructure.EventSourcing;
using Compendium.Infrastructure.Projections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.PostgreSQL.Tests.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private const string SampleConnectionString = "Host=localhost;Database=db;Username=u;Password=p";

    [Fact]
    public void AddPostgreSqlEventStore_WithConfigurationAction_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlEventStore(opts => opts.ConnectionString = SampleConnectionString);

        // Assert — the returned collection is the same one (chainable)
        returned.Should().BeSameAs(services);

        // Required services are present in registrations
        services.Should().Contain(d => d.ServiceType == typeof(IEventTypeRegistry));
        services.Should().Contain(d => d.ServiceType == typeof(IEventDeserializer));
        services.Should().Contain(d => d.ServiceType == typeof(PostgreSqlEventStore));
        services.Should().Contain(d => d.ServiceType == typeof(IEventStore));
    }

    [Fact]
    public void AddPostgreSqlEventStore_WithConnectionString_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlEventStore(SampleConnectionString);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
        options.ConnectionString.Should().Be(SampleConnectionString);
    }

    [Fact]
    public void AddPostgreSqlEventStore_WithNoConfigurationAction_DoesNotConfigure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — call without explicit configuration
        services.AddPostgreSqlEventStore(configurationAction: null);

        // Assert — services were still registered, but options stayed at defaults
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
        options.ConnectionString.Should().BeEmpty();
    }

    [Fact]
    public void AddPostgreSqlEventStore_RegistersBothConcreteAndInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlEventStore(opts => opts.ConnectionString = SampleConnectionString);

        // Assert — IEventStore should resolve to the same instance as PostgreSqlEventStore
        var provider = services.BuildServiceProvider();
        var concrete = provider.GetRequiredService<PostgreSqlEventStore>();
        var asInterface = provider.GetRequiredService<IEventStore>();
        asInterface.Should().BeSameAs(concrete);
    }

    [Fact]
    public void AddPostgreSqlProjectionCheckpointStore_WithConfigurationAction_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlProjectionCheckpointStore(
            opts => opts.ConnectionString = SampleConnectionString);

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IProjectionCheckpointStore));
    }

    [Fact]
    public void AddPostgreSqlProjectionCheckpointStore_WithConnectionString_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProjectionCheckpointStore(SampleConnectionString);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
        options.ConnectionString.Should().Be(SampleConnectionString);
    }

    [Fact]
    public void AddPostgreSqlProjectionCheckpointStore_WithNoConfigurationAction_RegistersWithoutConfiguring()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProjectionCheckpointStore(configurationAction: null);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IProjectionCheckpointStore));
    }

    [Fact]
    public void AddPostgreSqlProjectionStore_WithConfigurationAction_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlProjectionStore(
            opts => opts.ConnectionString = SampleConnectionString);

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IProjectionStore));
    }

    [Fact]
    public void AddPostgreSqlProjectionStore_WithConnectionString_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProjectionStore(SampleConnectionString);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
        options.ConnectionString.Should().Be(SampleConnectionString);
    }

    [Fact]
    public void AddPostgreSqlProjectionStore_WithNoConfigurationAction_RegistersWithoutConfiguring()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProjectionStore(configurationAction: null);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IProjectionStore));
    }

    [Fact]
    public void AddPostgreSqlStreamingEventStore_WithConfigurationAction_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlStreamingEventStore(opts => opts.ConnectionString = SampleConnectionString);

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IStreamingEventStore));
    }

    [Fact]
    public void AddPostgreSqlStreamingEventStore_WithConnectionString_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlStreamingEventStore(SampleConnectionString);

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IStreamingEventStore));
    }

    [Fact]
    public void AddPostgreSqlStreamingEventStore_WithNoConfigurationAction_StillRegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlStreamingEventStore(configurationAction: null);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IStreamingEventStore));
    }
}
