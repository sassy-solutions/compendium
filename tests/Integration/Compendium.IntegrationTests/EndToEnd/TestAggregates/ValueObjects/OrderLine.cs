// -----------------------------------------------------------------------
// <copyright file="OrderLine.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Primitives;

namespace Compendium.IntegrationTests.EndToEnd.TestAggregates.ValueObjects;

/// <summary>
/// Value object representing a line item in an order.
/// </summary>
public sealed class OrderLine : ValueObject
{
    public OrderLine(string lineId, string productId, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        }

        LineId = lineId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Gets the unique identifier for this order line.
    /// </summary>
    public string LineId { get; }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public string ProductId { get; }

    /// <summary>
    /// Gets the quantity ordered.
    /// </summary>
    public int Quantity { get; }

    /// <summary>
    /// Gets the unit price.
    /// </summary>
    public decimal UnitPrice { get; }

    /// <summary>
    /// Gets the total price for this line (Quantity * UnitPrice).
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LineId;
        yield return ProductId;
        yield return Quantity;
        yield return UnitPrice;
    }
}
