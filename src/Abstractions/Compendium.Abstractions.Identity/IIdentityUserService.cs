// -----------------------------------------------------------------------
// <copyright file="IIdentityUserService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Identity.Models;
using Compendium.Abstractions.Identity.Models.Requests;

namespace Compendium.Abstractions.Identity;

/// <summary>
/// Provides operations for managing identity users.
/// This interface is provider-agnostic and can be implemented by various identity providers
/// such as Zitadel, Auth0, Keycloak, or Azure AD B2C.
/// </summary>
public interface IIdentityUserService
{
    /// <summary>
    /// Creates a new user in the identity system.
    /// </summary>
    /// <param name="request">The create user request containing user details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created user or an error.</returns>
    Task<Result<IdentityUser>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the user or an error if not found.</returns>
    Task<Result<IdentityUser>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the user or an error if not found.</returns>
    Task<Result<IdentityUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The update request containing the fields to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account, preventing them from logging in.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to deactivate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to reactivate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> ReactivateUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists users with pagination and optional filtering.
    /// </summary>
    /// <param name="request">The list request containing pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing a paginated list of users or an error.</returns>
    Task<Result<PagedResult<IdentityUser>>> ListUsersAsync(ListUsersRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from the identity system.
    /// This is typically a destructive operation and should be used with caution.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or an error.</returns>
    Task<Result> InitiatePasswordResetAsync(string userId, CancellationToken cancellationToken = default);
}
