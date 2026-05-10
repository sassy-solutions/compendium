// -----------------------------------------------------------------------
// <copyright file="AuthorizationRequest.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization.Models;

/// <summary>
/// Represents a relationship-based authorization check request.
/// Asks whether <paramref name="Subject"/> has <paramref name="Relation"/> with <paramref name="Object"/>
/// in the store identified by <paramref name="TenantId"/>, optionally augmented with <paramref name="ContextualTuples"/>
/// that exist only for the duration of the check.
/// </summary>
/// <param name="Subject">The subject under evaluation (typically <c>user:&lt;id&gt;</c> or a userset reference).</param>
/// <param name="Relation">The relation to check, as defined in the authorization model.</param>
/// <param name="Object">The object the relation is checked against (typically <c>&lt;type&gt;:&lt;id&gt;</c>).</param>
/// <param name="TenantId">The tenant whose authorization store should answer the check.</param>
/// <param name="ContextualTuples">
/// Optional in-flight tuples added to the evaluation context but not persisted to the store. Use for
/// "what-if" evaluations or for relationships that exist only within the request scope.
/// </param>
public sealed record AuthorizationRequest(
    string Subject,
    string Relation,
    string Object,
    string TenantId,
    IReadOnlyList<RelationTuple>? ContextualTuples = null);
