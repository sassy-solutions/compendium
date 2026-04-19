// -----------------------------------------------------------------------
// <copyright file="ResultExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Results;

/// <summary>
/// Extension methods for the Result types providing functional programming patterns.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes a function if the result is successful.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <returns>The original result.</returns>
    public static Result<TValue> Tap<TValue>(this Result<TValue> result, Action<TValue> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsSuccess)
        {
            onSuccess(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes a function if the result is failed.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onFailure">The function to execute on failure.</param>
    /// <returns>The original result.</returns>
    public static Result<TValue> TapError<TValue>(this Result<TValue> result, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (result.IsFailure)
        {
            onFailure(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Maps the value of a successful result to a new value.
    /// </summary>
    /// <typeparam name="TValue">The type of the original value.</typeparam>
    /// <typeparam name="TNewValue">The type of the new value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the mapped value if successful; otherwise, the original error.</returns>
    public static Result<TNewValue> Map<TValue, TNewValue>(this Result<TValue> result, Func<TValue, TNewValue> mapper)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return result.IsSuccess
            ? Result.Success(mapper(result.Value))
            : Result.Failure<TNewValue>(result.Error);
    }

    /// <summary>
    /// Binds the result to a new result-returning function.
    /// </summary>
    /// <typeparam name="TValue">The type of the original value.</typeparam>
    /// <typeparam name="TNewValue">The type of the new value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>The result of the binding function if successful; otherwise, the original error.</returns>
    public static Result<TNewValue> Bind<TValue, TNewValue>(this Result<TValue> result, Func<TValue, Result<TNewValue>> binder)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsSuccess
            ? binder(result.Value)
            : Result.Failure<TNewValue>(result.Error);
    }

    /// <summary>
    /// Matches the result and executes the appropriate function.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onFailure">The function to execute on failure.</param>
    /// <returns>The result of the executed function.</returns>
    public static TResult Match<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);
    }

    /// <summary>
    /// Matches the result and executes the appropriate action.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">The action to execute on success.</param>
    /// <param name="onFailure">The action to execute on failure.</param>
    public static void Match<TValue>(
        this Result<TValue> result,
        Action<TValue> onSuccess,
        Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (result.IsSuccess)
        {
            onSuccess(result.Value);
        }
        else
        {
            onFailure(result.Error);
        }
    }

    /// <summary>
    /// Converts a Result to a Result&lt;TValue&gt;.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="value">The value to use if the result is successful.</param>
    /// <returns>A Result&lt;TValue&gt; with the specified value if successful; otherwise, the original error.</returns>
    public static Result<TValue> Map<TValue>(this Result result, TValue value)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Result.Success(value)
            : Result.Failure<TValue>(result.Error);
    }

    /// <summary>
    /// Asynchronously maps the value of a successful result to a new value.
    /// </summary>
    /// <typeparam name="TValue">The type of the original value.</typeparam>
    /// <typeparam name="TNewValue">The type of the new value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A task representing the asynchronous operation with the mapped result.</returns>
    public static async Task<Result<TNewValue>> MapAsync<TValue, TNewValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, TNewValue> mapper)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// Asynchronously binds the result to a new result-returning function.
    /// </summary>
    /// <typeparam name="TValue">The type of the original value.</typeparam>
    /// <typeparam name="TNewValue">The type of the new value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>A task representing the asynchronous operation with the bound result.</returns>
    public static async Task<Result<TNewValue>> BindAsync<TValue, TNewValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task<Result<TNewValue>>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : Result.Failure<TNewValue>(result.Error);
    }

    /// <summary>
    /// Asynchronously executes a function if the result is successful.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <returns>A task representing the asynchronous operation with the original result.</returns>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Task<Result<TValue>> resultTask,
        Func<TValue, Task> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onSuccess);

        var result = await resultTask.ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await onSuccess(result.Value).ConfigureAwait(false);
        }

        return result;
    }
}
