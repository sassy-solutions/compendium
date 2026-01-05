// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Listmonk.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Listmonk.Tests.DependencyInjection;

/// <summary>
/// Unit tests for Listmonk ServiceCollection extensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddListmonk_WithAction_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddListmonk(options =>
        {
            options.BaseUrl = "https://listmonk.example.com";
            options.Username = "admin";
            options.Password = "password";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<ListmonkOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddListmonk_WithOptions_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new ListmonkOptions
        {
            BaseUrl = "https://listmonk.example.com",
            Username = "admin",
            Password = "password"
        };

        // Act
        services.AddListmonk(options);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<ListmonkOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddListmonk_WithAction_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddListmonk(opt =>
        {
            opt.BaseUrl = "https://listmonk.example.com";
            opt.Username = "testuser";
            opt.Password = "testpass";
            opt.DefaultFromEmail = "noreply@test.com";
            opt.TimeoutSeconds = 60;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ListmonkOptions>>().Value;

        // Assert
        options.BaseUrl.Should().Be("https://listmonk.example.com");
        options.Username.Should().Be("testuser");
        options.Password.Should().Be("testpass");
        options.DefaultFromEmail.Should().Be("noreply@test.com");
        options.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void AddListmonk_WithOptions_ConfiguresAllProperties()
    {
        // Arrange
        var services = new ServiceCollection();
        var inputOptions = new ListmonkOptions
        {
            BaseUrl = "https://listmonk.example.com",
            Username = "admin",
            Password = "secret",
            DefaultFromEmail = "mail@example.com",
            DefaultFromName = "Example",
            DefaultListId = 5,
            TimeoutSeconds = 45,
            MaxRetries = 2,
            SkipSslValidation = true
        };

        // Act
        services.AddListmonk(inputOptions);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ListmonkOptions>>().Value;

        // Assert
        options.BaseUrl.Should().Be("https://listmonk.example.com");
        options.Username.Should().Be("admin");
        options.Password.Should().Be("secret");
        options.DefaultFromEmail.Should().Be("mail@example.com");
        options.DefaultFromName.Should().Be("Example");
        options.DefaultListId.Should().Be(5);
        options.TimeoutSeconds.Should().Be(45);
        options.MaxRetries.Should().Be(2);
        options.SkipSslValidation.Should().BeTrue();
    }

    [Fact]
    public void AddListmonk_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddListmonk(opt =>
        {
            opt.BaseUrl = "https://test.com";
        });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddListmonk_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddListmonk((Action<ListmonkOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddListmonk_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddListmonk((ListmonkOptions)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddListmonk_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddListmonk(opt =>
        {
            opt.BaseUrl = "https://listmonk.example.com";
            opt.Username = "admin";
            opt.Password = "pass";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddListmonk_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddListmonk(opt =>
        {
            opt.BaseUrl = "https://test.com";
            opt.Username = "user";
            opt.Password = "pass";
        });

        // Assert
        result.Should().BeSameAs(services);
    }
}
