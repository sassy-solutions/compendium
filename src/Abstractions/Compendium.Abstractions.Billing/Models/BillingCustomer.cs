// -----------------------------------------------------------------------
// <copyright file="BillingCustomer.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Billing.Models;

/// <summary>
/// Represents a customer in the billing system.
/// </summary>
public sealed record BillingCustomer
{
    /// <summary>
    /// Gets or initializes the unique identifier of the customer.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the store ID this customer belongs to.
    /// </summary>
    public string? StoreId { get; init; }

    /// <summary>
    /// Gets or initializes the customer's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the customer's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the customer's city.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Gets or initializes the customer's region/state.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets or initializes the customer's country code.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Gets or initializes the total revenue from this customer in cents.
    /// </summary>
    public int? TotalRevenueCents { get; init; }

    /// <summary>
    /// Gets or initializes the currency code for total revenue.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Gets or initializes the tenant ID associated with this customer.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or initializes the user ID associated with this customer.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata associated with the customer.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }

    /// <summary>
    /// Gets or initializes when the customer was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or initializes when the customer was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Represents a request to create or update a customer.
/// </summary>
public sealed record UpsertCustomerRequest
{
    /// <summary>
    /// Gets or initializes the customer's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or initializes the customer's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or initializes the customer's city.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Gets or initializes the customer's region/state.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets or initializes the customer's country code.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Gets or initializes the tenant ID to associate with this customer.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or initializes the user ID to associate with this customer.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or initializes custom metadata for the customer.
    /// </summary>
    public IReadOnlyDictionary<string, object>? CustomData { get; init; }
}
