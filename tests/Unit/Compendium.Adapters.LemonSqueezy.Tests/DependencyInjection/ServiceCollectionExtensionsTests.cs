// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing;
using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.LemonSqueezy.Tests.DependencyInjection;

/// <summary>
/// Unit tests for LemonSqueezy ServiceCollection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLemonSqueezy_WithAction_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLemonSqueezy(options =>
        {
            options.ApiKey = "sk_test_12345";
            options.StoreId = "store-123";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<LemonSqueezyOptions>>().Should().NotBeNull();
        provider.GetService<IBillingService>().Should().NotBeNull();
        provider.GetService<ISubscriptionService>().Should().NotBeNull();
        provider.GetService<ILicenseService>().Should().NotBeNull();
        provider.GetService<IPaymentWebhookHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddLemonSqueezy_WithAction_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLemonSqueezy(opt =>
        {
            opt.ApiKey = "sk_test_abc";
            opt.StoreId = "store-456";
            opt.WebhookSigningSecret = "whsec_xyz";
            opt.TimeoutSeconds = 45;
            opt.MaxRetries = 2;
            opt.TestMode = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LemonSqueezyOptions>>().Value;

        // Assert
        options.ApiKey.Should().Be("sk_test_abc");
        options.StoreId.Should().Be("store-456");
        options.WebhookSigningSecret.Should().Be("whsec_xyz");
        options.TimeoutSeconds.Should().Be(45);
        options.MaxRetries.Should().Be(2);
        options.TestMode.Should().BeTrue();
    }

    [Fact]
    public void AddLemonSqueezy_WithConfiguration_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["LemonSqueezy:ApiKey"] = "sk_test_config123",
            ["LemonSqueezy:StoreId"] = "store-config456",
            ["LemonSqueezy:WebhookSigningSecret"] = "whsec_config789",
            ["LemonSqueezy:TimeoutSeconds"] = "60",
            ["LemonSqueezy:MaxRetries"] = "5",
            ["LemonSqueezy:TestMode"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddLemonSqueezy(configuration.GetSection("LemonSqueezy"));
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<LemonSqueezyOptions>>().Should().NotBeNull();
        provider.GetService<IBillingService>().Should().NotBeNull();
        provider.GetService<ISubscriptionService>().Should().NotBeNull();
        provider.GetService<ILicenseService>().Should().NotBeNull();
        provider.GetService<IPaymentWebhookHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddLemonSqueezy_WithConfiguration_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["LemonSqueezy:ApiKey"] = "sk_test_bound",
            ["LemonSqueezy:StoreId"] = "store-bound",
            ["LemonSqueezy:TimeoutSeconds"] = "90"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddLemonSqueezy(configuration.GetSection("LemonSqueezy"));
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LemonSqueezyOptions>>().Value;

        // Assert
        options.ApiKey.Should().Be("sk_test_bound");
        options.StoreId.Should().Be("store-bound");
        options.TimeoutSeconds.Should().Be(90);
    }

    [Fact]
    public void AddLemonSqueezy_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddLemonSqueezy(opt => opt.ApiKey = "test");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLemonSqueezy_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddLemonSqueezy((Action<LemonSqueezyOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLemonSqueezy_WithNullConfigurationSection_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddLemonSqueezy((IConfigurationSection)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLemonSqueezy_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLemonSqueezy(opt =>
        {
            opt.ApiKey = "sk_test_http";
            opt.StoreId = "store-http";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddLemonSqueezy_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddLemonSqueezy(opt =>
        {
            opt.ApiKey = "sk_test_chain";
            opt.StoreId = "store-chain";
        });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddLemonSqueezy_RegistersServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLemonSqueezy(opt =>
        {
            opt.ApiKey = "sk_test_scope";
            opt.StoreId = "store-scope";
        });

        var provider = services.BuildServiceProvider();

        // Act - Create two scopes and get services
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var billing1 = scope1.ServiceProvider.GetRequiredService<IBillingService>();
        var billing2 = scope2.ServiceProvider.GetRequiredService<IBillingService>();

        // Assert - Different scopes should have different instances
        billing1.Should().NotBeSameAs(billing2);
    }
}
