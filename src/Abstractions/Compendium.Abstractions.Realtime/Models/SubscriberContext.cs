// -----------------------------------------------------------------------
// <copyright file="SubscriberContext.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Models;

/// <summary>
/// Context describing a subscriber attempting to join a realtime channel.
/// Adapters use this to authorize the subscription and bind presence metadata.
/// </summary>
/// <param name="UserId">The subscriber's user identifier.</param>
/// <param name="TenantId">The subscriber's tenant; must match the channel prefix.</param>
/// <param name="Info">Optional presence info to attach for presence channels.</param>
public sealed record SubscriberContext(
    string UserId,
    string TenantId,
    IReadOnlyDictionary<string, object>? Info = null);
