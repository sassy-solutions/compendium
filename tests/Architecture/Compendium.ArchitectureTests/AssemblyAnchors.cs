// -----------------------------------------------------------------------
// <copyright file="AssemblyAnchors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Compendium.Abstractions.CQRS.Commands;
using Compendium.Application.CQRS;
using Compendium.Core.Domain.Events;
using Compendium.Core.Results;
using Compendium.Infrastructure.EventSourcing;

namespace Compendium.ArchitectureTests;

/// <summary>
/// Marker types used to obtain references to each Compendium framework assembly.
/// Centralising these anchors keeps the architecture rules resilient to future
/// renames inside the production projects.
/// </summary>
internal static class AssemblyAnchors
{
    /// <summary>Compendium.Core (zero dependencies).</summary>
    public static readonly Assembly Core = typeof(Result).Assembly;

    /// <summary>Compendium.Abstractions (depends only on Core).</summary>
    public static readonly Assembly Abstractions = typeof(ICommand).Assembly;

    /// <summary>Compendium.Application (depends on Core + Abstractions).</summary>
    public static readonly Assembly Application = typeof(ICommandDispatcher).Assembly;

    /// <summary>Compendium.Infrastructure (depends on Core + Abstractions + Application + Multitenancy).</summary>
    public static readonly Assembly Infrastructure = typeof(InMemoryEventStore).Assembly;

    /// <summary>Marker namespace prefix shared by every Compendium adapter assembly.</summary>
    public const string AdapterNamespacePrefix = "Compendium.Adapters";

    /// <summary>The Core domain-event marker interface.</summary>
    public static readonly Type DomainEventInterface = typeof(IDomainEvent);
}
