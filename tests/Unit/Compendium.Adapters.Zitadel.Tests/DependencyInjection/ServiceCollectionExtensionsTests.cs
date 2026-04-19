// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Authentication;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Zitadel.Tests.DependencyInjection;

/// <summary>
/// Unit tests for Zitadel ServiceCollection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddZitadel_WithAction_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddZitadel(options =>
        {
            options.Authority = "https://zitadel.example.com";
            options.ClientId = "test-client";
            options.ClientSecret = "test-secret";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<ZitadelOptions>>().Should().NotBeNull();
        provider.GetService<ZitadelClaimsTransformation>().Should().NotBeNull();
    }

    [Fact]
    public void AddZitadel_WithOptions_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new ZitadelOptions
        {
            Authority = "https://zitadel.example.com",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        // Act
        services.AddZitadel(options);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<ZitadelOptions>>().Should().NotBeNull();
        provider.GetService<ZitadelClaimsTransformation>().Should().NotBeNull();
    }

    [Fact]
    public void AddZitadel_WithAction_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddZitadel(opt =>
        {
            opt.Authority = "https://zitadel.example.com";
            opt.TimeoutSeconds = 60;
            opt.MaxRetries = 5;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ZitadelOptions>>().Value;

        // Assert
        options.Authority.Should().Be("https://zitadel.example.com");
        options.TimeoutSeconds.Should().Be(60);
        options.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void AddZitadel_WithOptions_ConfiguresAllProperties()
    {
        // Arrange
        var services = new ServiceCollection();
        var inputOptions = new ZitadelOptions
        {
            Authority = "https://zitadel.example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            ServiceAccountKeyJson = "{\"key\": \"value\"}",
            ServiceAccountKeyPath = "/path/to/key.json",
            ProjectId = "project-123",
            DefaultOrganizationId = "org-456",
            TimeoutSeconds = 45,
            MaxRetries = 2,
            SkipSslValidation = true
        };

        // Act
        services.AddZitadel(inputOptions);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ZitadelOptions>>().Value;

        // Assert
        options.Authority.Should().Be("https://zitadel.example.com");
        options.ClientId.Should().Be("client-id");
        options.ClientSecret.Should().Be("client-secret");
        options.ServiceAccountKeyJson.Should().Be("{\"key\": \"value\"}");
        options.ServiceAccountKeyPath.Should().Be("/path/to/key.json");
        options.ProjectId.Should().Be("project-123");
        options.DefaultOrganizationId.Should().Be("org-456");
        options.TimeoutSeconds.Should().Be(45);
        options.MaxRetries.Should().Be(2);
        options.SkipSslValidation.Should().BeTrue();
    }

    [Fact]
    public void AddZitadel_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddZitadel(opt => { opt.Authority = "https://test.com"; });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddZitadel_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddZitadel((Action<ZitadelOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddZitadel_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddZitadel((ZitadelOptions)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddZitadel_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddZitadel(opt => opt.Authority = "https://zitadel.example.com");
        var provider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddZitadel_RegistersClaimsTransformationAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZitadel(opt => opt.Authority = "https://zitadel.example.com");
        var provider = services.BuildServiceProvider();

        // Act
        var transformation1 = provider.GetRequiredService<ZitadelClaimsTransformation>();
        var transformation2 = provider.GetRequiredService<ZitadelClaimsTransformation>();

        // Assert
        transformation1.Should().BeSameAs(transformation2);
    }

    [Fact]
    public void AddZitadel_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddZitadel(opt => opt.Authority = "https://test.com");

        // Assert
        result.Should().BeSameAs(services);
    }
}
