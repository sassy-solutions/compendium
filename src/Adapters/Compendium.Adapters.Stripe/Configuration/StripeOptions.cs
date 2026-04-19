// -----------------------------------------------------------------------
// <copyright file="StripeOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Stripe.Configuration;

/// <summary>
/// Configuration options for the Stripe billing adapter.
/// </summary>
public sealed class StripeOptions
{
    /// <summary>
    /// Gets or sets the Stripe secret API key (e.g., "sk_live_..." or "sk_test_...").
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Stripe publishable key (e.g., "pk_live_..." or "pk_test_...").
    /// Optional; primarily used by frontend clients.
    /// </summary>
    public string? PublishableKey { get; set; }

    /// <summary>
    /// Gets or sets the webhook signing secret (e.g., "whsec_...") used to validate
    /// incoming webhook payloads via HMAC-SHA256. When empty, signature validation is
    /// bypassed (development mode only).
    /// </summary>
    public string WebhookSigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional pinned Stripe API version (e.g., "2024-06-20").
    /// When null, the Stripe.net default version is used.
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether test mode is enabled. This is a
    /// convenience flag for callers; the actual mode is determined by the secret key.
    /// </summary>
    public bool TestMode { get; set; }
}
