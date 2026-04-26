// -----------------------------------------------------------------------
// <copyright file="IEventChoreography.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Marker interface identifying a class as a participant in an event-driven choreography
/// saga (the "Event Saga" flavor: no central orchestrator; services react to integration
/// events and publish further events).
/// </summary>
/// <remarks>
/// Choose choreography over a <c>ProcessManager</c> when:
/// <list type="bullet">
///   <item><description>The workflow spans multiple bounded contexts that should remain loosely coupled.</description></item>
///   <item><description>Eventual consistency is acceptable.</description></item>
///   <item><description>You'd rather not introduce a central component that knows about every step.</description></item>
/// </list>
/// In contrast, choose <c>IProcessManager</c> when the workflow is owned by one bounded
/// context and you need centralized step tracking and compensation.
/// </remarks>
public interface IEventChoreography
{
}
