// -----------------------------------------------------------------------
// <copyright file="IdempotencyBehavior.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Compendium.Application.Idempotency;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.CQRS.Behaviors;

/// <summary>
/// Pipeline behavior that provides idempotency for command processing.
/// Prevents duplicate command execution by caching results and checking for previously processed commands.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response being returned.</typeparam>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="idempotencyService">The idempotency service for tracking processed operations.</param>
    /// <param name="logger">The logger instance.</param>
    public IdempotencyBehavior(
        IIdempotencyService idempotencyService,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyService = idempotencyService ?? throw new ArgumentNullException(nameof(idempotencyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Handles the request with idempotency checking and result caching.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        // Only apply idempotency to commands, not queries
        if (!IsCommand(request))
        {
            return await next().ConfigureAwait(false);
        }

        var idempotencyKey = GenerateIdempotencyKey(request);
        var requestName = typeof(TRequest).Name;

        _logger.LogDebug("Checking idempotency for {RequestName} with key {IdempotencyKey}", requestName, idempotencyKey);

        // Check if this command has already been processed
        var isProcessed = await _idempotencyService.IsProcessedAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);

        if (isProcessed)
        {
            _logger.LogInformation("Command {RequestName} with key {IdempotencyKey} has already been processed, returning cached result", requestName, idempotencyKey);

            // Return cached result
            var cachedResult = await _idempotencyService.GetResultAsync<TResponse>(idempotencyKey, cancellationToken).ConfigureAwait(false);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            // If we can't retrieve the cached result, log warning and proceed
            _logger.LogWarning("Command {RequestName} was marked as processed but cached result not found for key {IdempotencyKey}", requestName, idempotencyKey);
        }

        // Process the command
        _logger.LogDebug("Processing {RequestName} with key {IdempotencyKey}", requestName, idempotencyKey);

        var response = await next().ConfigureAwait(false);

        // Cache the result for future idempotency checks
        try
        {
            await _idempotencyService.SetResultAsync(idempotencyKey, response, null, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cached result for {RequestName} with key {IdempotencyKey}", requestName, idempotencyKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache result for {RequestName} with key {IdempotencyKey}", requestName, idempotencyKey);
            // Don't fail the request if caching fails, just log the error
        }

        return response;
    }

    /// <summary>
    /// Determines if the request is a command that should have idempotency applied.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if the request is a command; otherwise, false.</returns>
    private static bool IsCommand(TRequest request)
    {
        var requestType = request.GetType();

        // Check for ICommand interface
        return requestType.GetInterfaces().Any(i =>
            i.Name == "ICommand" ||
            (i.IsGenericType && i.GetGenericTypeDefinition().Name == "ICommand`1"));
    }

    /// <summary>
    /// Generates a unique idempotency key based on the request content and type.
    /// </summary>
    /// <param name="request">The request to generate a key for.</param>
    /// <returns>A unique idempotency key.</returns>
    private string GenerateIdempotencyKey(TRequest request)
    {
        try
        {
            // Serialize the request to JSON for consistent hashing
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var requestType = typeof(TRequest).FullName ?? typeof(TRequest).Name;

            // Combine type name and serialized content
            var combinedContent = $"{requestType}:{requestJson}";

            // Generate SHA256 hash for the key
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedContent));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return $"idempotency:{hash}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate idempotency key for {RequestType}, falling back to type-based key", typeof(TRequest).Name);

            // Fallback: use type name and a simple hash of the request
            var fallbackContent = $"{typeof(TRequest).Name}:{request.GetHashCode()}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackContent));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return $"idempotency:fallback:{hash}";
        }
    }
}
