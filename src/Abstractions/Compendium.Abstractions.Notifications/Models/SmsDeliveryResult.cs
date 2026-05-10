// -----------------------------------------------------------------------
// <copyright file="SmsDeliveryResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents the result of submitting a single SMS message to a provider.
/// </summary>
public sealed record SmsDeliveryResult
{
    /// <summary>
    /// Gets the provider-assigned identifier for the submitted message (used for status callbacks).
    /// </summary>
    public required string ProviderMessageId { get; init; }

    /// <summary>
    /// Gets the current delivery status of the message as reported by the provider at submission time.
    /// </summary>
    public required SmsStatus Status { get; init; }

    /// <summary>
    /// Gets the number of SMS segments the body was split into (drives billing on most providers).
    /// </summary>
    public required int SegmentCount { get; init; }

    /// <summary>
    /// Gets an optional provider-reported cost hint (e.g. "0.0075 USD"). May be null when the provider does not return one.
    /// </summary>
    public string? CostHint { get; init; }
}
