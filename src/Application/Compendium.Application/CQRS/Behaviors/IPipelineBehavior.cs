namespace Compendium.Application.CQRS.Behaviors;

/// <summary>
/// Defines a pipeline behavior that can be used to add cross-cutting concerns
/// such as validation, logging, caching, or authorization to request handling.
/// </summary>
/// <typeparam name="TRequest">The type of the request being processed.</typeparam>
/// <typeparam name="TResponse">The type of the response being returned.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the request asynchronously and can intercept or modify the request/response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a delegate for the next handler in the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>A task representing the asynchronous operation with the response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
