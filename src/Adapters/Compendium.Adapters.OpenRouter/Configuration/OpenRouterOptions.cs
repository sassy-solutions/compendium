// -----------------------------------------------------------------------
// <copyright file="OpenRouterOptions.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.OpenRouter.Configuration;

/// <summary>
/// Configuration options for the OpenRouter AI provider.
/// </summary>
public sealed class OpenRouterOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "OpenRouter";

    /// <summary>
    /// Gets or sets the OpenRouter API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the OpenRouter API.
    /// Default is "https://openrouter.ai/api/v1".
    /// </summary>
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    /// <summary>
    /// Gets or sets the default model to use when not specified.
    /// Default is "anthropic/claude-3.5-sonnet".
    /// </summary>
    public string DefaultModel { get; set; } = "anthropic/claude-3.5-sonnet";

    /// <summary>
    /// Gets or sets the default temperature.
    /// </summary>
    public float DefaultTemperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the default maximum tokens.
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the number of retry attempts for transient failures.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the site URL to send with requests (for OpenRouter rankings).
    /// </summary>
    public string? SiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the site name to send with requests (for OpenRouter rankings).
    /// </summary>
    public string? SiteName { get; set; }

    /// <summary>
    /// Gets or sets whether to enable request/response logging.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets model-specific configurations.
    /// </summary>
    public Dictionary<string, ModelConfig> Models { get; set; } = new();

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid() => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// Configuration for a specific model.
/// </summary>
public sealed class ModelConfig
{
    /// <summary>
    /// Gets or sets the maximum tokens for this model.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the default temperature for this model.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Gets or sets custom parameters for this model.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
