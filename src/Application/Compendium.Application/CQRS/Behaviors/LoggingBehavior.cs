// -----------------------------------------------------------------------
// <copyright file="LoggingBehavior.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.CQRS.Behaviors;

/// <summary>
/// Pipeline behavior that provides comprehensive logging for request processing.
/// Logs request start, completion, performance metrics, and any failures.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response being returned.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the request with comprehensive logging including performance metrics.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "Processing request {RequestName} with ID {RequestId}",
            requestName, requestId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();

            var isSuccess = IsSuccessfulResponse(response);
            var logLevel = isSuccess ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel,
                "Completed request {RequestName} with ID {RequestId} in {ElapsedMs}ms. Success: {Success}",
                requestName, requestId, stopwatch.ElapsedMilliseconds, isSuccess);

            if (stopwatch.ElapsedMilliseconds > 1000) // Log warning for slow requests
            {
                _logger.LogWarning(
                    "Slow request detected: {RequestName} with ID {RequestId} took {ElapsedMs}ms",
                    requestName, requestId, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Request {RequestName} with ID {RequestId} failed after {ElapsedMs}ms: {ErrorMessage}",
                requestName, requestId, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Determines if the response indicates a successful operation.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <returns>True if the response indicates success; otherwise, false.</returns>
    private static bool IsSuccessfulResponse(TResponse response)
    {
        // Handle Result types
        if (response is Result result)
        {
            return result.IsSuccess;
        }

        // Handle Result<T> types
        if (response.GetType().IsGenericType && response.GetType().GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccessProperty = response.GetType().GetProperty("IsSuccess");
            return isSuccessProperty?.GetValue(response) as bool? ?? true;
        }

        // For other types, assume success if not null
        return response is not null;
    }
}
