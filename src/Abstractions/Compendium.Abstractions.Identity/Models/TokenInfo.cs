// -----------------------------------------------------------------------
// <copyright file="TokenInfo.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Identity.Models;

/// <summary>
/// Represents information extracted from a validated token.
/// </summary>
public sealed record TokenInfo
{
    /// <summary>
    /// Gets or initializes the subject (user ID) from the token.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets or initializes the issuer of the token.
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// Gets or initializes the audience(s) of the token.
    /// </summary>
    public IReadOnlyList<string>? Audience { get; init; }

    /// <summary>
    /// Gets or initializes when the token was issued.
    /// </summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets or initializes the email claim from the token.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or initializes whether the email is verified.
    /// </summary>
    public bool? EmailVerified { get; init; }

    /// <summary>
    /// Gets or initializes the name claim from the token.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the organization ID from the token (for multi-tenancy).
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets or initializes the roles from the token.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Gets or initializes the scopes granted in the token.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>
    /// Gets or initializes all claims from the token.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Claims { get; init; }

    /// <summary>
    /// Gets a value indicating whether the token has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}

/// <summary>
/// Represents the result of a token introspection request.
/// </summary>
public sealed record TokenIntrospectionResult
{
    /// <summary>
    /// Gets or initializes whether the token is active and valid.
    /// </summary>
    public required bool Active { get; init; }

    /// <summary>
    /// Gets or initializes the token info if the token is active.
    /// </summary>
    public TokenInfo? TokenInfo { get; init; }

    /// <summary>
    /// Gets or initializes the token type (e.g., "Bearer").
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// Gets or initializes the client ID that the token was issued for.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets or initializes the reason why the token is not active, if applicable.
    /// </summary>
    public string? InactiveReason { get; init; }
}
