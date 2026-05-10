// -----------------------------------------------------------------------
// <copyright file="DeviceToken.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents a registered device token for a specific tenant and (optionally) user.
/// </summary>
public sealed record DeviceToken
{
    /// <summary>
    /// Gets the push provider that issued this token.
    /// </summary>
    public required PushProvider Provider { get; init; }

    /// <summary>
    /// Gets the opaque device token value as understood by the provider.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets the tenant identifier the token belongs to.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the optional user identifier the token is bound to.
    /// </summary>
    public string? UserId { get; init; }
}
