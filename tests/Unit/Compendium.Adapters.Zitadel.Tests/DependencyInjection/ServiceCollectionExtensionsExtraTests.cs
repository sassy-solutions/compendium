// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsExtraTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.DependencyInjection;
using Compendium.Multitenancy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Compendium.Adapters.Zitadel.Tests.DependencyInjection;

/// <summary>
/// Additional tests for <see cref="ServiceCollectionExtensions"/> covering
/// the resolved-services side (scoped lifetimes), <c>AddZitadelHealthCheck</c>,
/// and the SkipSslValidation handler-configuration branch.
/// </summary>
public class ServiceCollectionExtensionsExtraTests
{
    [Fact]
    public void AddZitadel_RegistersAllScopedIdentityServices()
    {
        // Arrange — provide the dependencies the scoped services need
        // (ITenantContext for ZitadelUserService).
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext, TenantContext>();
        services.AddZitadel(opt =>
        {
            opt.Authority = "https://zitadel.example.com";
            opt.ClientId = "cid";
            opt.ClientSecret = "sec";
        });

        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetService<IIdentityUserService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IOrganizationService>().Should().NotBeNull();
        scope.ServiceProvider.GetService<ITokenValidator>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IOrganizationIdentityProvisioner>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IProjectIdentityProvisioner>().Should().NotBeNull();
    }

    [Fact]
    public void AddZitadel_WithSkipSslValidation_DoesNotThrowDuringResolution()
    {
        // Arrange — exercises the `if (options.SkipSslValidation)` branch in the
        // primary handler factory.
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext, TenantContext>();
        services.AddZitadel(opt =>
        {
            opt.Authority = "https://zitadel.example.com";
            opt.ClientId = "cid";
            opt.ClientSecret = "sec";
            opt.SkipSslValidation = true;
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var act = () => scope.ServiceProvider.GetRequiredService<IIdentityUserService>();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddZitadelHealthCheck_RegistersHealthCheckBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZitadel(opt => opt.Authority = "https://zitadel.example.com");

        // Act
        var hcBuilder = services.AddHealthChecks().AddZitadelHealthCheck();

        // Assert
        hcBuilder.Should().NotBeNull();
        var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Should().Contain(r => r.Name == "zitadel");
    }

    [Fact]
    public void AddZitadelHealthCheck_WithCustomNameAndTags_RegistersExpected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZitadel(opt => opt.Authority = "https://zitadel.example.com");

        // Act
        services.AddHealthChecks()
            .AddZitadelHealthCheck(
                name: "iam-zitadel",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "iam" },
                timeout: TimeSpan.FromSeconds(5));

        // Assert
        var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Should().Contain(r => r.Name == "iam-zitadel" && r.Tags.Contains("iam"));
    }

    [Fact]
    public void AddZitadelHealthCheck_WithSkipSslValidation_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZitadel(opt =>
        {
            opt.Authority = "https://zitadel.example.com";
            opt.SkipSslValidation = true;
        });

        // Act
        var act = () => services.AddHealthChecks().AddZitadelHealthCheck();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddZitadelHealthCheck_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHealthChecksBuilder? builder = null;

        // Act
        var act = () => builder!.AddZitadelHealthCheck();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
