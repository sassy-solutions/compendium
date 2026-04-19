// -----------------------------------------------------------------------
// <copyright file="StripeSubscriptionService.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.Stripe.Services;

/// <summary>
/// Stripe implementation of <see cref="ISubscriptionService"/>.
/// </summary>
internal sealed class StripeSubscriptionService : ISubscriptionService
{
    private readonly ILogger<StripeSubscriptionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeSubscriptionService"/> class.
    /// </summary>
    public StripeSubscriptionService(ILogger<StripeSubscriptionService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<Subscription>> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        try
        {
            var service = new SubscriptionService();
            var sub = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);
            return Result.Success(StripeMapper.ToSubscription(sub));
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} not found", subscriptionId);
            return Result.Failure<Subscription>(BillingErrors.SubscriptionNotFound(subscriptionId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription lookup failed for {SubscriptionId}", subscriptionId);
            return Result.Failure<Subscription>(
                Error.Failure("Billing.Stripe.GetSubscriptionFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Subscription?>> GetActiveSubscriptionAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        try
        {
            var service = new SubscriptionService();
            var list = await service.ListAsync(
                new SubscriptionListOptions
                {
                    Customer = customerId,
                    Status = "active",
                    Limit = 1
                },
                cancellationToken: cancellationToken);

            if (list?.Data is { Count: > 0 } data)
            {
                return Result.Success<Subscription?>(StripeMapper.ToSubscription(data[0]));
            }

            return Result.Success<Subscription?>(null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe active-subscription lookup failed for customer {CustomerId}", customerId);
            return Result.Failure<Subscription?>(
                Error.Failure("Billing.Stripe.GetActiveSubscriptionFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Subscription>>> ListSubscriptionsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        try
        {
            var service = new SubscriptionService();
            var list = await service.ListAsync(
                new SubscriptionListOptions { Customer = customerId, Limit = 100 },
                cancellationToken: cancellationToken);

            var results = new List<Subscription>(list?.Data?.Count ?? 0);
            if (list?.Data is { } data)
            {
                foreach (var sub in data)
                {
                    results.Add(StripeMapper.ToSubscription(sub));
                }
            }

            return Result.Success<IReadOnlyList<Subscription>>(results);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription listing failed for customer {CustomerId}", customerId);
            return Result.Failure<IReadOnlyList<Subscription>>(
                Error.Failure("Billing.Stripe.ListSubscriptionsFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        try
        {
            var service = new SubscriptionService();
            await service.CancelAsync(
                subscriptionId,
                new SubscriptionCancelOptions(),
                cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} not found for cancel", subscriptionId);
            return Result.Failure(BillingErrors.SubscriptionNotFound(subscriptionId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription cancel failed for {SubscriptionId}", subscriptionId);
            return Result.Failure(
                Error.Failure("Billing.Stripe.CancelSubscriptionFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> PauseSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        try
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    PauseCollection = new SubscriptionPauseCollectionOptions
                    {
                        Behavior = "mark_uncollectible"
                    }
                },
                cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} not found for pause", subscriptionId);
            return Result.Failure(BillingErrors.SubscriptionNotFound(subscriptionId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription pause failed for {SubscriptionId}", subscriptionId);
            return Result.Failure(
                Error.Failure("Billing.Stripe.PauseSubscriptionFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ResumeSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        try
        {
            var service = new SubscriptionService();
            var opts = new SubscriptionUpdateOptions();
            // Clear pause_collection by sending an empty string to the raw API param.
            opts.AddExtraParam("pause_collection", string.Empty);

            await service.UpdateAsync(subscriptionId, opts, cancellationToken: cancellationToken);
            return Result.Success();
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} not found for resume", subscriptionId);
            return Result.Failure(BillingErrors.SubscriptionNotFound(subscriptionId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription resume failed for {SubscriptionId}", subscriptionId);
            return Result.Failure(
                Error.Failure("Billing.Stripe.ResumeSubscriptionFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Subscription>> UpdateSubscriptionAsync(
        string subscriptionId,
        string newVariantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newVariantId);

        try
        {
            var service = new SubscriptionService();
            var current = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);

            var firstItem = current.Items?.Data is { Count: > 0 } items ? items[0] : null;
            if (firstItem is null)
            {
                _logger.LogWarning("Stripe subscription {SubscriptionId} has no items", subscriptionId);
                return Result.Failure<Subscription>(
                    Error.Failure("Billing.Stripe.UpdateSubscriptionFailed", "Subscription has no items."));
            }

            var updated = await service.UpdateAsync(
                subscriptionId,
                new SubscriptionUpdateOptions
                {
                    Items = new List<SubscriptionItemOptions>
                    {
                        new()
                        {
                            Id = firstItem.Id,
                            Price = newVariantId
                        }
                    }
                },
                cancellationToken: cancellationToken);

            return Result.Success(StripeMapper.ToSubscription(updated));
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} not found for update", subscriptionId);
            return Result.Failure<Subscription>(BillingErrors.SubscriptionNotFound(subscriptionId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe subscription update failed for {SubscriptionId}", subscriptionId);
            return Result.Failure<Subscription>(
                Error.Failure("Billing.Stripe.UpdateSubscriptionFailed", ex.Message));
        }
    }
}
