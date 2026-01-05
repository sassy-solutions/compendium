// -----------------------------------------------------------------------
// <copyright file="EventMetadata.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// Metadata associated with an event for projection processing.
/// Contains positioning, timing, and context information.
/// </summary>
/// <param name="StreamId">The stream identifier the event belongs to.</param>
/// <param name="StreamPosition">The position of the event within its stream.</param>
/// <param name="GlobalPosition">The global position of the event across all streams.</param>
/// <param name="Timestamp">The timestamp when the event was stored.</param>
/// <param name="UserId">The identifier of the user who triggered the event (if applicable).</param>
/// <param name="TenantId">The tenant identifier for multi-tenant scenarios (if applicable).</param>
/// <param name="Headers">Additional headers and metadata associated with the event.</param>
public record EventMetadata(
    string StreamId,
    long StreamPosition,
    long GlobalPosition,
    DateTime Timestamp,
    string? UserId,
    string? TenantId,
    Dictionary<string, object>? Headers
);
