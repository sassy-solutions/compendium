// -----------------------------------------------------------------------
// <copyright file="IdentityErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provides standard error definitions for identity operations.
/// </summary>
public static class IdentityErrors
{
    /// <summary>
    /// Error returned when a user is not found.
    /// </summary>
    public static Error UserNotFound(string userId) =>
        Error.NotFound("Identity.UserNotFound", $"User with ID '{userId}' was not found.");

    /// <summary>
    /// Error returned when a user with the specified email is not found.
    /// </summary>
    public static Error UserNotFoundByEmail(string email) =>
        Error.NotFound("Identity.UserNotFoundByEmail", $"User with email '{email}' was not found.");

    /// <summary>
    /// Error returned when a user with the specified email already exists.
    /// </summary>
    public static Error UserAlreadyExists(string email) =>
        Error.Conflict("Identity.UserAlreadyExists", $"A user with email '{email}' already exists.");

    /// <summary>
    /// Error returned when an organization is not found.
    /// </summary>
    public static Error OrganizationNotFound(string organizationId) =>
        Error.NotFound("Identity.OrganizationNotFound", $"Organization with ID '{organizationId}' was not found.");

    /// <summary>
    /// Error returned when an organization with the specified name already exists.
    /// </summary>
    public static Error OrganizationAlreadyExists(string name) =>
        Error.Conflict("Identity.OrganizationAlreadyExists", $"An organization with name '{name}' already exists.");

    /// <summary>
    /// Error returned when a user is already a member of an organization.
    /// </summary>
    public static Error UserAlreadyMember(string userId, string organizationId) =>
        Error.Conflict("Identity.UserAlreadyMember", $"User '{userId}' is already a member of organization '{organizationId}'.");

    /// <summary>
    /// Error returned when a user is not a member of an organization.
    /// </summary>
    public static Error UserNotMember(string userId, string organizationId) =>
        Error.NotFound("Identity.UserNotMember", $"User '{userId}' is not a member of organization '{organizationId}'.");

    /// <summary>
    /// Error returned when a token is invalid.
    /// </summary>
    public static readonly Error InvalidToken =
        Error.Unauthorized("Identity.InvalidToken", "The provided token is invalid.");

    /// <summary>
    /// Error returned when a token has expired.
    /// </summary>
    public static readonly Error TokenExpired =
        Error.Unauthorized("Identity.TokenExpired", "The provided token has expired.");

    /// <summary>
    /// Error returned when a token has been revoked.
    /// </summary>
    public static readonly Error TokenRevoked =
        Error.Unauthorized("Identity.TokenRevoked", "The provided token has been revoked.");

    /// <summary>
    /// Error returned when the identity provider is unavailable.
    /// </summary>
    public static readonly Error ProviderUnavailable =
        Error.Unavailable("Identity.ProviderUnavailable", "The identity provider is currently unavailable.");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static readonly Error RateLimitExceeded =
        Error.TooManyRequests("Identity.RateLimitExceeded", "Rate limit exceeded. Please try again later.");

    /// <summary>
    /// Error returned when the email format is invalid.
    /// </summary>
    public static Error InvalidEmail(string email) =>
        Error.Validation("Identity.InvalidEmail", $"The email '{email}' is not valid.");

    /// <summary>
    /// Error returned when required tenant context is missing.
    /// </summary>
    public static readonly Error TenantContextRequired =
        Error.Validation("Identity.TenantContextRequired", "Tenant context is required for this operation.");
}
