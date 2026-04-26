// -----------------------------------------------------------------------
// <copyright file="PiiMasking.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Shared.Logging;

/// <summary>
/// PII masking helpers for log statements. Use sparingly — prefer non-PII identifiers
/// (subscriber_id, customer_id, activity_id) over masked PII per GDPR data-minimization.
/// </summary>
public static class PiiMasking
{
    /// <summary>
    /// Masks an email for logging: "john.doe@acme.com" → "j***@acme.com".
    /// Returns "&lt;empty&gt;" or "&lt;null&gt;" for non-values.
    /// Use only when subscriber_id/customer_id is unavailable AND email correlation is required for debugging.
    /// </summary>
    /// <param name="email">The email to mask.</param>
    /// <returns>Masked email, or a placeholder for empty/invalid input.</returns>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "<empty>";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";
        return $"{email[0]}***{email[atIndex..]}";
    }
}
