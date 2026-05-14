// -----------------------------------------------------------------------
// <copyright file="AuthorizationErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization;

/// <summary>
/// Provides standard error definitions for relationship-based authorization operations.
/// </summary>
public static class AuthorizationErrors
{
    /// <summary>
    /// Error returned when a subject is not authorized to perform the requested relation on the object.
    /// </summary>
    /// <param name="subject">The subject that was denied.</param>
    /// <param name="relation">The relation that was checked.</param>
    /// <param name="object">The object the relation was checked against.</param>
    /// <returns>A forbidden error describing the denial.</returns>
    public static Error NotAuthorized(string subject, string relation, string @object) =>
        Error.Forbidden(
            "Authorization.NotAuthorized",
            $"Subject '{subject}' is not authorized for relation '{relation}' on object '{@object}'.");

    /// <summary>
    /// Error returned when a supplied relation tuple is malformed or violates the authorization model.
    /// </summary>
    /// <param name="reason">The validation reason.</param>
    /// <returns>A validation error describing the invalid tuple.</returns>
    public static Error InvalidTuple(string reason) =>
        Error.Validation("Authorization.InvalidTuple", $"The relation tuple is invalid: {reason}");

    /// <summary>
    /// Error returned when no authorization store exists for the supplied tenant.
    /// </summary>
    /// <param name="tenantId">The tenant whose store could not be located.</param>
    /// <returns>A not-found error describing the missing store.</returns>
    public static Error StoreNotFound(string tenantId) =>
        Error.NotFound(
            "Authorization.StoreNotFound",
            $"No authorization store was found for tenant '{tenantId}'.");

    /// <summary>
    /// Error returned when the authorization provider cannot be reached.
    /// </summary>
    /// <param name="reason">The underlying transport / connectivity reason.</param>
    /// <returns>An unavailable error describing the connectivity failure.</returns>
    public static Error ProviderUnreachable(string reason) =>
        Error.Unavailable(
            "Authorization.ProviderUnreachable",
            $"The authorization provider is unreachable: {reason}");
}
