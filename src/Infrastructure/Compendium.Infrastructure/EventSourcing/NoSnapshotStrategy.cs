// -----------------------------------------------------------------------
// <copyright file="NoSnapshotStrategy.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.EventSourcing;

/// <summary>
/// A snapshot strategy that never takes snapshots.
/// This is the default strategy when no snapshot store is configured.
/// </summary>
public sealed class NoSnapshotStrategy : ISnapshotStrategy
{
    /// <inheritdoc />
    public bool ShouldTakeSnapshot(string aggregateId, long version, int eventCount)
    {
        return false;
    }
}
