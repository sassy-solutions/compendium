// -----------------------------------------------------------------------
// <copyright file="IBusinessRule.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Rules;

/// <summary>
/// Represents a business rule that can be validated within the domain.
/// Business rules encapsulate domain logic and invariants that must be maintained.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the error message that describes why the business rule is broken.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the error code that uniquely identifies the type of business rule violation.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// Determines whether the business rule is broken.
    /// </summary>
    /// <returns>true if the business rule is broken; otherwise, false.</returns>
    bool IsBroken();
}
