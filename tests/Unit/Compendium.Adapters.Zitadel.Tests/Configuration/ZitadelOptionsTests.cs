// -----------------------------------------------------------------------
// <copyright file="ZitadelOptionsTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Zitadel.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ZitadelOptions"/>.
/// </summary>
public class ZitadelOptionsTests
{
    [Fact]
    public void ZitadelOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ZitadelOptions();

        // Assert
        options.Authority.Should().BeEmpty();
        options.ServiceAccountKeyJson.Should().BeNull();
        options.ServiceAccountKeyPath.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ProjectId.Should().BeNull();
        options.DefaultOrganizationId.Should().BeNull();
        options.TimeoutSeconds.Should().Be(30);
        options.MaxRetries.Should().Be(3);
        options.SkipSslValidation.Should().BeFalse();
        options.RedirectUriTemplate.Should().BeNull();
        options.PostLogoutUriTemplate.Should().BeNull();
    }

    [Fact]
    public void ZitadelOptions_UriTemplateFields_CanBeSet()
    {
        // Arrange & Act
        var options = new ZitadelOptions
        {
            RedirectUriTemplate = "https://{organization}.example.com/cb",
            PostLogoutUriTemplate = "https://{organization}.example.com",
        };

        // Assert
        options.RedirectUriTemplate.Should().Be("https://{organization}.example.com/cb");
        options.PostLogoutUriTemplate.Should().Be("https://{organization}.example.com");
    }

    [Fact]
    public void ZitadelOptions_ManagementApiUrl_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.ManagementApiUrl;

        // Assert
        url.Should().Be("https://zitadel.example.com/management/v1");
    }

    [Fact]
    public void ZitadelOptions_ManagementApiUrl_TrimsTrailingSlash()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com/" };

        // Act
        var url = options.ManagementApiUrl;

        // Assert
        url.Should().Be("https://zitadel.example.com/management/v1");
    }

    [Fact]
    public void ZitadelOptions_AuthApiUrl_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.AuthApiUrl;

        // Assert
        url.Should().Be("https://zitadel.example.com/auth/v1");
    }

    [Fact]
    public void ZitadelOptions_UserApiV2Url_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.UserApiV2Url;

        // Assert
        url.Should().Be("https://zitadel.example.com/v2");
    }

    [Fact]
    public void ZitadelOptions_TokenEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.TokenEndpoint;

        // Assert
        url.Should().Be("https://zitadel.example.com/oauth/v2/token");
    }

    [Fact]
    public void ZitadelOptions_IntrospectionEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.IntrospectionEndpoint;

        // Assert
        url.Should().Be("https://zitadel.example.com/oauth/v2/introspect");
    }

    [Fact]
    public void ZitadelOptions_JwksEndpoint_BuildsCorrectUrl()
    {
        // Arrange
        var options = new ZitadelOptions { Authority = "https://zitadel.example.com" };

        // Act
        var url = options.JwksEndpoint;

        // Assert
        url.Should().Be("https://zitadel.example.com/.well-known/jwks.json");
    }

    [Fact]
    public void ZitadelOptions_WithCustomValues_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new ZitadelOptions
        {
            Authority = "https://custom.zitadel.io",
            ClientId = "my-client-id",
            ClientSecret = "my-client-secret",
            ProjectId = "project-123",
            DefaultOrganizationId = "org-456",
            TimeoutSeconds = 60,
            MaxRetries = 5,
            SkipSslValidation = true
        };

        // Assert
        options.Authority.Should().Be("https://custom.zitadel.io");
        options.ClientId.Should().Be("my-client-id");
        options.ClientSecret.Should().Be("my-client-secret");
        options.ProjectId.Should().Be("project-123");
        options.DefaultOrganizationId.Should().Be("org-456");
        options.TimeoutSeconds.Should().Be(60);
        options.MaxRetries.Should().Be(5);
        options.SkipSslValidation.Should().BeTrue();
    }
}
