// -----------------------------------------------------------------------
// <copyright file="InMemoryIdempotencyStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Application.Idempotency;
using Compendium.Core.Results;

namespace Compendium.Infrastructure.Idempotency;

/// <summary>
/// In-memory implementation of <see cref="IIdempotencyStore"/> for testing
/// and InMemory-backed framework E2E scenarios. Thread-safe; expiring entries
/// are pruned lazily on read.
/// </summary>
/// <remarks>
/// Semantic contract: matches <c>RedisIdempotencyStore</c>. Each entry has a
/// TTL set via <see cref="SetAsync{TValue}"/>; entries that have outlived
/// their TTL are removed on the next access (lazy eviction).
/// </remarks>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _store = new();

    /// <inheritdoc />
    public Task<Result<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var exists = TryGetLive(key, out _);
        return Task.FromResult(Result.Success(exists));
    }

    /// <inheritdoc />
    public Task<Result<TResult?>> GetAsync<TResult>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (TryGetLive(key, out var entry) && entry.Value is TResult typed)
        {
            return Task.FromResult(Result.Success<TResult?>(typed));
        }

        return Task.FromResult(Result.Success<TResult?>(default));
    }

    /// <inheritdoc />
    public Task<Result> SetAsync<TValue>(
        string key,
        TValue value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (expiration <= TimeSpan.Zero)
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("Idempotency.InvalidExpiration", "Expiration must be positive.")));
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(expiration);
        _store[key] = new Entry(value, expiresAt);
        return Task.FromResult(Result.Success());
    }

    /// <summary>Clears all entries. Test-only helper.</summary>
    public void Clear() => _store.Clear();

    private bool TryGetLive(string key, out Entry entry)
    {
        if (_store.TryGetValue(key, out var found))
        {
            if (found.ExpiresAt > DateTimeOffset.UtcNow)
            {
                entry = found;
                return true;
            }

            _store.TryRemove(key, out _);
        }

        entry = default!;
        return false;
    }

    private sealed record Entry(object? Value, DateTimeOffset ExpiresAt);
}
