// -----------------------------------------------------------------------
// <copyright file="PresignedAction.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Storage.Models;

/// <summary>
/// Specifies the action allowed by a presigned URL.
/// </summary>
public enum PresignedAction
{
    /// <summary>
    /// Allows downloading the object via the presigned URL (HTTP GET).
    /// </summary>
    Get = 0,

    /// <summary>
    /// Allows uploading the object via the presigned URL (HTTP PUT).
    /// </summary>
    Put = 1,
}
