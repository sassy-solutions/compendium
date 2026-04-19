// -----------------------------------------------------------------------
// <copyright file="Encryption.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Compendium.Infrastructure.Security;

/// <summary>
/// Interface for encryption and hashing services.
/// Provides secure encryption, decryption, hashing, and hash verification operations.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the encrypted text on success, or an error on failure.</returns>
    Task<Result<string>> EncryptAsync(string plainText, CancellationToken cancellationToken = default);
    /// <summary>
    /// Decrypts the specified encrypted text.
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the decrypted plain text on success, or an error on failure.</returns>
    Task<Result<string>> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default);
    /// <summary>
    /// Computes a hash of the specified input string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the computed hash on success, or an error on failure.</returns>
    Task<Result<string>> HashAsync(string input, CancellationToken cancellationToken = default);
    /// <summary>
    /// Verifies that the specified input matches the given hash.
    /// </summary>
    /// <param name="input">The input string to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing true if the hash matches, false otherwise, or an error on failure.</returns>
    Task<Result<bool>> VerifyHashAsync(string input, string hash, CancellationToken cancellationToken = default);
}

/// <summary>
/// AES-based encryption service that provides secure encryption, decryption, and hashing operations.
/// Uses AES-256 encryption with randomly generated initialization vectors for enhanced security.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly EncryptionOptions _options;
    private readonly ILogger<AesEncryptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class.
    /// </summary>
    /// <param name="options">The encryption options containing key and salt.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public AesEncryptionService(EncryptionOptions options, ILogger<AesEncryptionService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<string>> EncryptAsync(string plainText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return Result.Failure<string>(Error.Validation("Encryption.EmptyInput", "Plain text cannot be null or empty"));
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_options.Key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            await swEncrypt.WriteAsync(plainText);
            await swEncrypt.FlushAsync();
            csEncrypt.FlushFinalBlock();

            var iv = Convert.ToBase64String(aes.IV);
            var encrypted = Convert.ToBase64String(msEncrypt.ToArray());
            var result = $"{iv}:{encrypted}";

            _logger.LogDebug("Successfully encrypted data");
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            return Result.Failure<string>(Error.Failure("Encryption.Failed", "Failed to encrypt data"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> DecryptAsync(string encryptedText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return Result.Failure<string>(Error.Validation("Decryption.EmptyInput", "Encrypted text cannot be null or empty"));
        }

        try
        {
            var parts = encryptedText.Split(':');
            if (parts.Length != 2)
            {
                return Result.Failure<string>(Error.Validation("Decryption.InvalidFormat", "Invalid encrypted text format"));
            }

            var iv = Convert.FromBase64String(parts[0]);
            var encrypted = Convert.FromBase64String(parts[1]);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_options.Key);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encrypted);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var result = await srDecrypt.ReadToEndAsync();
            _logger.LogDebug("Successfully decrypted data");
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            return Result.Failure<string>(Error.Failure("Decryption.Failed", "Failed to decrypt data"));
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> HashAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Task.FromResult(Result.Failure<string>(Error.Validation("Hash.EmptyInput", "Input cannot be null or empty")));
        }

        try
        {
            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input + _options.Salt);
            var hashBytes = sha256.ComputeHash(inputBytes);
            var hash = Convert.ToBase64String(hashBytes);

            _logger.LogDebug("Successfully hashed input");
            return Task.FromResult(Result.Success(hash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash input");
            return Task.FromResult(Result.Failure<string>(Error.Failure("Hash.Failed", "Failed to hash input")));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> VerifyHashAsync(string input, string hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
        {
            return Result.Failure<bool>(Error.Validation("HashVerification.EmptyInput", "Input and hash cannot be null or empty"));
        }

        var hashResult = await HashAsync(input, cancellationToken);
        if (hashResult.IsFailure)
        {
            return Result.Failure<bool>(hashResult.Error);
        }

        var isValid = string.Equals(hashResult.Value, hash, StringComparison.Ordinal);
        _logger.LogDebug("Hash verification result: {IsValid}", isValid);

        return Result.Success(isValid);
    }
}

/// <summary>
/// Configuration options for encryption services.
/// Contains the encryption key and salt used for cryptographic operations.
/// </summary>
public sealed class EncryptionOptions
{
    /// <summary>
    /// Gets or initializes the Base64-encoded encryption key.
    /// Must be a 256-bit (32-byte) key encoded as Base64.
    /// </summary>
    public string Key { get; init; } = string.Empty;
    /// <summary>
    /// Gets or initializes the salt used for hashing operations.
    /// </summary>
    public string Salt { get; init; } = string.Empty;

    /// <summary>
    /// Validates the encryption options to ensure they are properly configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the key or salt is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException("Encryption key must be provided");
        }

        if (string.IsNullOrWhiteSpace(Salt))
        {
            throw new InvalidOperationException("Salt must be provided");
        }

        try
        {
            var keyBytes = Convert.FromBase64String(Key);
            if (keyBytes.Length != 32) // 256 bits
            {
                throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes) encoded as Base64");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Encryption key must be valid Base64");
        }
    }
}
