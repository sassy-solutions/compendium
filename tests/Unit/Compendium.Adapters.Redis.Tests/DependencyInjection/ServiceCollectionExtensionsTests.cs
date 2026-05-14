// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Redis.Configuration;
using Compendium.Adapters.Redis.DependencyInjection;
using Compendium.Adapters.Redis.Idempotency;
using Compendium.Adapters.Redis.Projections;
using Compendium.Application.Idempotency;
using Compendium.Infrastructure.EventSourcing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace Compendium.Adapters.Redis.Tests.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
///
/// These tests focus on the registration shape (service descriptors and lifetimes) without
/// activating the singleton Redis connection multiplexer factory, which requires a real Redis.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    // ---------- AddRedis(action) ----------

    [Fact]
    public void AddRedis_WithoutAction_RegistersConnectionMultiplexerSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedis();

        // Assert
        var descriptor = services.Should()
            .ContainSingle(d => d.ServiceType == typeof(IConnectionMultiplexer))
            .Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddRedis_WithAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedis(o =>
        {
            o.ConnectionString = "remote:6379";
            o.DefaultDatabase = 3;
            o.KeyPrefix = "myapp";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("remote:6379");
        options.DefaultDatabase.Should().Be(3);
        options.KeyPrefix.Should().Be("myapp");
    }

    [Fact]
    public void AddRedis_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddRedis();

        // Assert
        returned.Should().BeSameAs(services);
    }

    // ---------- AddRedis(connectionString) ----------

    [Fact]
    public void AddRedis_WithConnectionString_StoresIt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedis("test-host:6380");
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("test-host:6380");
    }

    // ---------- AddRedisIdempotencyStore ----------

    [Fact]
    public void AddRedisIdempotencyStore_RegistersScopedStoreAndService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisIdempotencyStore();

        // Assert
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IIdempotencyStore) &&
            d.ImplementationType == typeof(RedisIdempotencyStore) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IIdempotencyService) &&
            d.ImplementationType == typeof(IdempotencyService) &&
            d.Lifetime == ServiceLifetime.Scoped);

        // Underlying multiplexer is also registered.
        services.Should().Contain(d => d.ServiceType == typeof(IConnectionMultiplexer));
    }

    [Fact]
    public void AddRedisIdempotencyStore_WithAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisIdempotencyStore(o => o.ConnectionString = "abc:1234");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("abc:1234");
    }

    [Fact]
    public void AddRedisIdempotencyStore_WithConnectionString_AppliesIt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisIdempotencyStore("xyz:1111");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("xyz:1111");
    }

    [Fact]
    public void AddRedisIdempotencyStore_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddRedisIdempotencyStore();

        // Assert
        returned.Should().BeSameAs(services);
    }

    // ---------- AddRedisProjectionCheckpointStore ----------

    [Fact]
    public void AddRedisProjectionCheckpointStore_RegistersSingletonStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisProjectionCheckpointStore();

        // Assert
        var descriptor = services.Should()
            .ContainSingle(d => d.ServiceType == typeof(IProjectionCheckpointStore))
            .Subject;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_WithAction_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisProjectionCheckpointStore(o => o.ConnectionString = "host:6379");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("host:6379");
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_WithConnectionString_AppliesIt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisProjectionCheckpointStore("zzz:9999");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        // Assert
        options.ConnectionString.Should().Be("zzz:9999");
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_WithExpiration_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRedisProjectionCheckpointStore(
            configurationAction: null,
            defaultExpiration: TimeSpan.FromHours(2));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_WithConnectionStringAndExpiration_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRedisProjectionCheckpointStore("host:6379", TimeSpan.FromHours(3));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddRedisProjectionCheckpointStore();

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_FactoryActivatesStoreWithSubstitutedMultiplexer()
    {
        // Arrange
        var services = new ServiceCollection();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        // Pre-register the substitute multiplexer FIRST so the factory's
        // GetRequiredService<IConnectionMultiplexer>() resolves it instead of attempting
        // a real connection. ServiceCollection resolves the LAST registration for a service type.
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddRedisProjectionCheckpointStore(o => o.ConnectionString = "x:1");

        // Strip the AddRedis-registered multiplexer factory so our substitute wins.
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(IConnectionMultiplexer) &&
                services[i].ImplementationInstance != multiplexer)
            {
                services.RemoveAt(i);
            }
        }

        var provider = services.BuildServiceProvider();

        // Act
        var store = provider.GetRequiredService<IProjectionCheckpointStore>();

        // Assert
        store.Should().BeOfType<RedisProjectionCheckpointStore>();
    }

    [Fact]
    public void AddRedisProjectionCheckpointStore_FactoryHonorsCustomExpiration()
    {
        // Arrange
        var services = new ServiceCollection();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddRedisProjectionCheckpointStore(
            o => o.ConnectionString = "x:1",
            TimeSpan.FromMinutes(15));

        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(IConnectionMultiplexer) &&
                services[i].ImplementationInstance != multiplexer)
            {
                services.RemoveAt(i);
            }
        }

        var provider = services.BuildServiceProvider();

        // Act
        var store = provider.GetRequiredService<IProjectionCheckpointStore>();

        // Assert (no exception, factory ran with substituted multiplexer)
        store.Should().BeOfType<RedisProjectionCheckpointStore>();
    }
}
