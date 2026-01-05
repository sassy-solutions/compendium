// -----------------------------------------------------------------------
// <copyright file="ICommand.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
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
