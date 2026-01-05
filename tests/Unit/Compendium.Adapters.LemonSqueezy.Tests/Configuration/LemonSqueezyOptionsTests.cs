// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyOptionsTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.LemonSqueezy.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="LemonSqueezyOptions"/>.
/// </summary>
public class LemonSqueezyOptionsTests
{
    [Fact]
    public void LemonSqueezyOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.StoreId.Should().BeEmpty();
        options.WebhookSigningSecret.Should().BeEmpty();
        options.BaseUrl.Should().Be("https://api.lemonsqueezy.com/v1/");
        options.TimeoutSeconds.Should().Be(30);
        options.MaxRetries.Should().Be(3);
        options.TestMode.Should().BeFalse();
    }

    [Fact]
    public void LemonSqueezyOptions_WithCustomValues_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions
        {
            ApiKey = "sk_test_abc123",
            StoreId = "store-456",
            WebhookSigningSecret = "whsec_xyz789",
            BaseUrl = "https://custom.api.lemonsqueezy.com/v1/",
            TimeoutSeconds = 60,
            MaxRetries = 5,
            TestMode = true
        };

        // Assert
        options.ApiKey.Should().Be("sk_test_abc123");
        options.StoreId.Should().Be("store-456");
        options.WebhookSigningSecret.Should().Be("whsec_xyz789");
        options.BaseUrl.Should().Be("https://custom.api.lemonsqueezy.com/v1/");
        options.TimeoutSeconds.Should().Be(60);
        options.MaxRetries.Should().Be(5);
        options.TestMode.Should().BeTrue();
    }

    [Fact]
    public void LemonSqueezyOptions_BaseUrl_HasProductionDefault()
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions();

        // Assert
        options.BaseUrl.Should().Contain("api.lemonsqueezy.com");
        options.BaseUrl.Should().EndWith("/");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(120)]
    public void LemonSqueezyOptions_TimeoutSeconds_AcceptsValidValues(int timeout)
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions { TimeoutSeconds = timeout };

        // Assert
        options.TimeoutSeconds.Should().Be(timeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void LemonSqueezyOptions_MaxRetries_AcceptsValidValues(int retries)
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions { MaxRetries = retries };

        // Assert
        options.MaxRetries.Should().Be(retries);
    }

    [Fact]
    public void LemonSqueezyOptions_TestMode_DefaultsToFalse()
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions();

        // Assert
        options.TestMode.Should().BeFalse();
    }

    [Fact]
    public void LemonSqueezyOptions_TestMode_CanBeEnabled()
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions { TestMode = true };

        // Assert
        options.TestMode.Should().BeTrue();
    }

    [Theory]
    [InlineData("sk_live_12345")]
    [InlineData("sk_test_67890")]
    public void LemonSqueezyOptions_ApiKey_AcceptsLiveAndTestKeys(string apiKey)
    {
        // Arrange & Act
        var options = new LemonSqueezyOptions { ApiKey = apiKey };

        // Assert
        options.ApiKey.Should().Be(apiKey);
    }
}
