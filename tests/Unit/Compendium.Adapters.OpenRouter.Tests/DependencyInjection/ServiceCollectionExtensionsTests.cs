// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.OpenRouter.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.OpenRouter.Tests.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static IServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        return services;
    }

    [Fact]
    public void AddOpenRouter_WithConfigureAction_RegistersOptionsAndProvider()
    {
        // Arrange
        var services = BuildServices();

        // Act
        services.AddOpenRouter(o => o.ApiKey = "sk-or-v1-abc");
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IOptions<OpenRouterOptions>>().Should().NotBeNull();
        sp.GetService<IOptions<OpenRouterOptions>>()!.Value.ApiKey.Should().Be("sk-or-v1-abc");
        sp.GetService<IAIProvider>().Should().NotBeNull();
        sp.GetService<IAIProvider>()!.ProviderId.Should().Be("openrouter");
    }

    [Fact]
    public void AddOpenRouter_WithConfigureAction_RegistersHttpClientFactory()
    {
        // Arrange
        var services = BuildServices();

        // Act
        services.AddOpenRouter(o =>
        {
            o.ApiKey = "sk";
            o.TimeoutSeconds = 42;
        });
        var sp = services.BuildServiceProvider();

        // Assert — IHttpClientFactory is registered when AddHttpClient<T> is used
        var factory = sp.GetService<System.Net.Http.IHttpClientFactory>();
        factory.Should().NotBeNull();
        // Confirm options carried the value through the typed-client configurator
        sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value.TimeoutSeconds.Should().Be(42);
    }

    [Fact]
    public void AddOpenRouter_WithConfigureAction_ResolvesIAIProviderAsSingleton()
    {
        // Arrange
        var services = BuildServices();
        services.AddOpenRouter(o => o.ApiKey = "sk");
        var sp = services.BuildServiceProvider();

        // Act
        var first = sp.GetRequiredService<IAIProvider>();
        var second = sp.GetRequiredService<IAIProvider>();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddOpenRouter_WithConfiguration_BindsOpenRouterSection()
    {
        // Arrange
        var services = BuildServices();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenRouter:ApiKey"] = "sk-bound",
                ["OpenRouter:DefaultModel"] = "openai/gpt-4o",
                ["OpenRouter:DefaultMaxTokens"] = "8192",
                ["OpenRouter:TimeoutSeconds"] = "30",
                ["OpenRouter:SiteName"] = "Test App"
            })
            .Build();

        // Act
        services.AddOpenRouter(config);
        var sp = services.BuildServiceProvider();

        // Assert
        var opts = sp.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
        opts.ApiKey.Should().Be("sk-bound");
        opts.DefaultModel.Should().Be("openai/gpt-4o");
        opts.DefaultMaxTokens.Should().Be(8192);
        opts.TimeoutSeconds.Should().Be(30);
        opts.SiteName.Should().Be("Test App");
        sp.GetService<IAIProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddOpenRouter_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var returned = services.AddOpenRouter(o => o.ApiKey = "sk");

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddOpenRouter_WithConfiguration_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = BuildServices();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenRouter:ApiKey"] = "sk" })
            .Build();

        // Act
        var returned = services.AddOpenRouter(config);

        // Assert
        returned.Should().BeSameAs(services);
    }
}
