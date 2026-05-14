// -----------------------------------------------------------------------
// <copyright file="IFeatureFlags.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.FeatureFlags.Models;

namespace Compendium.Abstractions.FeatureFlags;

/// <summary>
/// Provides feature flag and experiment evaluation operations.
/// This interface is provider-agnostic and can be implemented by various feature flag
/// providers such as GrowthBook, Flagsmith, LaunchDarkly, ConfigCat or self-hosted backends.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    /// Evaluates a boolean feature flag for the given context.
    /// </summary>
    /// <param name="flagKey">The unique key of the feature flag to evaluate.</param>
    /// <param name="ctx">The evaluation context (mandatory tenant + optional user / attributes).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the boolean evaluation or an error.</returns>
    Task<Result<bool>> IsOnAsync(string flagKey, FlagContext ctx, CancellationToken ct);

    /// <summary>
    /// Evaluates a typed variant feature flag for the given context.
    /// </summary>
    /// <typeparam name="T">The CLR type of the variant value.</typeparam>
    /// <param name="flagKey">The unique key of the feature flag to evaluate.</param>
    /// <param name="defaultValue">The value returned when the provider has no opinion or the flag is missing.</param>
    /// <param name="ctx">The evaluation context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the resolved variant value or an error.</returns>
    Task<Result<T>> GetVariantAsync<T>(string flagKey, T defaultValue, FlagContext ctx, CancellationToken ct);

    /// <summary>
    /// Evaluates an experiment and returns the assignment for the given context.
    /// </summary>
    /// <param name="experimentKey">The unique key of the experiment.</param>
    /// <param name="ctx">The evaluation context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ExperimentAssignment"/> or an error.</returns>
    Task<Result<ExperimentAssignment>> GetExperimentAsync(string experimentKey, FlagContext ctx, CancellationToken ct);
}
