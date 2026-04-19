// -----------------------------------------------------------------------
// <copyright file="StripeOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Stripe.Configuration;
using FluentAssertions;

namespace Compendium.Adapters.Stripe.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="StripeOptions"/>.
/// </summary>
public class StripeOptionsTests
{
    [Fact]
    public void StripeOptions_DefaultValues_AreCorrect()
    {
        var options = new StripeOptions();

        options.SecretKey.Should().BeEmpty();
        options.PublishableKey.Should().BeNull();
        options.WebhookSigningSecret.Should().BeEmpty();
        options.ApiVersion.Should().BeNull();
        options.TestMode.Should().BeFalse();
    }

    [Fact]
    public void StripeOptions_WithCustomValues_SetsPropertiesCorrectly()
    {
        var options = new StripeOptions
        {
            SecretKey = "sk_test_abc",
            PublishableKey = "pk_test_xyz",
            WebhookSigningSecret = "whsec_signing",
            ApiVersion = "2024-06-20",
            TestMode = true
        };

        options.SecretKey.Should().Be("sk_test_abc");
        options.PublishableKey.Should().Be("pk_test_xyz");
        options.WebhookSigningSecret.Should().Be("whsec_signing");
        options.ApiVersion.Should().Be("2024-06-20");
        options.TestMode.Should().BeTrue();
    }

    [Theory]
    [InlineData("sk_live_abc123")]
    [InlineData("sk_test_def456")]
    public void StripeOptions_SecretKey_AcceptsLiveAndTestKeys(string key)
    {
        var options = new StripeOptions { SecretKey = key };

        options.SecretKey.Should().Be(key);
    }

    [Fact]
    public void StripeOptions_TestMode_CanBeEnabled()
    {
        var options = new StripeOptions { TestMode = true };

        options.TestMode.Should().BeTrue();
    }
}
