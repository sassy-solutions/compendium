// -----------------------------------------------------------------------
// <copyright file="ExperimentAssignment.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.FeatureFlags.Models;

/// <summary>
/// Represents the result of evaluating an experiment for a given <see cref="FlagContext"/>.
/// </summary>
/// <param name="VariationKey">The key of the variation assigned to the caller (e.g. "control", "v1").</param>
/// <param name="Value">The payload associated with the assigned variation.</param>
/// <param name="InExperiment">
/// <c>true</c> when the caller was actually enrolled in the experiment;
/// <c>false</c> when the value comes from a fallback / default cohort.
/// </param>
public sealed record ExperimentAssignment(
    string VariationKey,
    object Value,
    bool InExperiment);
