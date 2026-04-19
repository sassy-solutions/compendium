// -----------------------------------------------------------------------
// <copyright file="Entity.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Rules;

namespace Compendium.Core.Domain.Primitives;

/// <summary>
/// Base class for all entities in the domain.
/// Provides identity, timestamps, and business rule validation.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IBusinessRule> _brokenRules = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
        CreatedAt = DateTimeOffset.UtcNow;
        ModifiedAt = CreatedAt;
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; private init; }

    /// <summary>
    /// Gets the timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Gets the timestamp when the entity was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this entity is transient (not yet persisted).
    /// </summary>
    public virtual bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default);

    /// <summary>
    /// Gets the collection of broken business rules.
    /// </summary>
    public IReadOnlyCollection<IBusinessRule> BrokenRules
    {
        get
        {
            lock (_lockObject)
            {
                return _brokenRules.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the entity has any broken business rules.
    /// </summary>
    public bool HasBrokenRules
    {
        get
        {
            lock (_lockObject)
            {
                return _brokenRules.Count > 0;
            }
        }
    }

    /// <summary>
    /// Checks a business rule and adds it to the broken rules collection if it's broken.
    /// </summary>
    /// <param name="rule">The business rule to check.</param>
    /// <exception cref="BusinessRuleValidationException">Thrown when the rule is broken.</exception>
    protected void CheckRule(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.IsBroken())
        {
            lock (_lockObject)
            {
                _brokenRules.Add(rule);
            }
            throw new BusinessRuleValidationException(rule);
        }
    }

    /// <summary>
    /// Updates the modified timestamp.
    /// </summary>
    protected void Touch()
    {
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Clears all broken business rules.
    /// </summary>
    protected void ClearBrokenRules()
    {
        lock (_lockObject)
        {
            _brokenRules.Clear();
        }
    }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>true if the specified entity is equal to the current entity; otherwise, false.</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (IsTransient || other.IsTransient)
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    /// <summary>
    /// Returns the hash code for this entity.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        return IsTransient ? base.GetHashCode() : HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}
