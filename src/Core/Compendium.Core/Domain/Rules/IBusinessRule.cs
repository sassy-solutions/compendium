// -----------------------------------------------------------------------
// <copyright file="IBusinessRule.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
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
