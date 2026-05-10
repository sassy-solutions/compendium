// -----------------------------------------------------------------------
// <copyright file="IProductAnalytics.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Analytics.Models;

namespace Compendium.Abstractions.Analytics;

/// <summary>
/// Provides product analytics operations such as event tracking, user identification,
/// group association, and identity aliasing. This interface is provider-agnostic and
/// can be implemented by analytics backends such as PostHog, Mixpanel, or Amplitude.
/// </summary>
public interface IProductAnalytics
{
    /// <summary>
    /// Records an analytics event.
    /// </summary>
    /// <param name="evt">The event to track.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> TrackAsync(AnalyticsEvent evt, CancellationToken ct);

    /// <summary>
    /// Associates a set of properties with a user identified by <paramref name="distinctId"/>.
    /// </summary>
    /// <param name="distinctId">The stable identifier for the user.</param>
    /// <param name="properties">The properties to associate with the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> IdentifyAsync(
        string distinctId,
        IReadOnlyDictionary<string, object> properties,
        CancellationToken ct);

    /// <summary>
    /// Associates a set of properties with a group (e.g. organization, account, project).
    /// </summary>
    /// <param name="groupType">The type of group (e.g. "organization", "account").</param>
    /// <param name="groupKey">The unique key identifying the group within its type.</param>
    /// <param name="properties">The properties to associate with the group.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> GroupAsync(
        string groupType,
        string groupKey,
        IReadOnlyDictionary<string, object> properties,
        CancellationToken ct);

    /// <summary>
    /// Merges a previously-used identity (typically anonymous) into a known distinct identity.
    /// </summary>
    /// <param name="previousId">The previously-used identifier (often anonymous).</param>
    /// <param name="distinctId">The canonical distinct identifier to merge into.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result> AliasAsync(string previousId, string distinctId, CancellationToken ct);
}
