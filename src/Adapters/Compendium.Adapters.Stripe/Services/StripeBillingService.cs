// -----------------------------------------------------------------------
// <copyright file="StripeBillingService.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Compendium.Adapters.Stripe.Configuration;
using Stripe.Checkout;

namespace Compendium.Adapters.Stripe.Services;

/// <summary>
/// Stripe implementation of <see cref="IBillingService"/>. Relies on the
/// Stripe.net SDK which picks up the globally configured
/// <see cref="StripeConfiguration.ApiKey"/> set during DI registration.
/// </summary>
internal sealed class StripeBillingService : IBillingService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeBillingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeBillingService"/> class.
    /// </summary>
    public StripeBillingService(
        IOptions<StripeOptions> options,
        ILogger<StripeBillingService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutSession>> CreateCheckoutSessionAsync(
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var opts = new SessionCreateOptions
            {
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = request.VariantId,
                        Quantity = 1
                    }
                },
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                CustomerEmail = request.Email,
                Metadata = ToMetadata(request.CustomData, request.UserId)
            };

            if (!string.IsNullOrEmpty(request.DiscountCode))
            {
                opts.Discounts = new List<SessionDiscountOptions>
                {
                    new() { PromotionCode = request.DiscountCode }
                };
            }

            var service = new SessionService();
            var session = await service.CreateAsync(opts, cancellationToken: cancellationToken);

            return Result.Success(StripeMapper.ToCheckoutSession(session));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed: {Message}", ex.Message);
            return Result.Failure<CheckoutSession>(
                Error.Failure("Billing.Stripe.CheckoutFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BillingCustomer>> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        try
        {
            var service = new CustomerService();
            var customer = await service.GetAsync(customerId, cancellationToken: cancellationToken);
            return Result.Success(StripeMapper.ToBillingCustomer(customer));
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe customer {CustomerId} not found", customerId);
            return Result.Failure<BillingCustomer>(BillingErrors.CustomerNotFound(customerId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe customer lookup failed for {CustomerId}", customerId);
            return Result.Failure<BillingCustomer>(
                Error.Failure("Billing.Stripe.GetCustomerFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BillingCustomer>> GetCustomerByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        try
        {
            var service = new CustomerService();
            var list = await service.ListAsync(
                new CustomerListOptions { Email = email, Limit = 1 },
                cancellationToken: cancellationToken);

            var customer = list?.Data is { Count: > 0 } data ? data[0] : null;
            if (customer is null)
            {
                return Result.Failure<BillingCustomer>(BillingErrors.CustomerNotFoundByEmail(email));
            }

            return Result.Success(StripeMapper.ToBillingCustomer(customer));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe customer lookup by email failed (activity {ActivityId})", Activity.Current?.Id);
            return Result.Failure<BillingCustomer>(
                Error.Failure("Billing.Stripe.GetCustomerByEmailFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<BillingCustomer>> UpsertCustomerAsync(
        UpsertCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);

        try
        {
            var service = new CustomerService();

            // Find by email first
            var existing = await service.ListAsync(
                new CustomerListOptions { Email = request.Email, Limit = 1 },
                cancellationToken: cancellationToken);

            var metadata = ToMetadata(request.CustomData, request.UserId, request.TenantId);

            global::Stripe.Customer customer;
            if (existing?.Data is { Count: > 0 } data)
            {
                var id = data[0].Id;
                var updateOpts = new CustomerUpdateOptions
                {
                    Name = request.Name,
                    Metadata = metadata,
                    Address = BuildAddress(request.City, request.Region, request.Country)
                };
                customer = await service.UpdateAsync(id, updateOpts, cancellationToken: cancellationToken);
            }
            else
            {
                var createOpts = new CustomerCreateOptions
                {
                    Email = request.Email,
                    Name = request.Name,
                    Metadata = metadata,
                    Address = BuildAddress(request.City, request.Region, request.Country)
                };
                customer = await service.CreateAsync(createOpts, cancellationToken: cancellationToken);
            }

            return Result.Success(StripeMapper.ToBillingCustomer(customer));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe customer upsert failed (activity {ActivityId})", Activity.Current?.Id);
            return Result.Failure<BillingCustomer>(
                Error.Failure("Billing.Stripe.UpsertCustomerFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> CreateCustomerPortalUrlAsync(
        string customerId,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        try
        {
            var service = new global::Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(
                new global::Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = returnUrl ?? "https://example.com/return"
                },
                cancellationToken: cancellationToken);

            return Result.Success(session.Url);
        }
        catch (StripeException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Stripe customer {CustomerId} not found for portal session", customerId);
            return Result.Failure<string>(BillingErrors.CustomerNotFound(customerId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe portal session creation failed for {CustomerId}", customerId);
            return Result.Failure<string>(
                Error.Failure("Billing.Stripe.PortalSessionFailed", ex.Message));
        }
    }

    private static AddressOptions? BuildAddress(string? city, string? region, string? country)
    {
        if (string.IsNullOrWhiteSpace(city) &&
            string.IsNullOrWhiteSpace(region) &&
            string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        return new AddressOptions
        {
            City = city,
            State = region,
            Country = country
        };
    }

    private static Dictionary<string, string>? ToMetadata(
        IReadOnlyDictionary<string, object>? customData,
        string? userId = null,
        string? tenantId = null)
    {
        if ((customData is null || customData.Count == 0) &&
            string.IsNullOrEmpty(userId) &&
            string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (customData is not null)
        {
            foreach (var kvp in customData)
            {
                result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }
        }

        if (!string.IsNullOrEmpty(userId))
        {
            result["user_id"] = userId!;
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            result["tenant_id"] = tenantId!;
        }

        return result;
    }
}
