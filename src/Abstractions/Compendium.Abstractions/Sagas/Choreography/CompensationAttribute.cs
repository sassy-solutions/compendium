// -----------------------------------------------------------------------
// <copyright file="CompensationAttribute.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Sagas.Choreography;

/// <summary>
/// Marks a choreography handler class (or one of its <c>HandleAsync</c> overloads) as a
/// compensation handler — i.e. it reacts to a "negative" event (refund, cancellation,
/// failure) that semantically rolls back a previous "positive" event.
/// </summary>
/// <remarks>
/// This attribute is metadata only. The framework uses it for telemetry (tagging spans
/// with <c>compensation = true</c>) and for the <c>docs/sagas.md</c> generator. It does
/// not change runtime behavior.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CompensationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationAttribute"/> class.
    /// </summary>
    /// <param name="compensatesEvent">The forward event type that this handler compensates.</param>
    public CompensationAttribute(Type compensatesEvent)
    {
        CompensatesEvent = compensatesEvent ?? throw new ArgumentNullException(nameof(compensatesEvent));
    }

    /// <summary>
    /// Gets the forward (positive) event type that this handler compensates.
    /// </summary>
    public Type CompensatesEvent { get; }
}
