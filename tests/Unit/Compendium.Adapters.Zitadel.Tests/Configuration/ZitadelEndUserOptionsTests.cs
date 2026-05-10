// -----------------------------------------------------------------------
// <copyright file="ZitadelEndUserOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Zitadel.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ZitadelEndUserOptions"/>.
/// </summary>
public class ZitadelEndUserOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange / Act
        var options = new ZitadelEndUserOptions();

        // Assert
        options.Authority.Should().BeEmpty();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ProjectId.Should().BeNull();
        options.Audience.Should().BeNull();
        options.SelfRegistrationEnabled.Should().BeTrue();
        options.WebhookSecret.Should().BeNull();
        options.WebhookSignatureHeader.Should().Be("X-Zitadel-Signature");
        options.TimeoutSeconds.Should().Be(30);
        options.SkipSslValidation.Should().BeFalse();
        options.DefaultSubscriptionTier.Should().Be("Free");
        options.OrgToTenantMapping.Should().BeEmpty();
    }

    [Fact]
    public void SectionName_IsZitadelEndUser()
    {
        // Arrange / Act / Assert
        ZitadelEndUserOptions.SectionName.Should().Be("ZitadelEndUser");
    }

    [Fact]
    public void AuthorizationEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelEndUserOptions { Authority = "https://zitadel.example.com" };

        // Act / Assert
        options.AuthorizationEndpoint.Should().Be("https://zitadel.example.com/oauth/v2/authorize");
    }

    [Fact]
    public void TokenEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelEndUserOptions { Authority = "https://zitadel.example.com/" };

        // Act / Assert
        options.TokenEndpoint.Should().Be("https://zitadel.example.com/oauth/v2/token");
    }

    [Fact]
    public void UserInfoEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelEndUserOptions { Authority = "https://zitadel.example.com" };

        // Act / Assert
        options.UserInfoEndpoint.Should().Be("https://zitadel.example.com/oidc/v1/userinfo");
    }

    [Fact]
    public void JwksEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelEndUserOptions { Authority = "https://zitadel.example.com" };

        // Act / Assert
        options.JwksEndpoint.Should().Be("https://zitadel.example.com/.well-known/jwks.json");
    }

    [Fact]
    public void DiscoveryEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelEndUserOptions { Authority = "https://zitadel.example.com" };

        // Act / Assert
        options.DiscoveryEndpoint.Should().Be("https://zitadel.example.com/.well-known/openid-configuration");
    }

    [Fact]
    public void OrgToTenantMapping_AcceptsKeyValueEntries()
    {
        // Arrange
        var options = new ZitadelEndUserOptions
        {
            OrgToTenantMapping = new Dictionary<string, string>
            {
                ["zitadel-org-1"] = "tenant-1",
                ["zitadel-org-2"] = "tenant-2"
            }
        };

        // Act / Assert
        options.OrgToTenantMapping.Should().HaveCount(2);
        options.OrgToTenantMapping["zitadel-org-1"].Should().Be("tenant-1");
    }

    [Fact]
    public void WithCustomValues_SetsPropertiesCorrectly()
    {
        // Arrange / Act
        var options = new ZitadelEndUserOptions
        {
            Authority = "https://x",
            ClientId = "cid",
            ClientSecret = "csec",
            ProjectId = "pid",
            Audience = "aud",
            SelfRegistrationEnabled = false,
            WebhookSecret = "wsec",
            WebhookSignatureHeader = "X-Custom-Sig",
            TimeoutSeconds = 60,
            SkipSslValidation = true,
            DefaultSubscriptionTier = "Pro"
        };

        // Assert
        options.Authority.Should().Be("https://x");
        options.ClientId.Should().Be("cid");
        options.ClientSecret.Should().Be("csec");
        options.ProjectId.Should().Be("pid");
        options.Audience.Should().Be("aud");
        options.SelfRegistrationEnabled.Should().BeFalse();
        options.WebhookSecret.Should().Be("wsec");
        options.WebhookSignatureHeader.Should().Be("X-Custom-Sig");
        options.TimeoutSeconds.Should().Be(60);
        options.SkipSslValidation.Should().BeTrue();
        options.DefaultSubscriptionTier.Should().Be("Pro");
    }
}
