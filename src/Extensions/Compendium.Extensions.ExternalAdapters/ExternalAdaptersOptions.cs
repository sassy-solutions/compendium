// -----------------------------------------------------------------------
// <copyright file="ExternalAdaptersOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.Listmonk.Configuration;
using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Multitenancy.Extensions;

namespace Compendium.Extensions.ExternalAdapters;

/// <summary>
/// Unified configuration options for all external adapters.
/// </summary>
public sealed class ExternalAdaptersOptions
{
    /// <summary>
    /// Gets or sets the Zitadel identity provider options.
    /// </summary>
    public ZitadelOptions? Zitadel { get; set; }

    /// <summary>
    /// Gets or sets the Listmonk email/newsletter options.
    /// </summary>
    public ListmonkOptions? Listmonk { get; set; }

    /// <summary>
    /// Gets or sets the LemonSqueezy billing options.
    /// </summary>
    public LemonSqueezyOptions? LemonSqueezy { get; set; }

    /// <summary>
    /// Gets or sets the multitenancy options.
    /// </summary>
    public MultitenancyOptions? Multitenancy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable Zitadel integration.
    /// </summary>
    public bool EnableZitadel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable Listmonk integration.
    /// </summary>
    public bool EnableListmonk { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable LemonSqueezy integration.
    /// </summary>
    public bool EnableLemonSqueezy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable multitenancy.
    /// </summary>
    public bool EnableMultitenancy { get; set; }
}

/// <summary>
/// Configuration section names for external adapters.
/// </summary>
public static class ExternalAdaptersConfigurationSections
{
    /// <summary>
    /// The root section name for all external adapters configuration.
    /// </summary>
    public const string Root = "Compendium:ExternalAdapters";

    /// <summary>
    /// The section name for Zitadel configuration.
    /// </summary>
    public const string Zitadel = "Zitadel";

    /// <summary>
    /// The section name for Listmonk configuration.
    /// </summary>
    public const string Listmonk = "Listmonk";

    /// <summary>
    /// The section name for LemonSqueezy configuration.
    /// </summary>
    public const string LemonSqueezy = "LemonSqueezy";

    /// <summary>
    /// The section name for multitenancy configuration.
    /// </summary>
    public const string Multitenancy = "Multitenancy";
}
