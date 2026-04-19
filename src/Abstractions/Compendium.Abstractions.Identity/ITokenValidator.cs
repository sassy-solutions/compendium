// -----------------------------------------------------------------------
// <copyright file="ITokenValidator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity.Models;

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provides operations for validating and introspecting tokens.
/// This interface supports both local JWT validation and remote token introspection.
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates a token and extracts its information.
    /// This method typically performs local validation using the issuer's public keys.
    /// </summary>
    /// <param name="token">The token to validate (typically a JWT).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the token information or an error if invalid.</returns>
    Task<Result<TokenInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Introspects a token by calling the identity provider's introspection endpoint.
    /// This method provides real-time validation status and is more accurate than local validation
    /// for checking if a token has been revoked.
    /// </summary>
    /// <param name="token">The token to introspect.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the introspection result or an error.</returns>
    Task<Result<TokenIntrospectionResult>> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default);
}
