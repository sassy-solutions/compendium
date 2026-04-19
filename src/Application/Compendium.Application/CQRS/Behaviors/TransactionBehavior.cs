// -----------------------------------------------------------------------
// <copyright file="TransactionBehavior.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Compendium.Application.CQRS.Behaviors;

/// <summary>
/// Pipeline behavior that wraps command execution in a database transaction.
/// Provides automatic rollback on failures and commit on success.
/// Only applies to commands that modify data (ICommand implementations).
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response being returned.</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly Func<DbConnection>? _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionFactory">Optional database connection factory for transaction management.</param>
    public TransactionBehavior(
        ILogger<TransactionBehavior<TRequest, TResponse>> logger,
        Func<DbConnection>? connectionFactory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Handles the request within a transaction scope if applicable.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        // Only apply transaction behavior to commands, not queries
        if (!IsCommand(request))
        {
            return await next().ConfigureAwait(false);
        }

        // If no connection factory is provided, execute without explicit transaction management
        if (_connectionFactory is null)
        {
            _logger.LogDebug("No connection factory provided. Executing {RequestType} without explicit transaction management", typeof(TRequest).Name);
            return await next().ConfigureAwait(false);
        }

        var requestName = typeof(TRequest).Name;
        var transactionId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogDebug("Starting transaction {TransactionId} for {RequestName}", transactionId, requestName);

        using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await next().ConfigureAwait(false);

            // Check if the response indicates success
            var isSuccess = IsSuccessfulResponse(response);

            if (isSuccess)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Transaction {TransactionId} committed successfully for {RequestName}", transactionId, requestName);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Transaction {TransactionId} rolled back due to failure response for {RequestName}", transactionId, requestName);
            }

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Transaction {TransactionId} rolled back due to exception for {RequestName}", transactionId, requestName);
            throw;
        }
    }

    /// <summary>
    /// Determines if the request is a command that should be executed within a transaction.
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
