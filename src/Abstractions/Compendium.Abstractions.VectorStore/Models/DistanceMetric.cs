// -----------------------------------------------------------------------
// <copyright file="DistanceMetric.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Models;

/// <summary>
/// Defines the distance metric used to compare vectors during similarity search.
/// </summary>
public enum DistanceMetric
{
    /// <summary>
    /// Cosine similarity (angular distance). Range typically [-1, 1] or [0, 2] depending on backend.
    /// </summary>
    Cosine = 0,

    /// <summary>
    /// Euclidean (L2) distance. Lower means closer.
    /// </summary>
    L2 = 1,

    /// <summary>
    /// Inner (dot) product. Higher means closer.
    /// </summary>
    InnerProduct = 2,
}
