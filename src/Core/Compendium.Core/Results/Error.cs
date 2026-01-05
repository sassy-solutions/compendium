// -----------------------------------------------------------------------
// <copyright file="Error.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Core.Domain.Primitives;

namespace Compendium.Core.Results;

/// <summary>
/// Represents an error that occurred during operation execution.
/// Provides structured error information with code, message, and optional details.
/// </summary>
public sealed class Error : ValueObject
{
    /// <summary>
    /// Represents no error (success state).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Represents a null value error.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.", ErrorType.Failure);

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="type">The error type.</param>
    /// <param name="metadata">Optional metadata associated with the error.</param>
    public Error(string code, string message, ErrorType type, IReadOnlyDictionary<string, object>? metadata = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Type = type;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the error code that uniquely identifies the error type.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type classification.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets additional metadata associated with the error.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Creates a failure error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new failure error.</returns>
    public static Error Failure(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Failure, metadata);
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new validation error.</returns>
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Validation, metadata);
    }

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new not found error.</returns>
    public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.NotFound, metadata);
    }

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new conflict error.</returns>
    public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Conflict, metadata);
    }

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new unauthorized error.</returns>
    public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Unauthorized, metadata);
    }

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new forbidden error.</returns>
    public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Forbidden, metadata);
    }

    /// <summary>
    /// Creates an unavailable error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new unavailable error.</returns>
    public static Error Unavailable(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Unavailable, metadata);
    }

    /// <summary>
    /// Creates a too many requests error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new too many requests error.</returns>
    public static Error TooManyRequests(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.TooManyRequests, metadata);
    }

    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A new unexpected error.</returns>
    public static Error Unexpected(string code, string message, IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new Error(code, message, ErrorType.Unexpected, metadata);
    }

    /// <summary>
    /// Implicitly converts a string to an Error.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static implicit operator Error(string error)
    {
        return Failure("General.Failure", error);
    }

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    /// <returns>A string that represents the current error.</returns>
    public override string ToString()
    {
        return $"Error [Code={Code}, Message={Message}, Type={Type}]";
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Message;
        yield return Type;
    }
}

/// <summary>
/// Defines the types of errors that can occur.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// A general failure occurred.
    /// </summary>
    Failure = 1,

    /// <summary>
    /// A validation error occurred.
    /// </summary>
    Validation = 2,

    /// <summary>
    /// A requested resource was not found.
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// A conflict occurred (e.g., duplicate resource).
    /// </summary>
    Conflict = 4,

    /// <summary>
    /// The request was unauthorized.
    /// </summary>
    Unauthorized = 5,

    /// <summary>
    /// The request was forbidden.
    /// </summary>
    Forbidden = 6,

    /// <summary>
    /// The service is unavailable.
    /// </summary>
    Unavailable = 7,

    /// <summary>
    /// Too many requests were made.
    /// </summary>
    TooManyRequests = 8,

    /// <summary>
    /// An unexpected error occurred.
    /// </summary>
    Unexpected = 9
}
