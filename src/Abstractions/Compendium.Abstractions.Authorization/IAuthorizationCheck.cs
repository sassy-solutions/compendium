// -----------------------------------------------------------------------
// <copyright file="IAuthorizationCheck.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Authorization.Models;

namespace Compendium.Abstractions.Authorization;

/// <summary>
/// Provides operations for relationship-based authorization (ReBAC) against a Zanzibar-style store.
/// This interface is provider-agnostic and can be implemented by adapters targeting
/// OpenFGA, SpiceDB, AuthZed, and similar relationship-based authorization services.
/// Each tenant has its own authorization store; tenant scoping is mandatory on every call.
/// </summary>
public interface IAuthorizationCheck
{
    /// <summary>
    /// Evaluates whether the supplied subject has the requested relation with the requested object.
    /// </summary>
    /// <param name="request">The authorization request, including tenant and optional contextual tuples.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A result containing <see langword="true"/> when the subject is authorized, <see langword="false"/> otherwise,
    /// or an error when the store cannot evaluate the check.
    /// </returns>
    Task<Result<bool>> CheckAsync(AuthorizationRequest request, CancellationToken ct);

    /// <summary>
    /// Lists the object identifiers of <paramref name="objectType"/> that the supplied subject has
    /// the requested relation with.
    /// </summary>
    /// <param name="subject">The subject whose objects should be listed (typically <c>user:&lt;id&gt;</c>).</param>
    /// <param name="relation">The relation to enumerate.</param>
    /// <param name="objectType">The type of object to enumerate (without the <c>&lt;id&gt;</c> suffix).</param>
    /// <param name="tenantId">The tenant whose authorization store should answer the request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the matching object identifiers or an error.</returns>
    Task<Result<IReadOnlyList<string>>> ListObjectsAsync(
        string subject,
        string relation,
        string objectType,
        string tenantId,
        CancellationToken ct);

    /// <summary>
    /// Writes and / or deletes relationship tuples in the tenant's authorization store atomically.
    /// </summary>
    /// <param name="writes">Tuples to insert; may be empty.</param>
    /// <param name="deletes">Tuples to remove; may be empty.</param>
    /// <param name="tenantId">The tenant whose authorization store should be mutated.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A success result when the mutation has been applied, or an error otherwise.</returns>
    Task<Result> WriteAsync(
        IEnumerable<RelationTuple> writes,
        IEnumerable<RelationTuple> deletes,
        string tenantId,
        CancellationToken ct);
}
