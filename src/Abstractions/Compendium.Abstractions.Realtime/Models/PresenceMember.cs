// -----------------------------------------------------------------------
// <copyright file="PresenceMember.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Realtime.Models;

/// <summary>
/// A member currently present on a realtime presence channel.
/// </summary>
/// <param name="Id">The provider-stable member identifier.</param>
/// <param name="Info">Arbitrary provider-supplied presence info for the member.</param>
public sealed record PresenceMember(
    string Id,
    IReadOnlyDictionary<string, object> Info);
