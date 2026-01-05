// -----------------------------------------------------------------------
// <copyright file="IntervalSnapshotStrategy.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Infrastructure.EventSourcing;

/// <summary>
/// A snapshot strategy that takes snapshots at regular intervals based on event count.
/// </summary>
public sealed class IntervalSnapshotStrategy : ISnapshotStrategy
{
    private readonly int _snapshotInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalSnapshotStrategy"/> class.
    /// </summary>
    /// <param name="snapshotInterval">The number of events between snapshots (default: 100).</param>
    public IntervalSnapshotStrategy(int snapshotInterval = 100)
    {
        if (snapshotInterval <= 0)
        {
            throw new ArgumentException("Snapshot interval must be positive", nameof(snapshotInterval));
        }

        _snapshotInterval = snapshotInterval;
    }

    /// <inheritdoc />
    public bool ShouldTakeSnapshot(string aggregateId, long version, int eventCount)
    {
        // Take a snapshot if the total event count is a multiple of the interval
        // and we have at least the minimum number of events
        return eventCount > 0 && eventCount % _snapshotInterval == 0;
    }
}
