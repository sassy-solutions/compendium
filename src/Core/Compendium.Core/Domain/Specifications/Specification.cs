// -----------------------------------------------------------------------
// <copyright file="Specification.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Frozen;
using System.Linq.Expressions;

namespace Compendium.Core.Domain.Specifications;

/// <summary>
/// Base class for specifications providing common functionality and composition methods.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly Lazy<Func<T, bool>> _compiledCriteria;

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class.
    /// </summary>
    /// <param name="criteria">The criteria expression.</param>
    protected Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
        _compiledCriteria = new Lazy<Func<T, bool>>(() => criteria.Compile());
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>> Criteria { get; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.ToFrozenSet().ToList().AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings.ToFrozenSet().ToList().AsReadOnly();

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled => Take.HasValue;

    /// <inheritdoc />
    public virtual bool IsSatisfiedBy(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledCriteria.Value(entity);
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> AddInclude(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);
        _includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Adds an include string for eager loading.
    /// </summary>
    /// <param name="includeString">The include string.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> AddInclude(string includeString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includeString);
        _includeStrings.Add(includeString);
        return this;
    }

    /// <summary>
    /// Applies ordering to the specification.
    /// </summary>
    /// <param name="orderByExpression">The order by expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByExpression);
        OrderBy = orderByExpression;
        return this;
    }

    /// <summary>
    /// Applies descending ordering to the specification.
    /// </summary>
    /// <param name="orderByDescendingExpression">The order by descending expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        ArgumentNullException.ThrowIfNull(orderByDescendingExpression);
        OrderByDescending = orderByDescendingExpression;
        return this;
    }

    /// <summary>
    /// Applies grouping to the specification.
    /// </summary>
    /// <param name="groupByExpression">The group by expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        ArgumentNullException.ThrowIfNull(groupByExpression);
        GroupBy = groupByExpression;
        return this;
    }

    /// <summary>
    /// Applies paging to the specification.
    /// </summary>
    /// <param name="skip">The number of entities to skip.</param>
    /// <param name="take">The number of entities to take.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected virtual Specification<T> ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        Skip = skip;
        Take = take;
        return this;
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new specification representing the logical AND of both specifications.</returns>
    public Specification<T> And(ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new specification representing the logical OR of both specifications.</returns>
    public Specification<T> Or(ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Creates a specification that represents the logical NOT of this specification.
    /// </summary>
    /// <returns>A new specification representing the logical NOT of this specification.</returns>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

/// <summary>
/// Specification that represents the logical AND of two specifications.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
internal sealed class AndSpecification<T> : Specification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(CombineExpressions(left.Criteria, right.Criteria, Expression.AndAlso))
    {
    }

    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        var combined = combiner(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }
}

/// <summary>
/// Specification that represents the logical OR of two specifications.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
internal sealed class OrSpecification<T> : Specification<T>
{
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(CombineExpressions(left.Criteria, right.Criteria, Expression.OrElse))
    {
    }

    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        var combined = combiner(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }
}

/// <summary>
/// Specification that represents the logical NOT of a specification.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
internal sealed class NotSpecification<T> : Specification<T>
{
    public NotSpecification(ISpecification<T> specification)
        : base(Expression.Lambda<Func<T, bool>>(
            Expression.Not(specification.Criteria.Body),
            specification.Criteria.Parameters))
    {
    }
}

/// <summary>
/// Expression visitor that replaces parameter expressions.
/// </summary>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}
