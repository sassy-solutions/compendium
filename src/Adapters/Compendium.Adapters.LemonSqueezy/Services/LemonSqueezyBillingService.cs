// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyBillingService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.LemonSqueezy.Configuration;
using Compendium.Adapters.LemonSqueezy.Http;
using Compendium.Adapters.LemonSqueezy.Http.Models;

namespace Compendium.Adapters.LemonSqueezy.Services;

/// <summary>
/// Implements billing service using LemonSqueezy REST API.
/// </summary>
internal sealed class LemonSqueezyBillingService : IBillingService
{
    private readonly LemonSqueezyHttpClient _httpClient;
    private readonly LemonSqueezyOptions _options;
    private readonly ILogger<LemonSqueezyBillingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LemonSqueezyBillingService"/> class.
    /// </summary>
    public LemonSqueezyBillingService(
        LemonSqueezyHttpClient httpClient,
        IOptions<LemonSqueezyOptions> options,
        ILogger<LemonSqueezyBillingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutSession>> CreateCheckoutSessionAsync(
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Creating checkout session for variant {VariantId}", request.VariantId);

        var lsRequest = new LsCreateCheckoutRequest
        {
            Data = new LsCreateCheckoutRequestData
            {
                Attributes = new LsCreateCheckoutAttributes
                {
                    CheckoutOptions = new LsCheckoutOptions
                    {
                        Embed = request.Embed
                    },
                    CheckoutData = new LsCheckoutData
                    {
                        Email = request.Email,
                        Name = request.Name,
                        DiscountCode = request.DiscountCode,
                        Custom = request.CustomData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    },
                    ProductOptions = new LsCheckoutProductOptions
                    {
                        RedirectUrl = request.SuccessUrl
                    }
                },
                Relationships = new LsCreateCheckoutRelationships
                {
                    Store = new LsRelationshipData
                    {
                        Data = new LsRelationshipDataItem
                        {
                            Type = "stores",
                            Id = _options.StoreId
                        }
                    },
                    Variant = new LsRelationshipData
                    {
                        Data = new LsRelationshipDataItem
                        {
                            Type = "variants",
                            Id = request.VariantId
                        }
                    }
                }
            }
        };

        var result = await _httpClient.CreateCheckoutAsync(lsRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create checkout: {Error}", result.Error.Message);
            return result.Error;
        }

        var checkout = MapToCheckoutSession(result.Value);
        _logger.LogInformation("Created checkout session {CheckoutId}", checkout.Id);

        return checkout;
    }

    /// <inheritdoc />
    public async Task<Result<BillingCustomer>> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        _logger.LogDebug("Getting customer {CustomerId}", customerId);

        var result = await _httpClient.GetCustomerAsync(customerId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "LemonSqueezy.NotFound")
            {
                return BillingErrors.CustomerNotFound(customerId);
            }
            return result.Error;
        }

        return MapToCustomer(result.Value);
    }

    /// <inheritdoc />
    public async Task<Result<BillingCustomer>> GetCustomerByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        _logger.LogDebug("Getting customer by email {Email}", email);

        var result = await _httpClient.ListCustomersAsync(email, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var customer = result.Value.FirstOrDefault();
        if (customer is null)
        {
            return BillingErrors.CustomerNotFoundByEmail(email);
        }

        return MapToCustomer(customer);
    }

    /// <inheritdoc />
    public Task<Result<BillingCustomer>> UpsertCustomerAsync(
        UpsertCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // LemonSqueezy creates customers automatically during checkout
        // There's no direct customer creation API
        _logger.LogWarning("LemonSqueezy does not support direct customer creation. Customers are created during checkout.");

        return Task.FromResult<Result<BillingCustomer>>(
            Error.Failure("LemonSqueezy.UnsupportedOperation",
                "LemonSqueezy does not support direct customer creation. Customers are created during checkout."));
    }

    /// <inheritdoc />
    public async Task<Result<string>> CreateCustomerPortalUrlAsync(
        string customerId,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customerId);

        _logger.LogDebug("Getting customer portal URL for customer {CustomerId}", customerId);

        // Get the customer's active subscriptions to get the portal URL
        var subscriptionsResult = await _httpClient.ListSubscriptionsAsync(customerId, "active", cancellationToken);

        if (subscriptionsResult.IsFailure)
        {
            return subscriptionsResult.Error;
        }

        var subscription = subscriptionsResult.Value.FirstOrDefault();
        if (subscription?.Attributes?.Urls?.CustomerPortal is null)
        {
            return BillingErrors.NoActiveSubscription(customerId);
        }

        return subscription.Attributes.Urls.CustomerPortal;
    }

    // ============================================================================
    // Mapping Helpers
    // ============================================================================

    private static CheckoutSession MapToCheckoutSession(JsonApiResource<LsCheckoutAttributes> resource)
    {
        var attrs = resource.Attributes!;

        return new CheckoutSession
        {
            Id = resource.Id ?? string.Empty,
            CheckoutUrl = attrs.Url ?? string.Empty,
            StoreId = attrs.StoreId?.ToString(),
            VariantId = attrs.VariantId?.ToString(),
            CreatedAt = attrs.CreatedAt ?? DateTimeOffset.UtcNow,
            ExpiresAt = attrs.ExpiresAt,
            CustomData = attrs.CheckoutData?.Custom?.AsReadOnly()
        };
    }

    private static BillingCustomer MapToCustomer(JsonApiResource<LsCustomerAttributes> resource)
    {
        var attrs = resource.Attributes!;

        return new BillingCustomer
        {
            Id = resource.Id ?? string.Empty,
            StoreId = attrs.StoreId?.ToString(),
            Name = attrs.Name,
            Email = attrs.Email ?? string.Empty,
            City = attrs.City,
            Region = attrs.Region,
            Country = attrs.Country,
            TotalRevenueCents = attrs.TotalRevenueCurrency,
            CreatedAt = attrs.CreatedAt ?? DateTimeOffset.MinValue,
            UpdatedAt = attrs.UpdatedAt
        };
    }
}

/// <summary>
/// Extension methods for dictionary operations.
/// </summary>
internal static class DictionaryExtensions
{
    /// <summary>
    /// Converts a dictionary to a read-only dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        return dictionary;
    }
}
