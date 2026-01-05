// -----------------------------------------------------------------------
// <copyright file="EncryptionServiceTests.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Compendium.Infrastructure.Security;

namespace Compendium.Infrastructure.Tests.Security;

/// <summary>
/// Comprehensive test suite for AesEncryptionService implementation.
/// Tests encryption/decryption cycles, security validation, thread-safety, and edge cases.
/// </summary>
public sealed class EncryptionServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<AesEncryptionService> _logger;
    private readonly EncryptionOptions _validOptions;
    private readonly AesEncryptionService _encryptionService;

    public EncryptionServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = Substitute.For<ILogger<AesEncryptionService>>();

        // Generate a valid 256-bit key for testing
        using var aes = Aes.Create();
        aes.GenerateKey();

        _validOptions = new EncryptionOptions
        {
            Key = Convert.ToBase64String(aes.Key),
            Salt = "TestSalt123!@#"
        };

        _encryptionService = new AesEncryptionService(_validOptions, _logger);
    }

    #region Basic Encryption/Decryption

    [Fact]
    public async Task EncryptAsync_ThenDecrypt_ShouldReturnOriginalData()
    {
        // Arrange
        const string originalText = "Hello, World! This is a test message.";

        // Act
        var encryptResult = await _encryptionService.EncryptAsync(originalText);
        var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);

        // Assert
        encryptResult.IsSuccess.Should().BeTrue();
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Be(originalText);
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?")]
    [InlineData("Unicode text: 你好世界 🌍 émojis and spëcial chars")]
    [InlineData("Very long text that spans multiple lines and contains various characters including newlines\nand tabs\t and other whitespace characters.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n\r\t")]
    public async Task Encryption_ShouldWorkWithVariousInputs(string input)
    {
        // Skip empty string test as it's handled separately
        if (string.IsNullOrEmpty(input))
        {
            var result = await _encryptionService.EncryptAsync(input);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Encryption.EmptyInput");
            return;
        }

        // Act
        var encryptResult = await _encryptionService.EncryptAsync(input);
        var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);

        // Assert
        encryptResult.IsSuccess.Should().BeTrue();
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Be(input);
    }

    [Fact]
    public async Task EncryptAsync_SameData_ShouldProduceDifferentCiphertext()
    {
        // Arrange
        const string plainText = "Same data for multiple encryptions";

        // Act
        var result1 = await _encryptionService.EncryptAsync(plainText);
        var result2 = await _encryptionService.EncryptAsync(plainText);
        var result3 = await _encryptionService.EncryptAsync(plainText);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();

        // Each encryption should produce different ciphertext due to random IV
        result1.Value.Should().NotBe(result2.Value);
        result2.Value.Should().NotBe(result3.Value);
        result1.Value.Should().NotBe(result3.Value);

        // But all should decrypt to the same original text
        var decrypt1 = await _encryptionService.DecryptAsync(result1.Value);
        var decrypt2 = await _encryptionService.DecryptAsync(result2.Value);
        var decrypt3 = await _encryptionService.DecryptAsync(result3.Value);

        decrypt1.Value.Should().Be(plainText);
        decrypt2.Value.Should().Be(plainText);
        decrypt3.Value.Should().Be(plainText);
    }

    #endregion

    #region Input Validation

    [Fact]
    public async Task EncryptAsync_WithNullInput_ShouldReturnValidationError()
    {
        // Act
        var result = await _encryptionService.EncryptAsync(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Encryption.EmptyInput");
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyInput_ShouldReturnValidationError()
    {
        // Act
        var result = await _encryptionService.EncryptAsync(string.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Encryption.EmptyInput");
    }

    [Fact]
    public async Task DecryptAsync_WithNullInput_ShouldReturnValidationError()
    {
        // Act
        var result = await _encryptionService.DecryptAsync(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Decryption.EmptyInput");
    }

    [Fact]
    public async Task DecryptAsync_WithEmptyInput_ShouldReturnValidationError()
    {
        // Act
        var result = await _encryptionService.DecryptAsync(string.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Decryption.EmptyInput");
    }

    #endregion

    #region Tampering Detection

    [Fact]
    public async Task DecryptAsync_WithTamperedData_ShouldFail()
    {
        // Arrange
        const string originalText = "Secret message";
        var encryptResult = await _encryptionService.EncryptAsync(originalText);

        // Tamper with the encrypted data more significantly
        var parts = encryptResult.Value.Split(':');
        var tamperedData = parts[0] + ":" + parts[1].Substring(0, parts[1].Length - 4) + "XXXX";

        // Act
        var decryptResult = await _encryptionService.DecryptAsync(tamperedData);

        // Assert
        decryptResult.IsSuccess.Should().BeFalse();
        decryptResult.Error.Code.Should().Be("Decryption.Failed");
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidBase64_ShouldFail()
    {
        // Arrange
        const string invalidBase64 = "invalid:base64!@#$%";

        // Act
        var result = await _encryptionService.DecryptAsync(invalidBase64);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Decryption.Failed");
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidFormat_ShouldFail()
    {
        // Arrange - Missing colon separator
        const string invalidFormat = "invalidformatwithoutcolon";

        // Act
        var result = await _encryptionService.DecryptAsync(invalidFormat);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Decryption.InvalidFormat");
    }

    [Theory]
    [InlineData("onlyonepart")]
    [InlineData("too:many:parts:here")]
    [InlineData(":missingfirstpart")]
    [InlineData("missingsecondpart:")]
    public async Task DecryptAsync_WithMalformedFormat_ShouldFail(string malformedData)
    {
        // Act
        var result = await _encryptionService.DecryptAsync(malformedData);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().BeOneOf("Decryption.InvalidFormat", "Decryption.Failed");
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task Encryption_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 100;
        const string testData = "Thread safety test data";
        var results = new ConcurrentBag<(Result<string> encrypt, Result<string> decrypt)>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        var tasks = Enumerable.Range(0, threadCount)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    var uniqueData = $"{testData} - Thread {i}";
                    var encryptResult = await _encryptionService.EncryptAsync(uniqueData);
                    var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);
                    results.Add((encryptResult, decryptResult));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
        results.Should().HaveCount(threadCount);

        foreach (var (encrypt, decrypt) in results)
        {
            encrypt.IsSuccess.Should().BeTrue();
            decrypt.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Encryption_ConcurrentOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        const int operationCount = 50;
        var testData = Enumerable.Range(0, operationCount)
            .Select(i => $"Test data item {i} with unique content")
            .ToList();

        var encryptionResults = new ConcurrentBag<(int index, string original, Result<string> encrypted)>();
        var decryptionResults = new ConcurrentBag<(int index, string original, Result<string> decrypted)>();

        // Act - Concurrent encryptions
        var encryptTasks = testData.Select((data, index) => Task.Run(async () =>
        {
            var result = await _encryptionService.EncryptAsync(data);
            encryptionResults.Add((index, data, result));
        }));

        await Task.WhenAll(encryptTasks);

        // Act - Concurrent decryptions
        var decryptTasks = encryptionResults.Select(item => Task.Run(async () =>
        {
            if (item.encrypted.IsSuccess)
            {
                var result = await _encryptionService.DecryptAsync(item.encrypted.Value);
                decryptionResults.Add((item.index, item.original, result));
            }
        }));

        await Task.WhenAll(decryptTasks);

        // Assert
        encryptionResults.Should().HaveCount(operationCount);
        decryptionResults.Should().HaveCount(operationCount);

        foreach (var (index, original, decrypted) in decryptionResults)
        {
            decrypted.IsSuccess.Should().BeTrue();
            decrypted.Value.Should().Be(original);
        }
    }

    #endregion

    #region Hashing

    [Fact]
    public async Task HashAsync_WithValidInput_ShouldReturnHash()
    {
        // Arrange
        const string input = "Password123!";

        // Act
        var result = await _encryptionService.HashAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().NotBe(input); // Hash should be different from input
    }

    [Fact]
    public async Task HashAsync_SameInput_ShouldReturnSameHash()
    {
        // Arrange
        const string input = "Consistent input";

        // Act
        var result1 = await _encryptionService.HashAsync(input);
        var result2 = await _encryptionService.HashAsync(input);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value);
    }

    [Fact]
    public async Task HashAsync_DifferentInputs_ShouldReturnDifferentHashes()
    {
        // Arrange
        const string input1 = "Password1";
        const string input2 = "Password2";

        // Act
        var result1 = await _encryptionService.HashAsync(input1);
        var result2 = await _encryptionService.HashAsync(input2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HashAsync_WithInvalidInput_ShouldReturnValidationError(string? input)
    {
        // Act
        var result = await _encryptionService.HashAsync(input!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Hash.EmptyInput");
    }

    #endregion

    #region Hash Verification

    [Fact]
    public async Task VerifyHashAsync_WithCorrectHash_ShouldReturnTrue()
    {
        // Arrange
        const string input = "TestPassword123";
        var hashResult = await _encryptionService.HashAsync(input);

        // Act
        var verifyResult = await _encryptionService.VerifyHashAsync(input, hashResult.Value);

        // Assert
        hashResult.IsSuccess.Should().BeTrue();
        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyHashAsync_WithIncorrectHash_ShouldReturnFalse()
    {
        // Arrange
        const string input = "TestPassword123";
        const string wrongInput = "WrongPassword456";
        var hashResult = await _encryptionService.HashAsync(input);

        // Act
        var verifyResult = await _encryptionService.VerifyHashAsync(wrongInput, hashResult.Value);

        // Assert
        hashResult.IsSuccess.Should().BeTrue();
        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "validhash")]
    [InlineData("", "validhash")]
    [InlineData("validinput", null)]
    [InlineData("validinput", "")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public async Task VerifyHashAsync_WithInvalidInputs_ShouldReturnValidationError(string? input, string? hash)
    {
        // Act
        var result = await _encryptionService.VerifyHashAsync(input!, hash!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("HashVerification.EmptyInput");
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AesEncryptionService(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AesEncryptionService(_validOptions, null!));
    }

    #endregion

    #region EncryptionOptions Validation

    [Fact]
    public void EncryptionOptions_WithValidKeyAndSalt_ShouldValidateSuccessfully()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            Key = _validOptions.Key,
            Salt = "ValidSalt123"
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(null, "ValidSalt")]
    [InlineData("", "ValidSalt")]
    [InlineData("   ", "ValidSalt")]
    public void EncryptionOptions_WithInvalidKey_ShouldThrowInvalidOperationException(string? key, string salt)
    {
        // Arrange
        var options = new EncryptionOptions
        {
            Key = key!,
            Salt = salt
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Encryption key must be provided");
    }

    [Theory]
    [InlineData("ValidKey", null)]
    [InlineData("ValidKey", "")]
    [InlineData("ValidKey", "   ")]
    public void EncryptionOptions_WithInvalidSalt_ShouldThrowInvalidOperationException(string key, string? salt)
    {
        // Arrange
        var options = new EncryptionOptions
        {
            Key = key,
            Salt = salt!
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Salt must be provided");
    }

    [Fact]
    public void EncryptionOptions_WithInvalidBase64Key_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            Key = "InvalidBase64!@#$%",
            Salt = "ValidSalt"
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Encryption key must be valid Base64");
    }

    [Fact]
    public void EncryptionOptions_WithWrongKeyLength_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create a 128-bit key instead of 256-bit
        var shortKey = Convert.ToBase64String(new byte[16]); // 128 bits
        var options = new EncryptionOptions
        {
            Key = shortKey,
            Salt = "ValidSalt"
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Encryption key must be 256 bits (32 bytes) encoded as Base64");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Encryption_PerformanceTest_ShouldHandleReasonableLoad()
    {
        // Arrange
        const int operationCount = 1000;
        const string testData = "Performance test data that is reasonably sized for encryption testing";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, operationCount)
            .Select(async i =>
            {
                var uniqueData = $"{testData} - Operation {i}";
                var encryptResult = await _encryptionService.EncryptAsync(uniqueData);
                var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);
                return (encryptResult, decryptResult);
            });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(operationCount);
        results.Should().AllSatisfy(r =>
        {
            r.encryptResult.IsSuccess.Should().BeTrue();
            r.decryptResult.IsSuccess.Should().BeTrue();
        });

        var avgTimePerOperation = (double)stopwatch.ElapsedMilliseconds / operationCount;
        _output.WriteLine($"Processed {operationCount} encrypt/decrypt cycles in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {avgTimePerOperation:F3}ms");

        // Performance assertion - should be reasonably fast
        avgTimePerOperation.Should().BeLessThan(10, "Each encrypt/decrypt cycle should be fast");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Encryption_WithVeryLargeData_ShouldWorkCorrectly()
    {
        // Arrange
        var largeData = new StringBuilder();
        for (int i = 0; i < 10000; i++)
        {
            largeData.AppendLine($"Line {i}: This is a test line with some content to make the data larger.");
        }
        var testData = largeData.ToString();

        // Act
        var encryptResult = await _encryptionService.EncryptAsync(testData);
        var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);

        // Assert
        encryptResult.IsSuccess.Should().BeTrue();
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Be(testData);

        _output.WriteLine($"Successfully encrypted/decrypted {testData.Length} characters");
    }

    [Fact]
    public async Task Encryption_WithSpecialCharacters_ShouldPreserveData()
    {
        // Arrange
        const string specialData = "Special chars: \0\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F" +
                                  "\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F" +
                                  "Regular text with émojis 🔐🔑🛡️ and unicode: 你好世界";

        // Act
        var encryptResult = await _encryptionService.EncryptAsync(specialData);
        var decryptResult = await _encryptionService.DecryptAsync(encryptResult.Value);

        // Assert
        encryptResult.IsSuccess.Should().BeTrue();
        decryptResult.IsSuccess.Should().BeTrue();
        decryptResult.Value.Should().Be(specialData);
        decryptResult.Value.Length.Should().Be(specialData.Length);
    }

    #endregion
}
