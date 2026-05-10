// -----------------------------------------------------------------------
// <copyright file="PublishOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Models;

/// <summary>
/// Options for publishing a realtime message. <see cref="TenantId"/> is required so adapters
/// can enforce the tenant-prefixed channel naming convention (<c>{tenantId}:{scope}</c>).
/// </summary>
/// <param name="TenantId">The tenant the message is being published on behalf of.</param>
/// <param name="Ttl">Optional time-to-live for transient messages.</param>
/// <param name="Headers">Optional opaque headers forwarded to the provider.</param>
public sealed record PublishOptions(
    string TenantId,
    TimeSpan? Ttl = null,
    IReadOnlyDictionary<string, string>? Headers = null);
