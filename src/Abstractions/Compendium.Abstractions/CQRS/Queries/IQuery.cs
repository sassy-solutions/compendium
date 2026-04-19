// -----------------------------------------------------------------------
// <copyright file="IQuery.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CQRS.Queries;

/// <summary>
/// Interface for queries that return data without modifying system state.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQuery<out TResponse>
{
}
