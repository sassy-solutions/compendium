// -----------------------------------------------------------------------
// <copyright file="ISagaInstance.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Common;

/// <summary>
/// Marker interface for any persisted saga instance, regardless of flavor.
/// </summary>
/// <remarks>
/// Compendium distinguishes two saga flavors:
/// <list type="bullet">
///   <item><description>
///     <b>Orchestration / DDD</b> — implemented by <c>IProcessManager</c>: a stateful
///     coordinator that groups operations across multiple aggregates within a bounded
///     context. Owns its state, drives the workflow with explicit steps, supports
///     compensation. Use this when the workflow logic is centralized.
///   </description></item>
///   <item><description>
///     <b>Choreography / Event-driven</b> — implemented by <c>IEventChoreography</c>:
///     no central coordinator. Each handler reacts to integration events and
///     publishes its own. State lives in the aggregates that emit the events. Use
///     this for cross-bounded-context workflows that can tolerate eventual consistency.
///   </description></item>
/// </list>
/// See <c>docs/sagas.md</c> for the decision tree.
/// </remarks>
public interface ISagaInstance
{
    /// <summary>
    /// Gets the unique identifier of the saga instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the current status of the saga.
    /// </summary>
    SagaStatus Status { get; }

    /// <summary>
    /// Gets the timestamp when the saga was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when the saga reached a terminal state
    /// (<see cref="SagaStatus.Completed"/> or <see cref="SagaStatus.Compensated"/>),
    /// or <c>null</c> if still in progress.
    /// </summary>
    DateTime? CompletedAt { get; }
}
