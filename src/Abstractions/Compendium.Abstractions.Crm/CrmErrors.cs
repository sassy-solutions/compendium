// -----------------------------------------------------------------------
// <copyright file="CrmErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Crm;

/// <summary>
/// Provides standardized error definitions for CRM synchronization operations.
/// </summary>
public static class CrmErrors
{
    /// <summary>
    /// Gets the error code prefix for CRM errors.
    /// </summary>
    public const string Prefix = "Crm";

    /// <summary>
    /// The requested contact was not found in the CRM provider.
    /// </summary>
    public static Error ContactNotFound(string externalId) =>
        Error.NotFound($"{Prefix}.ContactNotFound", $"Contact '{externalId}' was not found.");

    /// <summary>
    /// A contact with the same identifying field already exists.
    /// </summary>
    public static Error DuplicateContact(string identifier) =>
        Error.Conflict($"{Prefix}.DuplicateContact", $"A contact already exists for '{identifier}'.");

    /// <summary>
    /// The supplied email address is invalid.
    /// </summary>
    public static Error InvalidEmail(string email) =>
        Error.Validation($"{Prefix}.InvalidEmail", $"Email address '{email}' is invalid.");

    /// <summary>
    /// The CRM provider could not be reached.
    /// </summary>
    public static Error ProviderUnreachable(string provider) =>
        Error.Failure($"{Prefix}.ProviderUnreachable", $"CRM provider '{provider}' is unreachable.");

    /// <summary>
    /// The CRM provider rejected the request because the rate limit was exceeded.
    /// </summary>
    public static Error RateLimited(TimeSpan? retryAfter = null) =>
        Error.Failure(
            $"{Prefix}.RateLimited",
            retryAfter.HasValue
                ? $"Rate limit exceeded. Retry after {retryAfter.Value.TotalSeconds} seconds."
                : "Rate limit exceeded. Please try again later.");
}
