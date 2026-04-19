// -----------------------------------------------------------------------
// <copyright file="ExternalAdaptersOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Multitenancy.Extensions;
using FluentAssertions;

namespace Compendium.Extensions.ExternalAdapters.Tests;

/// <summary>
/// Unit tests for <see cref="ExternalAdaptersOptions"/>.
/// </summary>
public class ExternalAdaptersOptionsTests
{
    [Fact]
    public void ExternalAdaptersOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions();

        // Assert
        options.Zitadel.Should().BeNull();
        options.Listmonk.Should().BeNull();
        options.LemonSqueezy.Should().BeNull();
        options.Multitenancy.Should().BeNull();
        options.EnableZitadel.Should().BeFalse();
        options.EnableListmonk.Should().BeFalse();
        options.EnableLemonSqueezy.Should().BeFalse();
        options.EnableMultitenancy.Should().BeFalse();
    }

    [Fact]
    public void ExternalAdaptersOptions_WithZitadel_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions
        {
            EnableZitadel = true,
            Zitadel = new ZitadelOptions
            {
                Authority = "https://zitadel.example.com",
                ClientId = "client-123"
            }
        };

        // Assert
        options.EnableZitadel.Should().BeTrue();
        options.Zitadel.Should().NotBeNull();
        options.Zitadel!.Authority.Should().Be("https://zitadel.example.com");
        options.Zitadel.ClientId.Should().Be("client-123");
    }

    [Fact]
    public void ExternalAdaptersOptions_WithListmonk_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions
        {
            EnableListmonk = true,
            Listmonk = new ListmonkOptions
            {
                BaseUrl = "https://listmonk.example.com",
                Username = "admin",
                Password = "secret"
            }
        };

        // Assert
        options.EnableListmonk.Should().BeTrue();
        options.Listmonk.Should().NotBeNull();
        options.Listmonk!.BaseUrl.Should().Be("https://listmonk.example.com");
        options.Listmonk.Username.Should().Be("admin");
    }

    [Fact]
    public void ExternalAdaptersOptions_WithLemonSqueezy_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions
        {
            EnableLemonSqueezy = true,
            LemonSqueezy = new LemonSqueezyOptions
            {
                ApiKey = "sk_live_xyz",
                StoreId = "store-123"
            }
        };

        // Assert
        options.EnableLemonSqueezy.Should().BeTrue();
        options.LemonSqueezy.Should().NotBeNull();
        options.LemonSqueezy!.ApiKey.Should().Be("sk_live_xyz");
        options.LemonSqueezy.StoreId.Should().Be("store-123");
    }

    [Fact]
    public void ExternalAdaptersOptions_WithMultitenancy_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions
        {
            EnableMultitenancy = true,
            Multitenancy = new MultitenancyOptions
            {
                RequireTenant = true,
                DefaultTenantId = "default"
            }
        };

        // Assert
        options.EnableMultitenancy.Should().BeTrue();
        options.Multitenancy.Should().NotBeNull();
        options.Multitenancy!.RequireTenant.Should().BeTrue();
        options.Multitenancy.DefaultTenantId.Should().Be("default");
    }

    [Fact]
    public void ExternalAdaptersOptions_WithAllAdapters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ExternalAdaptersOptions
        {
            EnableZitadel = true,
            Zitadel = new ZitadelOptions { Authority = "https://zitadel.io" },
            EnableListmonk = true,
            Listmonk = new ListmonkOptions { BaseUrl = "https://listmonk.io" },
            EnableLemonSqueezy = true,
            LemonSqueezy = new LemonSqueezyOptions { ApiKey = "key" },
            EnableMultitenancy = true,
            Multitenancy = new MultitenancyOptions { RequireTenant = true }
        };

        // Assert
        options.EnableZitadel.Should().BeTrue();
        options.Zitadel.Should().NotBeNull();
        options.EnableListmonk.Should().BeTrue();
        options.Listmonk.Should().NotBeNull();
        options.EnableLemonSqueezy.Should().BeTrue();
        options.LemonSqueezy.Should().NotBeNull();
        options.EnableMultitenancy.Should().BeTrue();
        options.Multitenancy.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for <see cref="ExternalAdaptersConfigurationSections"/>.
/// </summary>
public class ExternalAdaptersConfigurationSectionsTests
{
    [Fact]
    public void ConfigurationSections_HasCorrectValues()
    {
        // Assert
        ExternalAdaptersConfigurationSections.Root.Should().Be("Compendium:ExternalAdapters");
        ExternalAdaptersConfigurationSections.Zitadel.Should().Be("Zitadel");
        ExternalAdaptersConfigurationSections.Listmonk.Should().Be("Listmonk");
        ExternalAdaptersConfigurationSections.LemonSqueezy.Should().Be("LemonSqueezy");
        ExternalAdaptersConfigurationSections.Multitenancy.Should().Be("Multitenancy");
    }
}
