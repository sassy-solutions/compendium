// -----------------------------------------------------------------------
// <copyright file="BatchSmsDeliveryResult.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Notifications.Models;

/// <summary>
/// Represents the aggregated result of submitting multiple SMS messages in a single batch.
/// </summary>
public sealed record BatchSmsDeliveryResult
{
    /// <summary>
    /// Gets the total number of messages that were accepted by the provider.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the total number of messages that the provider rejected at submission time.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the per-message delivery results that compose this aggregated result.
    /// </summary>
    public required IReadOnlyList<SmsDeliveryResult> Results { get; init; }
}
