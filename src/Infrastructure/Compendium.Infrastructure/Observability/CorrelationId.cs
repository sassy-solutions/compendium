namespace Compendium.Infrastructure.Observability;

/// <summary>
/// Provides functionality for managing correlation IDs across request contexts.
/// Correlation IDs help track requests across distributed systems and services.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID for the active context.
    /// </summary>
    /// <returns>The current correlation ID, or a newly generated one if none exists.</returns>
    string GetCorrelationId();

    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID and sets it as the current one.
    /// </summary>
    /// <returns>The newly generated correlation ID.</returns>
    string GenerateCorrelationId();
}

/// <summary>
/// Default implementation of correlation ID provider using AsyncLocal for thread-safe storage.
/// Maintains correlation IDs per async context, ensuring proper isolation in concurrent scenarios.
/// </summary>
public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// Gets the current correlation ID for the active context.
    /// </summary>
    /// <returns>The current correlation ID, or a newly generated one if none exists.</returns>
    public string GetCorrelationId()
    {
        return _correlationId.Value ?? GenerateCorrelationId();
    }

    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    /// <exception cref="ArgumentException">Thrown when correlationId is null or empty.</exception>
    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
        }

        _correlationId.Value = correlationId;
    }

    /// <summary>
    /// Generates a new correlation ID and sets it as the current one.
    /// </summary>
    /// <returns>The newly generated correlation ID.</returns>
    public string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        _correlationId.Value = correlationId;
        return correlationId;
    }
}

/// <summary>
/// Provides a disposable scope for temporarily setting a correlation ID.
/// When disposed, restores the previous correlation ID, enabling nested correlation contexts.
/// </summary>
public sealed class CorrelationIdScope : IDisposable
{
    private readonly ICorrelationIdProvider _provider;
    private readonly string? _previousCorrelationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdScope"/> class.
    /// </summary>
    /// <param name="provider">The correlation ID provider.</param>
    /// <param name="correlationId">The correlation ID to set for this scope.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    public CorrelationIdScope(ICorrelationIdProvider provider, string correlationId)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _previousCorrelationId = provider.GetCorrelationId();
        provider.SetCorrelationId(correlationId);
    }

    /// <summary>
    /// Restores the previous correlation ID when the scope is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_previousCorrelationId is not null)
        {
            _provider.SetCorrelationId(_previousCorrelationId);
        }
    }
}
