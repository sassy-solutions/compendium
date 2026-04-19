// -----------------------------------------------------------------------
// <copyright file="Result.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Core.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Provides a functional approach to error handling without exceptions.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">A value indicating whether the operation was successful.</param>
    /// <param name="error">The error that occurred, if any.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error that occurred during the operation.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success()
    {
        return new Result(true, Error.None);
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with the specified value.</returns>
    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value, true, Error.None);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with a value type.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error that occurred.</param>
    /// <returns>A failed result.</returns>
    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new Result<TValue>(default, false, error);
    }

    /// <summary>
    /// Creates a result based on a condition.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="error">The error to use if the condition is false.</param>
    /// <returns>A successful result if the condition is true; otherwise, a failed result.</returns>
    public static Result Create(bool condition, Error error)
    {
        return condition ? Success() : Failure(error);
    }

    /// <summary>
    /// Creates a result based on a value condition.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="error">The error to use if the value is null.</param>
    /// <returns>A successful result with the value if it's not null; otherwise, a failed result.</returns>
    public static Result<TValue> Create<TValue>(TValue? value, Error error)
        where TValue : class
    {
        return value is not null ? Success(value) : Failure<TValue>(error);
    }

    /// <summary>
    /// Creates a result based on a nullable value condition.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="error">The error to use if the value is null.</param>
    /// <returns>A successful result with the value if it has a value; otherwise, a failed result.</returns>
    public static Result<TValue> Create<TValue>(TValue? value, Error error)
        where TValue : struct
    {
        return value.HasValue ? Success(value.Value) : Failure<TValue>(error);
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful result if all results are successful; otherwise, a failed result with the first error.</returns>
    public static Result Combine(params Result[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful result if all results are successful; otherwise, a failed result with the first error.</returns>
    public static Result Combine(IEnumerable<Result> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }

    /// <summary>
    /// Implicitly converts an Error to a Result.
    /// </summary>
    /// <param name="error">The error.</param>
    public static implicit operator Result(Error error)
    {
        return Failure(error);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string that represents the current result.</returns>
    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {Error}";
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="TValue">The type of the value returned on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TValue}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="isSuccess">A value indicating whether the operation was successful.</param>
    /// <param name="error">The error that occurred, if any.</param>
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value of the result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing the value of a failed result.</exception>
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access the value of a failed result.");
            }

            return _value!;
        }
    }

    /// <summary>
    /// Implicitly converts a value to a successful Result.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator Result<TValue>(TValue value)
    {
        return Success(value);
    }

    /// <summary>
    /// Implicitly converts an Error to a failed Result.
    /// </summary>
    /// <param name="error">The error.</param>
    public static implicit operator Result<TValue>(Error error)
    {
        return Failure<TValue>(error);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string that represents the current result.</returns>
    public override string ToString()
    {
        return IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
    }
}
