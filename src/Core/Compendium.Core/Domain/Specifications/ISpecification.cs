// -----------------------------------------------------------------------
// <copyright file="ISpecification.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;

namespace Compendium.Core.Domain.Specifications;

/// <summary>
/// Represents a specification that can be used to filter entities.
/// Specifications encapsulate query logic and can be combined using logical operators.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the expression that represents the specification criteria.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Gets the include expressions for eager loading related entities.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the include string expressions for eager loading related entities.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression for sorting.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression for sorting.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the group by expression for grouping.
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the number of entities to take (for paging).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of entities to skip (for paging).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets a value indicating whether paging is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies the specification.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    /// <returns>true if the entity satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T entity);
}
