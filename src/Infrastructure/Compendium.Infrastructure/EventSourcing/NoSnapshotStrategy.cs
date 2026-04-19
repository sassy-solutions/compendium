// -----------------------------------------------------------------------
// <copyright file="NoSnapshotStrategy.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
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
