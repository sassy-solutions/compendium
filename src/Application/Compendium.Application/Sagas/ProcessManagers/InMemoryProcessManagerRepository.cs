// -----------------------------------------------------------------------
// <copyright file="InMemoryProcessManagerRepository.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Abstractions.Sagas.Common;
using Compendium.Abstractions.Sagas.ProcessManagers;

namespace Compendium.Application.Sagas.ProcessManagers;

/// <summary>
/// In-memory <see cref="IProcessManagerRepository"/> intended for tests and quick
/// prototyping. State is process-local and lost on restart — production code should
/// use a durable adapter (e.g. <c>Compendium.Adapters.PostgreSQL</c>).
/// </summary>
public sealed class InMemoryProcessManagerRepository : IProcessManagerRepository
{
    private readonly ConcurrentDictionary<Guid, IProcessManager> _store = new();

    /// <inheritdoc />
    public Task<Result<IProcessManager>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var pm))
        {
            return Task.FromResult(Result.Success(pm));
        }

        return Task.FromResult(Result.Failure<IProcessManager>(
            Error.NotFound("ProcessManager.NotFound", $"Process manager {id} not found.")));
    }

    /// <inheritdoc />
    public Task<Result> SaveAsync(IProcessManager processManager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processManager);
        _store[processManager.Id] = processManager;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> UpdateStatusAsync(Guid id, SagaStatus status, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(id, out var pm))
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("ProcessManager.NotFound", $"Process manager {id} not found.")));
        }

        if (pm is IMutableProcessManager mutable)
        {
            mutable.TransitionTo(status);
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> UpdateStepStatusAsync(
        Guid processManagerId,
        Guid stepId,
        SagaStepStatus status,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(processManagerId, out var pm))
        {
            return Task.FromResult(Result.Failure(
                Error.NotFound("ProcessManager.NotFound", $"Process manager {processManagerId} not found.")));
        }

        if (pm is IMutableProcessManager mutable)
        {
            mutable.TransitionStep(stepId, status, errorMessage);
        }

        return Task.FromResult(Result.Success());
    }
}
