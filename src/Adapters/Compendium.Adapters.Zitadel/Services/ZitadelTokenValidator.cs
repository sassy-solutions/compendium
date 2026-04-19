// -----------------------------------------------------------------------
// <copyright file="ZitadelTokenValidator.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Zitadel.Configuration;
using Compendium.Adapters.Zitadel.Http;
using Compendium.Adapters.Zitadel.Http.Models;

namespace Compendium.Adapters.Zitadel.Services;

/// <summary>
/// Implements token validation using Zitadel's introspection endpoint.
/// </summary>
internal sealed class ZitadelTokenValidator : ITokenValidator
{
    private readonly ZitadelHttpClient _httpClient;
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelTokenValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZitadelTokenValidator"/> class.
    /// </summary>
    public ZitadelTokenValidator(
        ZitadelHttpClient httpClient,
        IOptions<ZitadelOptions> options,
        ILogger<ZitadelTokenValidator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<TokenInfo>> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        _logger.LogDebug("Validating token via introspection");

        var result = await _httpClient.IntrospectTokenAsync(token, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var introspection = result.Value;

        if (!introspection.Active)
        {
            _logger.LogDebug("Token is not active");
            return IdentityErrors.InvalidToken;
        }

        var tokenInfo = MapToTokenInfo(introspection);

        if (tokenInfo.IsExpired)
        {
            _logger.LogDebug("Token has expired");
            return IdentityErrors.TokenExpired;
        }

        return tokenInfo;
    }

    /// <inheritdoc />
    public async Task<Result<TokenIntrospectionResult>> IntrospectTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        _logger.LogDebug("Introspecting token");

        var result = await _httpClient.IntrospectTokenAsync(token, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var introspection = result.Value;

        return new TokenIntrospectionResult
        {
            Active = introspection.Active,
            TokenInfo = introspection.Active ? MapToTokenInfo(introspection) : null,
            TokenType = introspection.TokenType,
            ClientId = introspection.ClientId,
            InactiveReason = !introspection.Active ? DetermineInactiveReason(introspection) : null
        };
    }

    private static TokenInfo MapToTokenInfo(ZitadelIntrospectionResponse introspection)
    {
        var audiences = ParseAudience(introspection.Aud);
        var roles = ParseRoles(introspection.Roles);

        return new TokenInfo
        {
            Subject = introspection.Sub ?? string.Empty,
            Issuer = introspection.Iss ?? string.Empty,
            Audience = audiences,
            IssuedAt = introspection.Iat.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(introspection.Iat.Value)
                : DateTimeOffset.MinValue,
            ExpiresAt = introspection.Exp.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(introspection.Exp.Value)
                : DateTimeOffset.MaxValue,
            Email = introspection.Email,
            EmailVerified = introspection.EmailVerified,
            Name = introspection.Name,
            OrganizationId = introspection.OrgId ?? introspection.ResourceOwnerId,
            Roles = roles,
            Scopes = ParseScopes(introspection.Scope)
        };
    }

    private static List<string>? ParseAudience(object? aud)
    {
        if (aud is null)
        {
            return null;
        }

        if (aud is string audString)
        {
            return new List<string> { audString };
        }

        if (aud is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return new List<string> { jsonElement.GetString() ?? string.Empty };
            }

            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? string.Empty)
                    .ToList();
            }
        }

        return null;
    }

    private static List<string>? ParseRoles(Dictionary<string, Dictionary<string, string>>? rolesDict)
    {
        if (rolesDict is null || rolesDict.Count == 0)
        {
            return null;
        }

        return rolesDict.Keys.ToList();
    }

    private static List<string>? ParseScopes(string? scope)
    {
        if (string.IsNullOrEmpty(scope))
        {
            return null;
        }

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string DetermineInactiveReason(ZitadelIntrospectionResponse introspection)
    {
        if (introspection.Exp.HasValue &&
            DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(introspection.Exp.Value))
        {
            return "Token has expired";
        }

        return "Token is invalid or has been revoked";
    }
}
