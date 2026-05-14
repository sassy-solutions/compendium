// -----------------------------------------------------------------------
// <copyright file="WebhookErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Webhooks;

/// <summary>
/// Provides standard error definitions for webhook fan-out operations.
/// </summary>
public static class WebhookErrors
{
    /// <summary>
    /// Gets the error code prefix for webhook errors.
    /// </summary>
    public const string Prefix = "Webhook";

    /// <summary>
    /// Error returned when the requested endpoint cannot be located for the supplied tenant.
    /// </summary>
    public static Error EndpointNotFound(string endpointId) =>
        Error.NotFound(
            $"{Prefix}.EndpointNotFound",
            $"Webhook endpoint '{endpointId}' was not found.");

    /// <summary>
    /// Error returned when the endpoint URL is missing, malformed, or uses an unsupported scheme.
    /// </summary>
    public static Error InvalidUrl(string url) =>
        Error.Validation(
            $"{Prefix}.InvalidUrl",
            $"The webhook URL '{url}' is invalid or uses an unsupported scheme.");

    /// <summary>
    /// Error returned when an endpoint requires a signing secret but none was supplied.
    /// </summary>
    public static Error SigningSecretMissing(string endpointId) =>
        Error.Validation(
            $"{Prefix}.SigningSecretMissing",
            $"Webhook endpoint '{endpointId}' requires a signing secret but none was supplied.");

    /// <summary>
    /// Error returned when the underlying webhook provider cannot be reached.
    /// </summary>
    public static Error ProviderUnreachable(string reason) =>
        Error.Unavailable(
            $"{Prefix}.ProviderUnreachable",
            $"The webhook provider is unreachable: {reason}");

    /// <summary>
    /// Error returned when the request rate limit imposed by the provider has been exceeded.
    /// </summary>
    public static Error RateLimited(string reason) =>
        Error.TooManyRequests(
            $"{Prefix}.RateLimited",
            $"Webhook rate limit exceeded: {reason}");
}
