// -----------------------------------------------------------------------
// <copyright file="ListmonkOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Listmonk.Configuration;

/// <summary>
/// Configuration options for the Listmonk email service adapter.
/// </summary>
public sealed class ListmonkOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Listmonk API.
    /// </summary>
    /// <example>https://listmonk.yourdomain.com</example>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for Basic authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for Basic authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default "from" email address.
    /// </summary>
    public string DefaultFromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default "from" name.
    /// </summary>
    public string DefaultFromName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default list ID for new subscribers.
    /// </summary>
    public int? DefaultListId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether to skip SSL certificate validation.
    /// Only use in development environments.
    /// </summary>
    public bool SkipSslValidation { get; set; }
}
