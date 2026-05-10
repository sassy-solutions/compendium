// -----------------------------------------------------------------------
// <copyright file="FlagContext.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags.Models;

/// <summary>
/// Evaluation context passed to a feature flag provider.
/// </summary>
/// <remarks>
/// A non-empty <see cref="TenantId"/> is mandatory: feature flag and experiment
/// evaluation in Compendium is always tenant-scoped to prevent cross-tenant leakage of
/// rollouts, gradual exposures and experiment cohorts.
/// </remarks>
public sealed record FlagContext
{
    private static readonly IReadOnlyDictionary<string, object> EmptyAttributes =
        new Dictionary<string, object>(0);

    /// <summary>
    /// Initializes a new instance of the <see cref="FlagContext"/> record.
    /// </summary>
    /// <param name="tenantId">The mandatory tenant identifier evaluating the flag.</param>
    /// <param name="userId">Optional stable identifier for the user requesting the flag, used for sticky bucketing.</param>
    /// <param name="attributes">Optional targeting attributes used by provider rules.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/> is null, empty or whitespace.</exception>
    public FlagContext(string tenantId, string? userId = null, IReadOnlyDictionary<string, object>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "TenantId is required for feature flag evaluation and cannot be null, empty or whitespace.",
                nameof(tenantId));
        }

        TenantId = tenantId;
        UserId = userId;
        Attributes = attributes ?? EmptyAttributes;
    }

    /// <summary>
    /// Gets the mandatory tenant identifier evaluating the flag.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the optional stable identifier of the user requesting the flag.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Gets the additional targeting attributes provided to the flag provider.
    /// </summary>
    public IReadOnlyDictionary<string, object> Attributes { get; }
}
