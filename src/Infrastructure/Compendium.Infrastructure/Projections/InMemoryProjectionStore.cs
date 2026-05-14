// -----------------------------------------------------------------------
// <copyright file="InMemoryProjectionStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text.Json;

namespace Compendium.Infrastructure.Projections;

/// <summary>
/// In-memory implementation of <see cref="IProjectionStore"/> for testing and
/// InMemory-backed framework E2E scenarios.
/// </summary>
/// <remarks>
/// Matches <c>PostgreSqlProjectionStore</c> semantics for checkpoint persistence,
/// snapshot storage, and projection state. Snapshots are round-tripped through
/// <see cref="JsonSerializer"/> so projection types that already serialize for
/// PG keep the same shape contract.
/// </remarks>
public sealed class InMemoryProjectionStore : IProjectionStore
{
    private readonly ConcurrentDictionary<string, long> _checkpoints = new();
    private readonly ConcurrentDictionary<string, string> _snapshots = new();
    private readonly ConcurrentDictionary<string, ProjectionState> _states = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <inheritdoc />
    public Task SaveCheckpointAsync(string projectionName, long position, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        _checkpoints[projectionName] = position;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long?> GetCheckpointAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        return Task.FromResult(_checkpoints.TryGetValue(projectionName, out var pos) ? pos : (long?)null);
    }

    /// <inheritdoc />
    public Task SaveSnapshotAsync<TProjection>(TProjection projection, CancellationToken cancellationToken = default)
        where TProjection : IProjection
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(projection.ProjectionName);

        var json = JsonSerializer.Serialize(projection, projection.GetType(), _jsonOptions);
        _snapshots[projection.ProjectionName] = json;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TProjection?> LoadSnapshotAsync<TProjection>(
        string projectionName,
        CancellationToken cancellationToken = default)
        where TProjection : IProjection
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        if (!_snapshots.TryGetValue(projectionName, out var json))
        {
            return Task.FromResult<TProjection?>(default);
        }

        var snapshot = JsonSerializer.Deserialize<TProjection>(json, _jsonOptions);
        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task DeleteProjectionDataAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        _checkpoints.TryRemove(projectionName, out _);
        _snapshots.TryRemove(projectionName, out _);
        _states.TryRemove(projectionName, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProjectionState?> GetProjectionStateAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        return Task.FromResult(_states.TryGetValue(projectionName, out var state) ? state : null);
    }

    /// <inheritdoc />
    public Task SaveProjectionStateAsync(ProjectionState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(state.ProjectionName);

        _states[state.ProjectionName] = state;
        return Task.CompletedTask;
    }

    /// <summary>Clears all stored projection data. Test-only helper.</summary>
    public void Clear()
    {
        _checkpoints.Clear();
        _snapshots.Clear();
        _states.Clear();
    }
}
