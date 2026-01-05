// -----------------------------------------------------------------------
// <copyright file="BusinessRuleValidationException.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Domain.Rules;

/// <summary>
/// Exception thrown when a business rule validation fails.
/// This exception should be caught at the application boundary and converted to appropriate error responses.
/// </summary>
public sealed class BusinessRuleValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> class.
    /// </summary>
    /// <param name="brokenRule">The business rule that was broken.</param>
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule?.Message ?? "A business rule was violated.")
    {
        BrokenRule = brokenRule ?? throw new ArgumentNullException(nameof(brokenRule));
        ErrorCode = brokenRule.ErrorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> class.
    /// </summary>
    /// <param name="brokenRule">The business rule that was broken.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BusinessRuleValidationException(IBusinessRule brokenRule, Exception innerException)
        : base(brokenRule?.Message ?? "A business rule was violated.", innerException)
    {
        BrokenRule = brokenRule ?? throw new ArgumentNullException(nameof(brokenRule));
        ErrorCode = brokenRule.ErrorCode;
    }

    /// <summary>
    /// Gets the business rule that was broken.
    /// </summary>
    public IBusinessRule BrokenRule { get; }

    /// <summary>
    /// Gets the error code that uniquely identifies the type of business rule violation.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Returns a string representation of the exception.
    /// </summary>
    /// <returns>A string that represents the current exception.</returns>
    public override string ToString()
    {
        return $"{GetType().Name}: {Message} (ErrorCode: {ErrorCode})";
    }
}
