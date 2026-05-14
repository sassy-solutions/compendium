// -----------------------------------------------------------------------
// <copyright file="RelationTuple.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Authorization.Models;

/// <summary>
/// Represents a relationship tuple in a Zanzibar-style relationship-based authorization store.
/// A tuple expresses the fact that <paramref name="Subject"/> has <paramref name="Relation"/> with <paramref name="Object"/>.
/// </summary>
/// <param name="Subject">The subject of the relationship (typically <c>user:&lt;id&gt;</c> or a userset reference).</param>
/// <param name="Relation">The name of the relation as defined in the authorization model.</param>
/// <param name="Object">The object of the relationship (typically <c>&lt;type&gt;:&lt;id&gt;</c>).</param>
public sealed record RelationTuple(string Subject, string Relation, string Object);
