// -----------------------------------------------------------------------
// <copyright file="EncryptionBenchmarks.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Compendium.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Infrastructure.PerfTests.Benchmarks;

/// <summary>AES encryption / decryption throughput.</summary>
[MemoryDiagnoser]
public class EncryptionBenchmarks
{
    private const string Payload = "Performance test data that is reasonably sized for encryption testing";
    private AesEncryptionService _sut = null!;
    private string _cipherText = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();

        var options = new EncryptionOptions
        {
            Key = Convert.ToBase64String(aes.Key),
            Salt = "BenchSalt123!@#",
        };

        _sut = new AesEncryptionService(options, NullLogger<AesEncryptionService>.Instance);

        // Pre-encrypt once so the decrypt benchmark has stable input.
        var encrypted = await _sut.EncryptAsync(Payload);
        _cipherText = encrypted.Value!;
    }

    [Benchmark]
    public async Task<string> Encrypt()
    {
        var result = await _sut.EncryptAsync(Payload);
        return result.Value!;
    }

    [Benchmark]
    public async Task<string> Decrypt()
    {
        var result = await _sut.DecryptAsync(_cipherText);
        return result.Value!;
    }
}
