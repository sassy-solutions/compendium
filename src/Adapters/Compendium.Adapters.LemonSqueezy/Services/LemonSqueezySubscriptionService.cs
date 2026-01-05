// -----------------------------------------------------------------------
// <copyright file="LemonSqueezySubscriptionService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http;
using Compendium.Adapters.LemonSqueezy.Http.Models;

namespace Compendium.Adapters.LemonSqueezy.Services;

/// <summary>
/// Implements subscription service using LemonSqueezy REST API.
/// </summary>
internal sealed class LemonSqueezySubscriptionService : ISubscriptionService
{
    private readonly LemonSqueezyHttpClient _httpClient;
    private readonly LemonSqueezyOptions _options;
    private readonly ILogger<LemonSqueezySubscriptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LemonSqueezySubscriptionService"/> class.
    /// </summary>
    public LemonSqueezySubscriptionService(
        LemonSqueezyHttpClient httpClient,
        IOptions<LemonSqueezyOptions> options,
        ILogger<LemonSqueezySubscriptionService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<Subscription>> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        _logger.LogDebug("Getting subscription {SubscriptionId}", subscriptionId);

        var result = await _httpClient.GetSubscriptionAsync(subscriptionId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "LemonSqueezy.NotFound")
            {
                return BillingErrors.SubscriptionNotFound(subscriptionId);
            }
            return result.Error;
        }

        return MapToSubscription(result.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Subscription?>> GetActiveSubscriptionAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        _logger.LogDebug("Getting active subscription for customer {CustomerId}", customerId);

        var result = await _httpClient.ListSubscriptionsAsync(customerId, "active", cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var subscription = result.Value.FirstOrDefault();
        if (subscription is null)
        {
            return Result<Subscription?>.Success((Subscription?)null);
        }

        return (Subscription?)MapToSubscription(subscription);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Subscription>>> ListSubscriptionsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        _logger.LogDebug("Listing subscriptions for customer {CustomerId}", customerId);

        var result = await _httpClient.ListSubscriptionsAsync(customerId, null, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        IReadOnlyList<Subscription> subscriptions = result.Value
            .Select(MapToSubscription)
            .ToList();

        return Result.Success(subscriptions);
    }

    /// <inheritdoc />
    public async Task<Result> CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        _logger.LogInformation("Canceling subscription {SubscriptionId}", subscriptionId);

        // Check if already cancelled
        var getResult = await _httpClient.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (getResult.IsFailure)
        {
            if (getResult.Error.Code == "LemonSqueezy.NotFound")
            {
                return BillingErrors.SubscriptionNotFound(subscriptionId);
            }
            return getResult.Error;
        }

        if (getResult.Value.Attributes?.Cancelled == true)
        {
            return BillingErrors.SubscriptionAlreadyCanceled(subscriptionId);
        }

        // Cancel by deleting the subscription
        var result = await _httpClient.DeleteSubscriptionAsync(subscriptionId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to cancel subscription {SubscriptionId}: {Error}",
                subscriptionId, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> PauseSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        _logger.LogInformation("Pausing subscription {SubscriptionId}", subscriptionId);

        var request = new LsUpdateSubscriptionRequest
        {
            Data = new LsUpdateSubscriptionRequestData
            {
                Id = subscriptionId,
                Attributes = new LsUpdateSubscriptionAttributes
                {
                    Pause = new LsSubscriptionPause
                    {
                        Mode = "void"  // Pause at end of current period
                    }
                }
            }
        };

        var result = await _httpClient.UpdateSubscriptionAsync(subscriptionId, request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to pause subscription {SubscriptionId}: {Error}",
                subscriptionId, result.Error.Message);
            return result.Error;
        }

        _logger.LogInformation("Paused subscription {SubscriptionId}", subscriptionId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResumeSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);

        _logger.LogInformation("Resuming subscription {SubscriptionId}", subscriptionId);

        // Check if subscription is paused
        var getResult = await _httpClient.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (getResult.IsFailure)
        {
            if (getResult.Error.Code == "LemonSqueezy.NotFound")
            {
                return BillingErrors.SubscriptionNotFound(subscriptionId);
            }
            return getResult.Error;
        }

        if (getResult.Value.Attributes?.Pause is null)
        {
            return BillingErrors.SubscriptionNotPaused(subscriptionId);
        }

        var request = new LsUpdateSubscriptionRequest
        {
            Data = new LsUpdateSubscriptionRequestData
            {
                Id = subscriptionId,
                Attributes = new LsUpdateSubscriptionAttributes
                {
                    Pause = null  // Remove pause to resume
                }
            }
        };

        var result = await _httpClient.UpdateSubscriptionAsync(subscriptionId, request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to resume subscription {SubscriptionId}: {Error}",
                subscriptionId, result.Error.Message);
            return result.Error;
        }

        _logger.LogInformation("Resumed subscription {SubscriptionId}", subscriptionId);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<Subscription>> UpdateSubscriptionAsync(
        string subscriptionId,
        string newVariantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(newVariantId);

        _logger.LogInformation("Updating subscription {SubscriptionId} to variant {VariantId}",
            subscriptionId, newVariantId);

        if (!int.TryParse(newVariantId, out var variantIdInt))
        {
            return BillingErrors.VariantNotFound(newVariantId);
        }

        var request = new LsUpdateSubscriptionRequest
        {
            Data = new LsUpdateSubscriptionRequestData
            {
                Id = subscriptionId,
                Attributes = new LsUpdateSubscriptionAttributes
                {
                    VariantId = variantIdInt
                }
            }
        };

        var result = await _httpClient.UpdateSubscriptionAsync(subscriptionId, request, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to update subscription {SubscriptionId}: {Error}",
                subscriptionId, result.Error.Message);
            return result.Error;
        }

        _logger.LogInformation("Updated subscription {SubscriptionId} to variant {VariantId}",
            subscriptionId, newVariantId);

        return MapToSubscription(result.Value);
    }

    // ============================================================================
    // Mapping Helpers
    // ============================================================================

    private static Subscription MapToSubscription(JsonApiResource<LsSubscriptionAttributes> resource)
    {
        var attrs = resource.Attributes!;

        return new Subscription
        {
            Id = resource.Id ?? string.Empty,
            CustomerId = attrs.CustomerId?.ToString() ?? string.Empty,
            ProductId = attrs.ProductId?.ToString() ?? string.Empty,
            VariantId = attrs.VariantId?.ToString() ?? string.Empty,
            ProductName = attrs.ProductName,
            VariantName = attrs.VariantName,
            Status = MapSubscriptionStatus(attrs.Status, attrs.Cancelled, attrs.Pause),
            CreatedAt = attrs.CreatedAt ?? DateTimeOffset.MinValue,
            UpdatedAt = attrs.UpdatedAt,
            CurrentPeriodEnd = attrs.RenewsAt,
            CanceledAt = attrs.Cancelled == true ? attrs.UpdatedAt : null,
            EndedAt = attrs.EndsAt,
            TrialEndsAt = attrs.TrialEndsAt,
            PausedAt = attrs.Pause is not null ? attrs.UpdatedAt : null,
            ResumesAt = attrs.Pause?.ResumesAt
        };
    }

    private static BillingSubscriptionStatus MapSubscriptionStatus(
        string? status,
        bool? cancelled,
        LsSubscriptionPause? pause)
    {
        if (cancelled == true)
        {
            return BillingSubscriptionStatus.Cancelled;
        }

        if (pause is not null)
        {
            return BillingSubscriptionStatus.Paused;
        }

        return status?.ToLowerInvariant() switch
        {
            "on_trial" => BillingSubscriptionStatus.OnTrial,
            "active" => BillingSubscriptionStatus.Active,
            "paused" => BillingSubscriptionStatus.Paused,
            "past_due" => BillingSubscriptionStatus.PastDue,
            "unpaid" => BillingSubscriptionStatus.Unpaid,
            "cancelled" => BillingSubscriptionStatus.Cancelled,
            "expired" => BillingSubscriptionStatus.Expired,
            _ => BillingSubscriptionStatus.Active
        };
    }
}
