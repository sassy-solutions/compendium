// -----------------------------------------------------------------------
// <copyright file="VectorFilter.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.VectorStore.Models;

/// <summary>
/// Identifies the kind of operation represented by a <see cref="VectorFilter"/> node.
/// </summary>
public enum VectorFilterKind
{
    /// <summary>Equality on a metadata field.</summary>
    Eq = 0,

    /// <summary>Inequality on a metadata field.</summary>
    Ne = 1,

    /// <summary>Membership of a metadata field value in a finite set.</summary>
    In = 2,

    /// <summary>Inclusive/exclusive range comparison on a metadata field.</summary>
    Range = 3,

    /// <summary>Logical conjunction over child filters.</summary>
    And = 4,

    /// <summary>Logical disjunction over child filters.</summary>
    Or = 5,
}

/// <summary>
/// Represents a tenant-aware metadata filter applied to a vector search.
/// Instances are immutable and built through the static factories.
/// </summary>
public sealed class VectorFilter
{
    private VectorFilter(
        VectorFilterKind kind,
        string? field,
        object? value,
        IReadOnlyList<object>? values,
        object? rangeMin,
        object? rangeMax,
        bool rangeMinInclusive,
        bool rangeMaxInclusive,
        IReadOnlyList<VectorFilter>? children,
        string? tenantId)
    {
        Kind = kind;
        Field = field;
        Value = value;
        Values = values;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        RangeMinInclusive = rangeMinInclusive;
        RangeMaxInclusive = rangeMaxInclusive;
        Children = children;
        TenantId = tenantId;
    }

    /// <summary>Gets the kind of filter node.</summary>
    public VectorFilterKind Kind { get; }

    /// <summary>Gets the metadata field this filter targets, when applicable.</summary>
    public string? Field { get; }

    /// <summary>Gets the value compared by the filter, when applicable (<see cref="VectorFilterKind.Eq"/> / <see cref="VectorFilterKind.Ne"/>).</summary>
    public object? Value { get; }

    /// <summary>Gets the values compared by the filter, when applicable (<see cref="VectorFilterKind.In"/>).</summary>
    public IReadOnlyList<object>? Values { get; }

    /// <summary>Gets the inclusive/exclusive lower bound of a range filter, when applicable.</summary>
    public object? RangeMin { get; }

    /// <summary>Gets the inclusive/exclusive upper bound of a range filter, when applicable.</summary>
    public object? RangeMax { get; }

    /// <summary>Gets a value indicating whether the lower bound is inclusive.</summary>
    public bool RangeMinInclusive { get; }

    /// <summary>Gets a value indicating whether the upper bound is inclusive.</summary>
    public bool RangeMaxInclusive { get; }

    /// <summary>Gets the children of an <see cref="VectorFilterKind.And"/> / <see cref="VectorFilterKind.Or"/> node.</summary>
    public IReadOnlyList<VectorFilter>? Children { get; }

    /// <summary>Gets the tenant scope attached to this filter, if any.</summary>
    public string? TenantId { get; }

    /// <summary>
    /// Builds an equality filter on a metadata field.
    /// </summary>
    public static VectorFilter Eq(string field, object value)
    {
        EnsureField(field);
        ArgumentNullException.ThrowIfNull(value);
        return new VectorFilter(VectorFilterKind.Eq, field, value, null, null, null, true, true, null, null);
    }

    /// <summary>
    /// Builds a non-equality filter on a metadata field.
    /// </summary>
    public static VectorFilter Ne(string field, object value)
    {
        EnsureField(field);
        ArgumentNullException.ThrowIfNull(value);
        return new VectorFilter(VectorFilterKind.Ne, field, value, null, null, null, true, true, null, null);
    }

    /// <summary>
    /// Builds a membership filter on a metadata field over a finite set of values.
    /// </summary>
    public static VectorFilter In(string field, IEnumerable<object> values)
    {
        EnsureField(field);
        ArgumentNullException.ThrowIfNull(values);
        var list = values.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("In() requires at least one value.", nameof(values));
        }

        return new VectorFilter(VectorFilterKind.In, field, null, list, null, null, true, true, null, null);
    }

    /// <summary>
    /// Builds a range filter on a metadata field. At least one of <paramref name="min"/> or <paramref name="max"/> must be supplied.
    /// </summary>
    public static VectorFilter Range(
        string field,
        object? min,
        object? max,
        bool minInclusive = true,
        bool maxInclusive = true)
    {
        EnsureField(field);
        if (min is null && max is null)
        {
            throw new ArgumentException("Range() requires at least one bound.", nameof(min));
        }

        return new VectorFilter(VectorFilterKind.Range, field, null, null, min, max, minInclusive, maxInclusive, null, null);
    }

    /// <summary>
    /// Combines filters with a logical AND.
    /// </summary>
    public static VectorFilter And(params VectorFilter[] filters)
    {
        return Combine(VectorFilterKind.And, filters);
    }

    /// <summary>
    /// Combines filters with a logical OR.
    /// </summary>
    public static VectorFilter Or(params VectorFilter[] filters)
    {
        return Combine(VectorFilterKind.Or, filters);
    }

    /// <summary>
    /// Returns a new filter scoped to the supplied tenant. The original filter is not modified.
    /// </summary>
    public VectorFilter ForTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id must be a non-empty string.", nameof(tenantId));
        }

        return new VectorFilter(Kind, Field, Value, Values, RangeMin, RangeMax, RangeMinInclusive, RangeMaxInclusive, Children, tenantId);
    }

    private static VectorFilter Combine(VectorFilterKind kind, VectorFilter[] filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        if (filters.Length == 0)
        {
            throw new ArgumentException("At least one child filter is required.", nameof(filters));
        }

        foreach (var f in filters)
        {
            if (f is null)
            {
                throw new ArgumentException("Child filter cannot be null.", nameof(filters));
            }
        }

        return new VectorFilter(kind, null, null, null, null, null, true, true, filters, null);
    }

    private static void EnsureField(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Field name must be a non-empty string.", nameof(field));
        }
    }
}
