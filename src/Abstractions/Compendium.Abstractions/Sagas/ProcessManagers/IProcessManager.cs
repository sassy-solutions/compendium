// -----------------------------------------------------------------------
// <copyright file="IProcessManager.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Sagas.Common;

namespace Compendium.Abstractions.Sagas.ProcessManagers;

/// <summary>
/// Represents a DDD-style orchestration saga ("Process Manager" in Vaughn Vernon's
/// terminology) — a stateful coordinator that groups operations across multiple
/// aggregates within a bounded context, drives them through explicit steps, and
/// performs compensation on failure.
/// </summary>
/// <remarks>
/// Choose a Process Manager over <c>IEventChoreography</c> when:
/// <list type="bullet">
///   <item><description>The workflow is owned by a single bounded context.</description></item>
///   <item><description>You need centralized visibility of progress (steps, status).</description></item>
///   <item><description>The compensation logic is non-trivial and benefits from a single owner.</description></item>
/// </list>
/// </remarks>
public interface IProcessManager : ISagaInstance
{
    /// <summary>
    /// Gets the read-only list of steps that comprise this process manager.
    /// Steps execute in <see cref="SagaStep.Order"/>; compensation runs in reverse.
    /// </summary>
    IReadOnlyList<SagaStep> Steps { get; }
}

/// <summary>
/// Represents a process manager whose internal state is exposed via a strongly-typed
/// state object. This is the primary base contract users will implement (typically by
/// deriving from <c>ProcessManager&lt;TState&gt;</c> in <c>Compendium.Application</c>).
/// </summary>
/// <typeparam name="TState">The type of the state carried by the process manager. Must be a class with a parameterless constructor — it is rehydrated from persistence.</typeparam>
public interface IProcessManager<out TState> : IProcessManager
    where TState : class, new()
{
    /// <summary>
    /// Gets the current state of the process manager. Mutating this object is the
    /// process manager's job; the orchestrator only reads it for persistence.
    /// </summary>
    TState State { get; }
}
