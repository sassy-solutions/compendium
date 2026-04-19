// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Billing;
using Compendium.Adapters.Stripe.Configuration;
using Compendium.Adapters.Stripe.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Stripe.Tests.DependencyInjection;

/// <summary>
/// Unit tests for Stripe <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStripeAdapter_WithAction_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddStripeAdapter(options =>
        {
            options.SecretKey = "sk_test_12345";
        });

        var provider = services.BuildServiceProvider();

        provider.GetService<IOptions<StripeOptions>>().Should().NotBeNull();
        provider.GetService<IBillingService>().Should().NotBeNull();
        provider.GetService<ISubscriptionService>().Should().NotBeNull();
        provider.GetService<IPaymentWebhookHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddStripeAdapter_WithAction_DoesNotRegisterLicenseService()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddStripeAdapter(options =>
        {
            options.SecretKey = "sk_test_no_license";
        });

        var provider = services.BuildServiceProvider();

        // Stripe has no native license-key product equivalent — ensure we don't register one.
        provider.GetService<ILicenseService>().Should().BeNull();
    }

    [Fact]
    public void AddStripeAdapter_WithAction_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddStripeAdapter(opt =>
        {
            opt.SecretKey = "sk_test_abc";
            opt.PublishableKey = "pk_test_xyz";
            opt.WebhookSigningSecret = "whsec_signing";
            opt.ApiVersion = "2024-06-20";
            opt.TestMode = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<StripeOptions>>().Value;

        options.SecretKey.Should().Be("sk_test_abc");
        options.PublishableKey.Should().Be("pk_test_xyz");
        options.WebhookSigningSecret.Should().Be("whsec_signing");
        options.ApiVersion.Should().Be("2024-06-20");
        options.TestMode.Should().BeTrue();
    }

    [Fact]
    public void AddStripeAdapter_WithConfiguration_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        var configData = new Dictionary<string, string?>
        {
            ["Stripe:SecretKey"] = "sk_test_config",
            ["Stripe:WebhookSigningSecret"] = "whsec_config",
            ["Stripe:TestMode"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddStripeAdapter(configuration.GetSection("Stripe"));
        var provider = services.BuildServiceProvider();

        provider.GetService<IOptions<StripeOptions>>().Should().NotBeNull();
        provider.GetService<IBillingService>().Should().NotBeNull();
        provider.GetService<ISubscriptionService>().Should().NotBeNull();
        provider.GetService<IPaymentWebhookHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddStripeAdapter_WithConfiguration_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        var configData = new Dictionary<string, string?>
        {
            ["Stripe:SecretKey"] = "sk_test_bound",
            ["Stripe:WebhookSigningSecret"] = "whsec_bound",
            ["Stripe:TestMode"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddStripeAdapter(configuration.GetSection("Stripe"));
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<StripeOptions>>().Value;

        options.SecretKey.Should().Be("sk_test_bound");
        options.WebhookSigningSecret.Should().Be("whsec_bound");
        options.TestMode.Should().BeTrue();
    }

    [Fact]
    public void AddStripeAdapter_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddStripeAdapter(opt => opt.SecretKey = "sk_test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStripeAdapter_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddStripeAdapter((Action<StripeOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStripeAdapter_WithNullConfigurationSection_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddStripeAdapter((IConfigurationSection)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStripeAdapter_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddStripeAdapter(opt => opt.SecretKey = "sk_test_chain");

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddStripeAdapter_RegistersServicesAsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddStripeAdapter(opt => opt.SecretKey = "sk_test_scope");

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var billing1 = scope1.ServiceProvider.GetRequiredService<IBillingService>();
        var billing2 = scope2.ServiceProvider.GetRequiredService<IBillingService>();

        billing1.Should().NotBeSameAs(billing2);
    }

    [Fact]
    public void AddStripeAdapter_SetsGlobalStripeApiKey()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddStripeAdapter(opt => opt.SecretKey = "sk_test_global_key_set");

        global::Stripe.StripeConfiguration.ApiKey.Should().Be("sk_test_global_key_set");
    }
}
