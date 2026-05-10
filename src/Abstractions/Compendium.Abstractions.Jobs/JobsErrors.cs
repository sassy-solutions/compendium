// -----------------------------------------------------------------------
// <copyright file="JobsErrors.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Jobs;

/// <summary>
/// Provides standard error definitions for background-job operations.
/// </summary>
public static class JobsErrors
{
    /// <summary>
    /// Error returned when the requested job identifier cannot be located by the adapter.
    /// </summary>
    public static Error JobNotFound(string jobId) =>
        Error.NotFound("Jobs.JobNotFound", $"The background job '{jobId}' was not found.");

    /// <summary>
    /// Error returned when the supplied payload cannot be serialized or fails adapter-side validation.
    /// </summary>
    public static Error InvalidPayload(string reason) =>
        Error.Validation("Jobs.InvalidPayload", $"The job payload is invalid: {reason}");

    /// <summary>
    /// Error returned when the supplied cron expression is not parseable by the adapter.
    /// </summary>
    public static Error CronInvalid(string cronExpression) =>
        Error.Validation("Jobs.CronInvalid", $"The cron expression '{cronExpression}' is invalid.");

    /// <summary>
    /// Error returned when the request rate limit has been exceeded.
    /// </summary>
    public static Error RateLimited(string reason) =>
        Error.TooManyRequests("Jobs.RateLimited", $"Rate limit exceeded: {reason}");

    /// <summary>
    /// Error returned when the background-job provider cannot be reached.
    /// </summary>
    public static Error ProviderUnreachable(string reason) =>
        Error.Unavailable("Jobs.ProviderUnreachable", $"The background-job provider is unreachable: {reason}");
}
