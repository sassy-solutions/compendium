// -----------------------------------------------------------------------
// <copyright file="InMemoryKeyVaultTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Compendium.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for <see cref="InMemoryKeyVault"/> covering the secret lifecycle, validation,
/// expiration handling, and listing semantics.
/// </summary>
public sealed class InMemoryKeyVaultTests
{
    private readonly IEncryptionService _encryption;
    private readonly InMemoryKeyVault _sut;

    public InMemoryKeyVaultTests()
    {
        // Arrange
        _encryption = CreateRealEncryptionService();
        _sut = new InMemoryKeyVault(_encryption, NullLogger<InMemoryKeyVault>.Instance);
    }

    [Fact]
    public void Ctor_NullEncryption_Throws()
    {
        // Arrange / Act
        var act = () => new InMemoryKeyVault(null!, NullLogger<InMemoryKeyVault>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("encryptionService");
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange / Act
        var act = () => new InMemoryKeyVault(_encryption, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task SetSecretAsync_ThenGetSecretAsync_ReturnsOriginalValue()
    {
        // Arrange / Act
        var setResult = await _sut.SetSecretAsync("api-key", "super-secret");
        var getResult = await _sut.GetSecretAsync("api-key");

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be("super-secret");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetSecretAsync_EmptyName_ReturnsValidationFailure(string invalid)
    {
        // Arrange / Act
        var result = await _sut.SetSecretAsync(invalid, "value");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.EmptyName");
    }

    [Fact]
    public async Task SetSecretAsync_EmptyValue_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.SetSecretAsync("name", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.EmptyValue");
    }

    [Fact]
    public async Task SetSecretAsync_OverwritesExistingValue()
    {
        // Arrange
        await _sut.SetSecretAsync("k", "v1");

        // Act
        await _sut.SetSecretAsync("k", "v2");
        var get = await _sut.GetSecretAsync("k");

        // Assert
        get.IsSuccess.Should().BeTrue();
        get.Value.Should().Be("v2");
    }

    [Fact]
    public async Task SetSecretAsync_EncryptionFailure_PropagatesError()
    {
        // Arrange
        var failingEncryption = Substitute.For<IEncryptionService>();
        failingEncryption.EncryptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("enc.fail", "boom")));
        var sut = new InMemoryKeyVault(failingEncryption, NullLogger<InMemoryKeyVault>.Instance);

        // Act
        var result = await sut.SetSecretAsync("k", "v");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("enc.fail");
    }

    [Fact]
    public async Task GetSecretAsync_UnknownSecret_ReturnsNotFound()
    {
        // Arrange / Act
        var result = await _sut.GetSecretAsync("missing");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.SecretNotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_EmptyName_ReturnsValidationFailure(string invalid)
    {
        // Arrange / Act
        var result = await _sut.GetSecretAsync(invalid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.EmptyName");
    }

    [Fact]
    public async Task GetSecretAsync_DecryptionFailure_PropagatesError()
    {
        // Arrange
        var failingEncryption = Substitute.For<IEncryptionService>();
        failingEncryption.EncryptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("ciphertext"));
        failingEncryption.DecryptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("dec.fail", "boom")));
        var sut = new InMemoryKeyVault(failingEncryption, NullLogger<InMemoryKeyVault>.Instance);
        await sut.SetSecretAsync("k", "v");

        // Act
        var result = await sut.GetSecretAsync("k");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("dec.fail");
    }

    [Fact]
    public async Task DeleteSecretAsync_ExistingSecret_ReturnsSuccess()
    {
        // Arrange
        await _sut.SetSecretAsync("k", "v");

        // Act
        var deleted = await _sut.DeleteSecretAsync("k");
        var get = await _sut.GetSecretAsync("k");

        // Assert
        deleted.IsSuccess.Should().BeTrue();
        get.IsFailure.Should().BeTrue();
        get.Error.Code.Should().Be("KeyVault.SecretNotFound");
    }

    [Fact]
    public async Task DeleteSecretAsync_UnknownSecret_ReturnsNotFound()
    {
        // Arrange / Act
        var result = await _sut.DeleteSecretAsync("ghost");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.SecretNotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteSecretAsync_EmptyName_ReturnsValidationFailure(string invalid)
    {
        // Arrange / Act
        var result = await _sut.DeleteSecretAsync(invalid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.EmptyName");
    }

    [Fact]
    public async Task ListSecretsAsync_ReturnsSortedNames()
    {
        // Arrange
        await _sut.SetSecretAsync("z", "1");
        await _sut.SetSecretAsync("a", "1");
        await _sut.SetSecretAsync("m", "1");

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { "a", "m", "z" }, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public async Task ListSecretsAsync_EmptyVault_ReturnsEmptyList()
    {
        // Arrange / Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSecretAsync_ExpiredSecret_ReturnsExpiredAndRemoves()
    {
        // Arrange — store secret then mutate the underlying SecretEntry record to set ExpiresAt in the past.
        await _sut.SetSecretAsync("k", "v");
        ForceSecretExpiration(_sut, "k");

        // Act
        var result = await _sut.GetSecretAsync("k");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeyVault.SecretExpired");
    }

    [Fact]
    public async Task ListSecretsAsync_FiltersExpiredSecrets()
    {
        // Arrange
        await _sut.SetSecretAsync("active", "v1");
        await _sut.SetSecretAsync("expired", "v2");
        ForceSecretExpiration(_sut, "expired");

        // Act
        var result = await _sut.ListSecretsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("active");
        result.Value.Should().NotContain("expired");
    }

    private static void ForceSecretExpiration(InMemoryKeyVault vault, string name)
    {
        var dictField = typeof(InMemoryKeyVault).GetField("_secrets", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dict = dictField.GetValue(vault)!;
        var entryType = typeof(InMemoryKeyVault).Assembly.GetType("Compendium.Infrastructure.Security.SecretEntry")!;

        // Read existing entry from the ConcurrentDictionary<string, SecretEntry>
        var indexer = dict.GetType().GetProperty("Item")!;
        var existing = indexer.GetValue(dict, new object[] { name })!;

        // Construct a replacement entry with ExpiresAt in the past
        var newEntry = Activator.CreateInstance(entryType)!;
        entryType.GetProperty("Name")!.SetValue(newEntry, name);
        entryType.GetProperty("EncryptedValue")!.SetValue(newEntry, entryType.GetProperty("EncryptedValue")!.GetValue(existing));
        entryType.GetProperty("CreatedAt")!.SetValue(newEntry, DateTime.UtcNow.AddMinutes(-5));
        entryType.GetProperty("ExpiresAt")!.SetValue(newEntry, DateTime.UtcNow.AddSeconds(-1));

        indexer.SetValue(dict, newEntry, new object[] { name });
    }

    private static AesEncryptionService CreateRealEncryptionService()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        var options = new EncryptionOptions
        {
            Key = Convert.ToBase64String(aes.Key),
            Salt = "salt-value",
        };
        return new AesEncryptionService(options, NullLogger<AesEncryptionService>.Instance);
    }
}
