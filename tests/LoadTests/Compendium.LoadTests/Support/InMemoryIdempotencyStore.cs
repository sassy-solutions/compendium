// -----------------------------------------------------------------------
// <copyright file="InMemoryIdempotencyStore.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Compendium.Application.Idempotency;
using Compendium.Core.Results;

namespace Compendium.LoadTests.Support;

/// <summary>
/// Thread-safe in-memory <see cref="IIdempotencyStore"/> used to measure the
/// raw lookup / write latency of the idempotency abstraction without involving
/// Redis. Entries expire lazily on access to keep the hot path lock-free.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    /// <inheritdoc />
    public Task<Result<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            return Task.FromResult(Result.Success(false));
        }

        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _entries.TryRemove(key, out _);
                return Task.FromResult(Result.Success(false));
            }

            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Success(false));
    }

    /// <inheritdoc />
    public Task<Result<TResult?>> GetAsync<TResult>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            return Task.FromResult(Result.Success<TResult?>(default));
        }

        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _entries.TryRemove(key, out _);
                return Task.FromResult(Result.Success<TResult?>(default));
            }

            if (entry.Value is TResult typed)
            {
                return Task.FromResult(Result.Success<TResult?>(typed));
            }
        }

        return Task.FromResult(Result.Success<TResult?>(default));
    }

    /// <inheritdoc />
    public Task<Result> SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            return Task.FromResult(Result.Failure(Error.Validation("Idempotency.InvalidKey", "Key cannot be null or empty")));
        }

        var expiresAt = expiration > TimeSpan.Zero ? DateTimeOffset.UtcNow.Add(expiration) : DateTimeOffset.MaxValue;
        _entries[key] = new Entry(value, expiresAt);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Number of live entries currently held in the store.
    /// </summary>
    public int Count => _entries.Count;

    private sealed record Entry(object? Value, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
