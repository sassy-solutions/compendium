// -----------------------------------------------------------------------
// <copyright file="PostgreSqlSagaServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.ProcessManagers;
using Compendium.Adapters.PostgreSQL.Configuration;
using Compendium.Adapters.PostgreSQL.Sagas;
using Compendium.Adapters.PostgreSQL.Sagas.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Compendium.Adapters.PostgreSQL.Tests.Sagas.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="PostgreSqlSagaServiceCollectionExtensions"/>.
/// </summary>
public class PostgreSqlSagaServiceCollectionExtensionsTests
{
    private const string SampleConnectionString = "Host=localhost;Database=db;Username=u;Password=p";

    [Fact]
    public void AddPostgreSqlProcessManagerRepository_WhenServicesNull_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddPostgreSqlProcessManagerRepository();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPostgreSqlProcessManagerRepository_WithConfigurationAction_RegistersRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddPostgreSqlProcessManagerRepository(
            opts => opts.ConnectionString = SampleConnectionString);

        // Assert
        returned.Should().BeSameAs(services);
        services.Should().Contain(d =>
            d.ServiceType == typeof(IProcessManagerRepository) &&
            d.ImplementationType == typeof(PostgresProcessManagerRepository));
    }

    [Fact]
    public void AddPostgreSqlProcessManagerRepository_WithoutConfigurationAction_StillRegistersRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProcessManagerRepository(configurationAction: null);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IProcessManagerRepository));
    }

    [Fact]
    public void AddPostgreSqlProcessManagerRepository_RemovesPreviousRegistrationOfRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Pretend a previous adapter (in-memory) registered the repo first.
        var stub = Substitute.For<IProcessManagerRepository>();
        services.AddSingleton<IProcessManagerRepository>(stub);

        // Act
        services.AddPostgreSqlProcessManagerRepository(opts => opts.ConnectionString = SampleConnectionString);

        // Assert — only the PostgreSQL implementation should remain registered.
        services.Where(d => d.ServiceType == typeof(IProcessManagerRepository))
            .Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(PostgresProcessManagerRepository));
    }

    [Fact]
    public void AddPostgreSqlProcessManagerRepository_WithConfigurationAction_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddPostgreSqlProcessManagerRepository(opts => opts.ConnectionString = SampleConnectionString);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;
        options.ConnectionString.Should().Be(SampleConnectionString);
    }
}
