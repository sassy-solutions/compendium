// -----------------------------------------------------------------------
// <copyright file="WebhookEndpointTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks.Tests.Models;

public class WebhookEndpointTests
{
    [Fact]
    public void WebhookEndpoint_WithRequiredOnly_DefaultsActiveTrueAndSecretNull()
    {
        // Arrange / Act
        var endpoint = new WebhookEndpoint
        {
            Id = "ep-1",
            Url = new Uri("https://example.com/hook"),
            TenantId = "tenant-1",
            EventFilters = new[] { "order.created" },
        };

        // Assert
        endpoint.Id.Should().Be("ep-1");
        endpoint.Url.Should().Be(new Uri("https://example.com/hook"));
        endpoint.TenantId.Should().Be("tenant-1");
        endpoint.EventFilters.Should().ContainSingle().Which.Should().Be("order.created");
        endpoint.SigningSecret.Should().BeNull();
        endpoint.Active.Should().BeTrue();
    }

    [Fact]
    public void WebhookEndpoint_WithExplicitSecretAndInactive_PreservesValues()
    {
        // Arrange / Act
        var endpoint = new WebhookEndpoint
        {
            Id = "ep-2",
            Url = new Uri("https://hooks.example.com/x"),
            TenantId = "t",
            EventFilters = Array.Empty<string>(),
            SigningSecret = "shhh",
            Active = false,
        };

        // Assert
        endpoint.SigningSecret.Should().Be("shhh");
        endpoint.Active.Should().BeFalse();
        endpoint.EventFilters.Should().BeEmpty();
    }

    [Fact]
    public void WebhookEndpoint_RecordEquality_IdenticalEndpoints_AreEqual()
    {
        // Arrange
        var filters = new[] { "a", "b" };
        var first = new WebhookEndpoint
        {
            Id = "id",
            Url = new Uri("https://e/"),
            TenantId = "t",
            EventFilters = filters,
            SigningSecret = "s",
            Active = true,
        };
        var second = new WebhookEndpoint
        {
            Id = "id",
            Url = new Uri("https://e/"),
            TenantId = "t",
            EventFilters = filters,
            SigningSecret = "s",
            Active = true,
        };

        // Act / Assert
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void WebhookEndpoint_RecordEquality_DifferingTenant_AreNotEqual()
    {
        // Arrange
        var filters = new[] { "a" };
        var first = new WebhookEndpoint
        {
            Id = "id",
            Url = new Uri("https://e/"),
            TenantId = "tenant-a",
            EventFilters = filters,
        };
        var second = first with { TenantId = "tenant-b" };

        // Act / Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void WebhookEndpoint_With_FlippingActive_ProducesModifiedCopy()
    {
        // Arrange
        var endpoint = new WebhookEndpoint
        {
            Id = "id",
            Url = new Uri("https://e/"),
            TenantId = "t",
            EventFilters = Array.Empty<string>(),
        };

        // Act
        var disabled = endpoint with { Active = false };

        // Assert
        disabled.Active.Should().BeFalse();
        endpoint.Active.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com/hook")]
    [InlineData("http://localhost:9000/in")]
    public void WebhookEndpoint_AcceptsHttpAndHttpsUrls(string url)
    {
        // Arrange / Act
        var endpoint = new WebhookEndpoint
        {
            Id = "id",
            Url = new Uri(url),
            TenantId = "t",
            EventFilters = Array.Empty<string>(),
        };

        // Assert
        endpoint.Url.OriginalString.Should().Be(url);
    }
}
