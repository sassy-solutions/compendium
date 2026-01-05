// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.LemonSqueezy.Configuration;

/// <summary>
/// Configuration options for LemonSqueezy adapter.
/// </summary>
public sealed class LemonSqueezyOptions
{
    /// <summary>
    /// Gets or sets the LemonSqueezy API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook signing secret for HMAC-SHA256 validation.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the LemonSqueezy API.
    /// Defaults to the production API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.lemonsqueezy.com/v1/";

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to use test mode.
    /// </summary>
    public bool TestMode { get; set; }
}
