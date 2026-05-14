// -----------------------------------------------------------------------
// <copyright file="IRealtimeChannel.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Realtime.Models;

namespace Compendium.Abstractions.Realtime;

/// <summary>
/// Provider-agnostic realtime channel port. Implementations (Pusher, Ably, SignalR, …)
/// adapt this interface to their transport. Channel names MUST follow the tenant-prefixed
/// convention <c>{tenantId}:{scope}</c> so authorization can be enforced uniformly.
/// </summary>
public interface IRealtimeChannel
{
    /// <summary>
    /// Publishes <paramref name="payload"/> as <paramref name="eventName"/> on
    /// <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Tenant-prefixed channel name (<c>{tenantId}:{scope}</c>).</param>
    /// <param name="eventName">The event name to publish under.</param>
    /// <param name="payload">The payload object; serialization is adapter-defined.</param>
    /// <param name="opts">Publish options including tenant id, ttl, and headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or a typed realtime error.</returns>
    Task<Result> PublishAsync(
        string channel,
        string eventName,
        object payload,
        PublishOptions opts,
        CancellationToken ct);

    /// <summary>
    /// Authorizes <paramref name="subscriber"/> to subscribe to <paramref name="channel"/>.
    /// Implementations must reject when the subscriber's tenant does not match the channel prefix.
    /// </summary>
    /// <param name="subscriber">The subscriber requesting access.</param>
    /// <param name="channel">Tenant-prefixed channel name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> AuthorizeSubscriberAsync(
        SubscriberContext subscriber,
        string channel,
        CancellationToken ct);

    /// <summary>
    /// Returns the current presence roster for <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Tenant-prefixed presence channel name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<PresenceMember>>> GetPresenceAsync(
        string channel,
        CancellationToken ct);
}
