// -----------------------------------------------------------------------
// <copyright file="KeyVault.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Compendium.Infrastructure.Security;

/// <summary>
/// Interface for secure key vault operations.
/// Provides functionality to securely store, retrieve, and manage secrets.
/// </summary>
public interface IKeyVault
{
    /// <summary>
    /// Retrieves a secret by name.
    /// </summary>
    /// <param name="name">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret value on success, or an error on failure.</returns>
    Task<Result<string>> GetSecretAsync(string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Stores a secret with the specified name and value.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes a secret by name.
    /// </summary>
    /// <param name="name">The name of the secret to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists all available secret names.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of secret names on success, or an error on failure.</returns>
    Task<Result<IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of a key vault for development and testing purposes.
/// Stores encrypted secrets in memory with optional expiration support.
/// </summary>
public sealed class InMemoryKeyVault : IKeyVault
{
    private readonly ConcurrentDictionary<string, SecretEntry> _secrets = new();
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InMemoryKeyVault> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryKeyVault"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service for securing secrets.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when encryptionService or logger is null.</exception>
    public InMemoryKeyVault(IEncryptionService encryptionService, ILogger<InMemoryKeyVault> logger)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<string>(Error.Validation("KeyVault.EmptyName", "Secret name cannot be null or empty"));
        }

        try
        {
            if (!_secrets.TryGetValue(name, out var secretEntry))
            {
                _logger.LogWarning("Secret {SecretName} not found", name);
                return Result.Failure<string>(Error.NotFound("KeyVault.SecretNotFound", $"Secret '{name}' not found"));
            }

            if (secretEntry.ExpiresAt.HasValue && secretEntry.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _secrets.TryRemove(name, out _);
                _logger.LogWarning("Secret {SecretName} has expired", name);
                return Result.Failure<string>(Error.NotFound("KeyVault.SecretExpired", $"Secret '{name}' has expired"));
            }

            var decryptResult = await _encryptionService.DecryptAsync(secretEntry.EncryptedValue, cancellationToken);
            if (decryptResult.IsFailure)
            {
                _logger.LogError("Failed to decrypt secret {SecretName}", name);
                return Result.Failure<string>(decryptResult.Error);
            }

            _logger.LogDebug("Successfully retrieved secret {SecretName}", name);
            return Result.Success(decryptResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret {SecretName}", name);
            return Result.Failure<string>(Error.Failure("KeyVault.GetFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("KeyVault.EmptyName", "Secret name cannot be null or empty"));
        }

        if (string.IsNullOrEmpty(value))
        {
            return Result.Failure(Error.Validation("KeyVault.EmptyValue", "Secret value cannot be null or empty"));
        }

        try
        {
            var encryptResult = await _encryptionService.EncryptAsync(value, cancellationToken);
            if (encryptResult.IsFailure)
            {
                _logger.LogError("Failed to encrypt secret {SecretName}", name);
                return Result.Failure(encryptResult.Error);
            }

            var secretEntry = new SecretEntry
            {
                Name = name,
                EncryptedValue = encryptResult.Value,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = null // Could be configurable
            };

            _secrets.AddOrUpdate(name, secretEntry, (_, _) => secretEntry);
            _logger.LogDebug("Successfully stored secret {SecretName}", name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName}", name);
            return Result.Failure(Error.Failure("KeyVault.SetFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(Result.Failure(Error.Validation("KeyVault.EmptyName", "Secret name cannot be null or empty")));
        }

        try
        {
            var removed = _secrets.TryRemove(name, out _);
            if (!removed)
            {
                _logger.LogWarning("Attempted to delete non-existent secret {SecretName}", name);
                return Task.FromResult(Result.Failure(Error.NotFound("KeyVault.SecretNotFound", $"Secret '{name}' not found")));
            }

            _logger.LogDebug("Successfully deleted secret {SecretName}", name);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName}", name);
            return Task.FromResult(Result.Failure(Error.Failure("KeyVault.DeleteFailed", ex.Message)));
        }
    }

    /// <inheritdoc />
    public Task<Result<IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var secretNames = _secrets.Values
                .Where(s => !s.ExpiresAt.HasValue || s.ExpiresAt.Value > now)
                .Select(s => s.Name)
                .OrderBy(name => name)
                .ToList();

            _logger.LogDebug("Listed {SecretCount} secrets", secretNames.Count);
            return Task.FromResult(Result.Success<IEnumerable<string>>(secretNames));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets");
            return Task.FromResult(Result.Failure<IEnumerable<string>>(Error.Failure("KeyVault.ListFailed", ex.Message)));
        }
    }
}

/// <summary>
/// Represents a secret entry stored in the key vault.
/// Contains the encrypted secret value and metadata about creation and expiration.
/// </summary>
internal sealed record SecretEntry
{
    /// <summary>
    /// Gets or initializes the name of the secret.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or initializes the encrypted value of the secret.
    /// </summary>
    public string EncryptedValue { get; init; } = string.Empty;
    /// <summary>
    /// Gets or initializes the timestamp when the secret was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
    /// <summary>
    /// Gets or initializes the optional expiration timestamp for the secret.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
