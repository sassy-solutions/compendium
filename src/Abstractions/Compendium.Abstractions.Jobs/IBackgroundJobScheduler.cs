// -----------------------------------------------------------------------
// <copyright file="IBackgroundJobScheduler.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.Jobs.Models;

namespace Compendium.Abstractions.Jobs;

/// <summary>
/// Provides operations for enqueueing, scheduling, recurring, and cancelling background jobs.
/// This interface is provider-agnostic and can be implemented by adapters targeting
/// Hangfire, Temporal, Quartz, or similar engines.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueues a job for immediate background execution.
    /// </summary>
    /// <typeparam name="TPayload">The payload type carried by the job.</typeparam>
    /// <param name="jobName">The logical job name used to route to a handler.</param>
    /// <param name="payload">The payload to deliver to the handler.</param>
    /// <param name="opts">The tenant-aware job options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the adapter-assigned job identifier or an error.</returns>
    Task<Result<string>> EnqueueAsync<TPayload>(
        string jobName,
        TPayload payload,
        JobOptions opts,
        CancellationToken ct);

    /// <summary>
    /// Schedules a job for one-shot execution at the supplied future time.
    /// </summary>
    /// <typeparam name="TPayload">The payload type carried by the job.</typeparam>
    /// <param name="jobName">The logical job name used to route to a handler.</param>
    /// <param name="payload">The payload to deliver to the handler.</param>
    /// <param name="runAt">The absolute time at which the job should run.</param>
    /// <param name="opts">The tenant-aware job options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the adapter-assigned job identifier or an error.</returns>
    Task<Result<string>> ScheduleAsync<TPayload>(
        string jobName,
        TPayload payload,
        DateTimeOffset runAt,
        JobOptions opts,
        CancellationToken ct);

    /// <summary>
    /// Registers (or replaces) a recurring job described by a cron expression.
    /// </summary>
    /// <typeparam name="TPayload">The payload type carried by the job.</typeparam>
    /// <param name="jobName">The logical job name used to route to a handler. Acts as the recurrence key.</param>
    /// <param name="payload">The payload to deliver to the handler on every tick.</param>
    /// <param name="cronExpression">The cron expression describing the recurrence.</param>
    /// <param name="opts">The tenant-aware job options.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the adapter-assigned recurring-job identifier or an error.</returns>
    Task<Result<string>> RecurAsync<TPayload>(
        string jobName,
        TPayload payload,
        string cronExpression,
        JobOptions opts,
        CancellationToken ct);

    /// <summary>
    /// Cancels a previously enqueued, scheduled, or recurring job by adapter-assigned identifier.
    /// </summary>
    /// <param name="jobId">The adapter-assigned job identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result indicating whether the cancellation succeeded.</returns>
    Task<Result> CancelAsync(string jobId, CancellationToken ct);
}
