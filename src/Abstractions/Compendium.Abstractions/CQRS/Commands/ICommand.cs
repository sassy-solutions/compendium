// -----------------------------------------------------------------------
// <copyright file="ICommand.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.CQRS.Commands;

/// <summary>
/// Marker interface for commands that don't return a value.
/// Commands represent write operations that change system state.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Interface for commands that return a value.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<out TResponse> : ICommand
{
}
