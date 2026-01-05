using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Compendium.Application.Idempotency;

/// <summary>
/// Defines a strategy for generating unique keys from request objects
/// for deduplication and idempotency purposes.
/// </summary>
public interface IDeduplicationStrategy
{
    /// <summary>
    /// Generates a unique key from the given request object.
    /// </summary>
    /// <typeparam name="T">The type of the request object.</typeparam>
    /// <param name="request">The request object to generate a key from.</param>
    /// <returns>A unique string key representing the request.</returns>
    string GenerateKey<T>(T request) where T : class;
}

/// <summary>
/// A deduplication strategy that generates keys by serializing the entire request object
/// to JSON and computing a SHA256 hash of the serialized content.
/// </summary>
public sealed class HashBasedDeduplicationStrategy : IDeduplicationStrategy
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashBasedDeduplicationStrategy"/> class.
    /// </summary>
    /// <param name="jsonOptions">Optional JSON serialization options. Uses default camelCase options if not provided.</param>
    public HashBasedDeduplicationStrategy(JsonSerializerOptions? jsonOptions = null)
    {
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Generates a unique key by serializing the request to JSON and computing its SHA256 hash.
    /// </summary>
    /// <typeparam name="T">The type of the request object.</typeparam>
    /// <param name="request">The request object to generate a key from.</param>
    /// <returns>A Base64-encoded SHA256 hash of the serialized request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public string GenerateKey<T>(T request) where T : class
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);

        return Convert.ToBase64String(hashBytes);
    }
}

/// <summary>
/// A deduplication strategy that generates keys based on specific properties of the request object.
/// Only the specified properties are used to compute the hash, providing more targeted deduplication.
/// </summary>
public sealed class PropertyBasedDeduplicationStrategy : IDeduplicationStrategy
{
    private readonly string[] _propertyNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyBasedDeduplicationStrategy"/> class.
    /// </summary>
    /// <param name="propertyNames">The names of the properties to include in key generation.</param>
    /// <exception cref="ArgumentNullException">Thrown when propertyNames is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no property names are specified.</exception>
    public PropertyBasedDeduplicationStrategy(params string[] propertyNames)
    {
        _propertyNames = propertyNames ?? throw new ArgumentNullException(nameof(propertyNames));

        if (_propertyNames.Length == 0)
        {
            throw new ArgumentException("At least one property name must be specified", nameof(propertyNames));
        }
    }

    /// <summary>
    /// Generates a unique key by extracting the specified properties from the request
    /// and computing a SHA256 hash of their combined values.
    /// </summary>
    /// <typeparam name="T">The type of the request object.</typeparam>
    /// <param name="request">The request object to generate a key from.</param>
    /// <returns>A Base64-encoded SHA256 hash of the specified property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a specified property is not found on the request type.</exception>
    public string GenerateKey<T>(T request) where T : class
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var type = typeof(T);
        var values = new List<string>();

        foreach (var propertyName in _propertyNames)
        {
            var property = type.GetProperty(propertyName);
            if (property is null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' not found on type '{type.Name}'");
            }

            var value = property.GetValue(request);
            values.Add(value?.ToString() ?? "null");
        }

        var combined = string.Join("|", values);
        var bytes = Encoding.UTF8.GetBytes(combined);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);

        return Convert.ToBase64String(hashBytes);
    }
}
