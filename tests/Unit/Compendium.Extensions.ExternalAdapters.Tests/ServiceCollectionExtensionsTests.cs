// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing;
using Compendium.Abstractions.Email;
using Compendium.Abstractions.Identity;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Multitenancy;
using Compendium.Multitenancy.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Extensions.ExternalAdapters.Tests;

/// <summary>
/// Unit tests for unified external adapters ServiceCollection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumExternalAdapters_WithNoAdaptersEnabled_DoesNotRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            // All adapters disabled by default
        });

        var provider = services.BuildServiceProvider();

        // Assert - No adapter services registered
        provider.GetService<IIdentityUserService>().Should().BeNull();
        provider.GetService<IEmailService>().Should().BeNull();
        provider.GetService<IBillingService>().Should().BeNull();
        provider.GetService<ITenantContextAccessor>().Should().BeNull();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithZitadelEnabled_RegistersZitadelServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            options.EnableZitadel = true;
            options.Zitadel = new ZitadelOptions
            {
                Authority = "https://zitadel.example.com",
                ClientId = "client-123",
                ClientSecret = "secret"
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert
        // Note: Services require HTTP client to be fully resolvable, but the type should be registered
        services.Any(s => s.ServiceType == typeof(IIdentityUserService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithListmonkEnabled_RegistersListmonkServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            options.EnableListmonk = true;
            options.Listmonk = new ListmonkOptions
            {
                BaseUrl = "https://listmonk.example.com",
                Username = "admin",
                Password = "password"
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert
        services.Any(s => s.ServiceType == typeof(IEmailService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(INewsletterService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithLemonSqueezyEnabled_RegistersLemonSqueezyServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            options.EnableLemonSqueezy = true;
            options.LemonSqueezy = new LemonSqueezyOptions
            {
                ApiKey = "sk_test_123",
                StoreId = "store-456"
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert
        services.Any(s => s.ServiceType == typeof(IBillingService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(ISubscriptionService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(ILicenseService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithMultitenancyEnabled_RegistersMultitenancyServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            options.EnableMultitenancy = true;
            options.Multitenancy = new MultitenancyOptions
            {
                RequireTenant = true,
                DefaultTenantId = "default-tenant"
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITenantContextAccessor>().Should().NotBeNull();
        provider.GetService<ITenantContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithAllAdaptersEnabled_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCompendiumExternalAdapters(options =>
        {
            options.EnableZitadel = true;
            options.Zitadel = new ZitadelOptions
            {
                Authority = "https://zitadel.example.com"
            };

            options.EnableListmonk = true;
            options.Listmonk = new ListmonkOptions
            {
                BaseUrl = "https://listmonk.example.com",
                Username = "admin",
                Password = "pass"
            };

            options.EnableLemonSqueezy = true;
            options.LemonSqueezy = new LemonSqueezyOptions
            {
                ApiKey = "key",
                StoreId = "store"
            };

            options.EnableMultitenancy = true;
            options.Multitenancy = new MultitenancyOptions
            {
                RequireTenant = false
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert - Multitenancy is fully resolvable
        provider.GetService<ITenantContextAccessor>().Should().NotBeNull();

        // Assert - Other services are registered (types present in service collection)
        services.Any(s => s.ServiceType == typeof(IIdentityUserService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IEmailService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IBillingService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumExternalAdapters(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumExternalAdapters((Action<ExternalAdaptersOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_FromConfiguration_RegistersEnabledServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["Compendium:ExternalAdapters:EnableMultitenancy"] = "true",
            ["Compendium:ExternalAdapters:Multitenancy:RequireTenant"] = "false",
            ["Compendium:ExternalAdapters:Multitenancy:DefaultTenantId"] = "test-tenant"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddCompendiumExternalAdapters(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITenantContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_FromConfiguration_WithCustomSectionPath()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["CustomPath:EnableMultitenancy"] = "true",
            ["CustomPath:Multitenancy:RequireTenant"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddCompendiumExternalAdapters(configuration, "CustomPath");
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ITenantContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumExternalAdapters_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCompendiumExternalAdapters(_ => { });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCompendiumZitadel_FromConfiguration_RegistersZitadelServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["Zitadel:Authority"] = "https://zitadel.example.com",
            ["Zitadel:ClientId"] = "client-123"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddCompendiumZitadel(configuration.GetSection("Zitadel"));

        // Assert
        services.Any(s => s.ServiceType == typeof(IIdentityUserService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumListmonk_FromConfiguration_RegistersListmonkServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["Listmonk:BaseUrl"] = "https://listmonk.example.com",
            ["Listmonk:Username"] = "admin",
            ["Listmonk:Password"] = "pass"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddCompendiumListmonk(configuration.GetSection("Listmonk"));

        // Assert
        services.Any(s => s.ServiceType == typeof(IEmailService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumLemonSqueezy_FromConfiguration_RegistersLemonSqueezyServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["LemonSqueezy:ApiKey"] = "sk_test_123",
            ["LemonSqueezy:StoreId"] = "store-456"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddCompendiumLemonSqueezy(configuration.GetSection("LemonSqueezy"));

        // Assert
        services.Any(s => s.ServiceType == typeof(IBillingService)).Should().BeTrue();
    }

    [Fact]
    public void AddCompendiumZitadel_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumZitadel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumListmonk_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumListmonk(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumLemonSqueezy_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumLemonSqueezy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
