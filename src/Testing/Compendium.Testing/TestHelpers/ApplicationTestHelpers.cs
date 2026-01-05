using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Testing.TestHelpers;

/// <summary>
/// Test helpers for application layer testing
/// </summary>
public static class ApplicationTestHelpers
{
    /// <summary>
    /// Adds test application services to the service collection
    /// </summary>
    public static IServiceCollection AddTestApplication(this IServiceCollection services)
    {
        // Basic test setup - can be extended as needed
        return services;
    }
}

/// <summary>
/// In-memory store for testing idempotency
/// </summary>
public sealed class InMemoryTestStore
{
    private readonly Dictionary<string, object?> _store = new();

    /// <summary>
    /// Checks if a key exists
    /// </summary>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(key));
    }

    /// <summary>
    /// Gets a value by key
    /// </summary>
    public Task<TResult?> GetAsync<TResult>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var value) && value is TResult result)
        {
            return Task.FromResult<TResult?>(result);
        }
        return Task.FromResult<TResult?>(default);
    }

    /// <summary>
    /// Sets a value with expiration
    /// </summary>
    public Task SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all stored values
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }
}
