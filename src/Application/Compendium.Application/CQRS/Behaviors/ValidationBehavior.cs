using System.ComponentModel.DataAnnotations;

namespace Compendium.Application.CQRS.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests using data annotations before processing.
/// Automatically converts validation failures to appropriate Result types.
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Validates the request and processes it through the pipeline if validation passes.
    /// </summary>
    /// <param name="request">The request to validate and process.</param>
    /// <param name="next">The next handler in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults
                .Select(vr => vr.ErrorMessage ?? "Validation error")
                .ToArray();

            var errorMessage = string.Join("; ", errors);

            // For Result<T> responses, we need to handle this differently
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>).MakeGenericType(resultType)
                    .GetMethod(nameof(Result<object>.Failure), new[] { typeof(Error) });

                var error = Error.Validation("Validation.Failed", errorMessage);
                var result = failureMethod?.Invoke(null, new object[] { error });
                return (TResponse)result!;
            }

            // For Result responses
            if (typeof(TResponse) == typeof(Result))
            {
                var error = Error.Validation("Validation.Failed", errorMessage);
                var result = Result.Failure(error);
                return (TResponse)(object)result;
            }
        }

        return await next();
    }
}
