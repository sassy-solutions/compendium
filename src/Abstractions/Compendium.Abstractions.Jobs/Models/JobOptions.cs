// -----------------------------------------------------------------------
// <copyright file="JobOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Abstractions.Jobs.Models;

/// <summary>
/// Defines the priority bands a background job can be enqueued with.
/// Adapters map these onto provider-specific queues / weights.
/// </summary>
public enum JobPriority
{
    /// <summary>
    /// Low priority work that may be deferred under load.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Default priority for ordinary background work.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Elevated priority — handled before normal work.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority — must run as soon as possible.
    /// </summary>
    Critical = 3,
}

/// <summary>
/// Describes the retry behaviour applied to a background job.
/// </summary>
/// <param name="MaxAttempts">The maximum number of attempts (including the initial run).</param>
/// <param name="InitialBackoff">The initial backoff between retries.</param>
/// <param name="MaxBackoff">The optional cap for the exponential backoff.</param>
public sealed record RetryPolicy(
    int MaxAttempts,
    TimeSpan InitialBackoff,
    TimeSpan? MaxBackoff = null);

/// <summary>
/// Tenant-aware options describing how a background job should be enqueued, scheduled, or recurred.
/// </summary>
/// <param name="TenantId">The tenant the job belongs to. Required so adapters can isolate work per tenant.</param>
/// <param name="Retry">The optional retry policy. Adapters may apply a sensible default when null.</param>
/// <param name="Queue">The optional queue (or task list) name the adapter should target.</param>
/// <param name="Priority">The job priority band.</param>
/// <param name="ScheduledAt">The optional scheduled execution time. Ignored by <c>EnqueueAsync</c>.</param>
public sealed record JobOptions(
    string TenantId,
    RetryPolicy? Retry = null,
    string? Queue = null,
    JobPriority Priority = JobPriority.Normal,
    DateTimeOffset? ScheduledAt = null);
