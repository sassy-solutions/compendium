// -----------------------------------------------------------------------
// <copyright file="LemonSqueezyApiModels.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.LemonSqueezy.Http.Models;

// ============================================================================
// JSON:API Response Wrappers
// ============================================================================

/// <summary>
/// JSON:API response wrapper for a single resource.
/// </summary>
internal sealed record JsonApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("meta")]
    public JsonApiMeta? Meta { get; init; }

    [JsonPropertyName("jsonapi")]
    public JsonApiVersion? JsonApi { get; init; }

    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; init; }
}

/// <summary>
/// JSON:API response wrapper for a collection of resources.
/// </summary>
internal sealed record JsonApiCollectionResponse<T>
{
    [JsonPropertyName("data")]
    public List<T>? Data { get; init; }

    [JsonPropertyName("meta")]
    public JsonApiCollectionMeta? Meta { get; init; }

    [JsonPropertyName("jsonapi")]
    public JsonApiVersion? JsonApi { get; init; }

    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; init; }
}

/// <summary>
/// JSON:API version information.
/// </summary>
internal sealed record JsonApiVersion
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>
/// JSON:API links.
/// </summary>
internal sealed record JsonApiLinks
{
    [JsonPropertyName("self")]
    public string? Self { get; init; }

    [JsonPropertyName("first")]
    public string? First { get; init; }

    [JsonPropertyName("last")]
    public string? Last { get; init; }

    [JsonPropertyName("next")]
    public string? Next { get; init; }

    [JsonPropertyName("prev")]
    public string? Prev { get; init; }
}

/// <summary>
/// JSON:API meta for single resource.
/// </summary>
internal sealed record JsonApiMeta
{
    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

/// <summary>
/// JSON:API meta for collections.
/// </summary>
internal sealed record JsonApiCollectionMeta
{
    [JsonPropertyName("page")]
    public JsonApiPageMeta? Page { get; init; }
}

/// <summary>
/// JSON:API pagination meta.
/// </summary>
internal sealed record JsonApiPageMeta
{
    [JsonPropertyName("currentPage")]
    public int? CurrentPage { get; init; }

    [JsonPropertyName("from")]
    public int? From { get; init; }

    [JsonPropertyName("lastPage")]
    public int? LastPage { get; init; }

    [JsonPropertyName("perPage")]
    public int? PerPage { get; init; }

    [JsonPropertyName("to")]
    public int? To { get; init; }

    [JsonPropertyName("total")]
    public int? Total { get; init; }
}

/// <summary>
/// JSON:API resource wrapper.
/// </summary>
internal sealed record JsonApiResource<TAttributes>
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("attributes")]
    public TAttributes? Attributes { get; init; }

    [JsonPropertyName("relationships")]
    public Dictionary<string, JsonApiRelationship>? Relationships { get; init; }

    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; init; }
}

/// <summary>
/// JSON:API relationship.
/// </summary>
internal sealed record JsonApiRelationship
{
    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; init; }
}

// ============================================================================
// Customer Models
// ============================================================================

/// <summary>
/// LemonSqueezy customer attributes.
/// </summary>
internal sealed record LsCustomerAttributes
{
    [JsonPropertyName("store_id")]
    public int? StoreId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("total_revenue_currency")]
    public int? TotalRevenueCurrency { get; init; }

    [JsonPropertyName("mrr")]
    public int? Mrr { get; init; }

    [JsonPropertyName("status_formatted")]
    public string? StatusFormatted { get; init; }

    [JsonPropertyName("country_formatted")]
    public string? CountryFormatted { get; init; }

    [JsonPropertyName("total_revenue_currency_formatted")]
    public string? TotalRevenueCurrencyFormatted { get; init; }

    [JsonPropertyName("mrr_formatted")]
    public string? MrrFormatted { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

// ============================================================================
// Subscription Models
// ============================================================================

/// <summary>
/// LemonSqueezy subscription attributes.
/// </summary>
internal sealed record LsSubscriptionAttributes
{
    [JsonPropertyName("store_id")]
    public int? StoreId { get; init; }

    [JsonPropertyName("customer_id")]
    public int? CustomerId { get; init; }

    [JsonPropertyName("order_id")]
    public int? OrderId { get; init; }

    [JsonPropertyName("order_item_id")]
    public int? OrderItemId { get; init; }

    [JsonPropertyName("product_id")]
    public int? ProductId { get; init; }

    [JsonPropertyName("variant_id")]
    public int? VariantId { get; init; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; init; }

    [JsonPropertyName("variant_name")]
    public string? VariantName { get; init; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; init; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("status_formatted")]
    public string? StatusFormatted { get; init; }

    [JsonPropertyName("card_brand")]
    public string? CardBrand { get; init; }

    [JsonPropertyName("card_last_four")]
    public string? CardLastFour { get; init; }

    [JsonPropertyName("pause")]
    public LsSubscriptionPause? Pause { get; init; }

    [JsonPropertyName("cancelled")]
    public bool? Cancelled { get; init; }

    [JsonPropertyName("trial_ends_at")]
    public DateTimeOffset? TrialEndsAt { get; init; }

    [JsonPropertyName("billing_anchor")]
    public int? BillingAnchor { get; init; }

    [JsonPropertyName("first_subscription_item")]
    public LsSubscriptionItem? FirstSubscriptionItem { get; init; }

    [JsonPropertyName("urls")]
    public LsSubscriptionUrls? Urls { get; init; }

    [JsonPropertyName("renews_at")]
    public DateTimeOffset? RenewsAt { get; init; }

    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

/// <summary>
/// LemonSqueezy subscription pause information.
/// </summary>
internal sealed record LsSubscriptionPause
{
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    [JsonPropertyName("resumes_at")]
    public DateTimeOffset? ResumesAt { get; init; }
}

/// <summary>
/// LemonSqueezy subscription item.
/// </summary>
internal sealed record LsSubscriptionItem
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("subscription_id")]
    public int? SubscriptionId { get; init; }

    [JsonPropertyName("price_id")]
    public int? PriceId { get; init; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }

    [JsonPropertyName("is_usage_based")]
    public bool? IsUsageBased { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// LemonSqueezy subscription URLs.
/// </summary>
internal sealed record LsSubscriptionUrls
{
    [JsonPropertyName("update_payment_method")]
    public string? UpdatePaymentMethod { get; init; }

    [JsonPropertyName("customer_portal")]
    public string? CustomerPortal { get; init; }
}

// ============================================================================
// Checkout Models
// ============================================================================

/// <summary>
/// LemonSqueezy checkout attributes.
/// </summary>
internal sealed record LsCheckoutAttributes
{
    [JsonPropertyName("store_id")]
    public int? StoreId { get; init; }

    [JsonPropertyName("variant_id")]
    public int? VariantId { get; init; }

    [JsonPropertyName("custom_price")]
    public int? CustomPrice { get; init; }

    [JsonPropertyName("product_options")]
    public LsCheckoutProductOptions? ProductOptions { get; init; }

    [JsonPropertyName("checkout_options")]
    public LsCheckoutOptions? CheckoutOptions { get; init; }

    [JsonPropertyName("checkout_data")]
    public LsCheckoutData? CheckoutData { get; init; }

    [JsonPropertyName("preview")]
    public LsCheckoutPreview? Preview { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

/// <summary>
/// LemonSqueezy checkout product options.
/// </summary>
internal sealed record LsCheckoutProductOptions
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("media")]
    public List<string>? Media { get; init; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; init; }

    [JsonPropertyName("receipt_button_text")]
    public string? ReceiptButtonText { get; init; }

    [JsonPropertyName("receipt_link_url")]
    public string? ReceiptLinkUrl { get; init; }

    [JsonPropertyName("receipt_thank_you_note")]
    public string? ReceiptThankYouNote { get; init; }

    [JsonPropertyName("enabled_variants")]
    public List<int>? EnabledVariants { get; init; }
}

/// <summary>
/// LemonSqueezy checkout options.
/// </summary>
internal sealed record LsCheckoutOptions
{
    [JsonPropertyName("embed")]
    public bool? Embed { get; init; }

    [JsonPropertyName("media")]
    public bool? Media { get; init; }

    [JsonPropertyName("logo")]
    public bool? Logo { get; init; }

    [JsonPropertyName("desc")]
    public bool? Desc { get; init; }

    [JsonPropertyName("discount")]
    public bool? Discount { get; init; }

    [JsonPropertyName("dark")]
    public bool? Dark { get; init; }

    [JsonPropertyName("subscription_preview")]
    public bool? SubscriptionPreview { get; init; }

    [JsonPropertyName("button_color")]
    public string? ButtonColor { get; init; }
}

/// <summary>
/// LemonSqueezy checkout data.
/// </summary>
internal sealed record LsCheckoutData
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("billing_address")]
    public LsBillingAddress? BillingAddress { get; init; }

    [JsonPropertyName("tax_number")]
    public string? TaxNumber { get; init; }

    [JsonPropertyName("discount_code")]
    public string? DiscountCode { get; init; }

    [JsonPropertyName("custom")]
    public Dictionary<string, object>? Custom { get; init; }

    [JsonPropertyName("variant_quantities")]
    public List<LsVariantQuantity>? VariantQuantities { get; init; }
}

/// <summary>
/// LemonSqueezy billing address.
/// </summary>
internal sealed record LsBillingAddress
{
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("zip")]
    public string? Zip { get; init; }
}

/// <summary>
/// LemonSqueezy variant quantity.
/// </summary>
internal sealed record LsVariantQuantity
{
    [JsonPropertyName("variant_id")]
    public int? VariantId { get; init; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; init; }
}

/// <summary>
/// LemonSqueezy checkout preview.
/// </summary>
internal sealed record LsCheckoutPreview
{
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("currency_rate")]
    public decimal? CurrencyRate { get; init; }

    [JsonPropertyName("subtotal")]
    public int? Subtotal { get; init; }

    [JsonPropertyName("discount_total")]
    public int? DiscountTotal { get; init; }

    [JsonPropertyName("tax")]
    public int? Tax { get; init; }

    [JsonPropertyName("total")]
    public int? Total { get; init; }

    [JsonPropertyName("subtotal_usd")]
    public int? SubtotalUsd { get; init; }

    [JsonPropertyName("discount_total_usd")]
    public int? DiscountTotalUsd { get; init; }

    [JsonPropertyName("tax_usd")]
    public int? TaxUsd { get; init; }

    [JsonPropertyName("total_usd")]
    public int? TotalUsd { get; init; }

    [JsonPropertyName("subtotal_formatted")]
    public string? SubtotalFormatted { get; init; }

    [JsonPropertyName("discount_total_formatted")]
    public string? DiscountTotalFormatted { get; init; }

    [JsonPropertyName("tax_formatted")]
    public string? TaxFormatted { get; init; }

    [JsonPropertyName("total_formatted")]
    public string? TotalFormatted { get; init; }
}

// ============================================================================
// License Key Models
// ============================================================================

/// <summary>
/// LemonSqueezy license key attributes.
/// </summary>
internal sealed record LsLicenseKeyAttributes
{
    [JsonPropertyName("store_id")]
    public int? StoreId { get; init; }

    [JsonPropertyName("customer_id")]
    public int? CustomerId { get; init; }

    [JsonPropertyName("order_id")]
    public int? OrderId { get; init; }

    [JsonPropertyName("order_item_id")]
    public int? OrderItemId { get; init; }

    [JsonPropertyName("product_id")]
    public int? ProductId { get; init; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; init; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("key_short")]
    public string? KeyShort { get; init; }

    [JsonPropertyName("activation_limit")]
    public int? ActivationLimit { get; init; }

    [JsonPropertyName("instances_count")]
    public int? InstancesCount { get; init; }

    [JsonPropertyName("disabled")]
    public bool? Disabled { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("status_formatted")]
    public string? StatusFormatted { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

/// <summary>
/// LemonSqueezy license key instance attributes.
/// </summary>
internal sealed record LsLicenseKeyInstanceAttributes
{
    [JsonPropertyName("license_key_id")]
    public int? LicenseKeyId { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

// ============================================================================
// License API Models (Non-JSON:API endpoints)
// ============================================================================

/// <summary>
/// License validation request.
/// </summary>
internal sealed record LsValidateLicenseRequest
{
    [JsonPropertyName("license_key")]
    public string? LicenseKey { get; init; }

    [JsonPropertyName("instance_id")]
    public string? InstanceId { get; init; }
}

/// <summary>
/// License validation response.
/// </summary>
internal sealed record LsValidateLicenseResponse
{
    [JsonPropertyName("valid")]
    public bool? Valid { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("license_key")]
    public LsLicenseKeyData? LicenseKey { get; init; }

    [JsonPropertyName("instance")]
    public LsLicenseInstanceData? Instance { get; init; }

    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; init; }
}

/// <summary>
/// License activation request.
/// </summary>
internal sealed record LsActivateLicenseRequest
{
    [JsonPropertyName("license_key")]
    public string? LicenseKey { get; init; }

    [JsonPropertyName("instance_name")]
    public string? InstanceName { get; init; }
}

/// <summary>
/// License activation response.
/// </summary>
internal sealed record LsActivateLicenseResponse
{
    [JsonPropertyName("activated")]
    public bool? Activated { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("license_key")]
    public LsLicenseKeyData? LicenseKey { get; init; }

    [JsonPropertyName("instance")]
    public LsLicenseInstanceData? Instance { get; init; }

    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; init; }
}

/// <summary>
/// License deactivation request.
/// </summary>
internal sealed record LsDeactivateLicenseRequest
{
    [JsonPropertyName("license_key")]
    public string? LicenseKey { get; init; }

    [JsonPropertyName("instance_id")]
    public string? InstanceId { get; init; }
}

/// <summary>
/// License deactivation response.
/// </summary>
internal sealed record LsDeactivateLicenseResponse
{
    [JsonPropertyName("deactivated")]
    public bool? Deactivated { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; init; }
}

/// <summary>
/// License key data from validation/activation responses.
/// </summary>
internal sealed record LsLicenseKeyData
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("activation_limit")]
    public int? ActivationLimit { get; init; }

    [JsonPropertyName("activation_usage")]
    public int? ActivationUsage { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

/// <summary>
/// License instance data from validation/activation responses.
/// </summary>
internal sealed record LsLicenseInstanceData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }
}

// ============================================================================
// Webhook Models
// ============================================================================

/// <summary>
/// LemonSqueezy webhook payload.
/// </summary>
internal sealed record LsWebhookPayload
{
    [JsonPropertyName("meta")]
    public LsWebhookMeta? Meta { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

/// <summary>
/// LemonSqueezy webhook meta.
/// </summary>
internal sealed record LsWebhookMeta
{
    [JsonPropertyName("event_name")]
    public string? EventName { get; init; }

    [JsonPropertyName("custom_data")]
    public Dictionary<string, object>? CustomData { get; init; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; init; }
}

// ============================================================================
// Request Models
// ============================================================================

/// <summary>
/// Create checkout request for LemonSqueezy API.
/// </summary>
internal sealed record LsCreateCheckoutRequest
{
    [JsonPropertyName("data")]
    public LsCreateCheckoutRequestData? Data { get; init; }
}

/// <summary>
/// Create checkout request data.
/// </summary>
internal sealed record LsCreateCheckoutRequestData
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "checkouts";

    [JsonPropertyName("attributes")]
    public LsCreateCheckoutAttributes? Attributes { get; init; }

    [JsonPropertyName("relationships")]
    public LsCreateCheckoutRelationships? Relationships { get; init; }
}

/// <summary>
/// Create checkout attributes.
/// </summary>
internal sealed record LsCreateCheckoutAttributes
{
    [JsonPropertyName("checkout_options")]
    public LsCheckoutOptions? CheckoutOptions { get; init; }

    [JsonPropertyName("checkout_data")]
    public LsCheckoutData? CheckoutData { get; init; }

    [JsonPropertyName("product_options")]
    public LsCheckoutProductOptions? ProductOptions { get; init; }

    [JsonPropertyName("custom_price")]
    public int? CustomPrice { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("preview")]
    public bool? Preview { get; init; }
}

/// <summary>
/// Create checkout relationships.
/// </summary>
internal sealed record LsCreateCheckoutRelationships
{
    [JsonPropertyName("store")]
    public LsRelationshipData? Store { get; init; }

    [JsonPropertyName("variant")]
    public LsRelationshipData? Variant { get; init; }
}

/// <summary>
/// Relationship data for JSON:API requests.
/// </summary>
internal sealed record LsRelationshipData
{
    [JsonPropertyName("data")]
    public LsRelationshipDataItem? Data { get; init; }
}

/// <summary>
/// Relationship data item.
/// </summary>
internal sealed record LsRelationshipDataItem
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

/// <summary>
/// Update subscription request for LemonSqueezy API.
/// </summary>
internal sealed record LsUpdateSubscriptionRequest
{
    [JsonPropertyName("data")]
    public LsUpdateSubscriptionRequestData? Data { get; init; }
}

/// <summary>
/// Update subscription request data.
/// </summary>
internal sealed record LsUpdateSubscriptionRequestData
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "subscriptions";

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("attributes")]
    public LsUpdateSubscriptionAttributes? Attributes { get; init; }
}

/// <summary>
/// Update subscription attributes.
/// </summary>
internal sealed record LsUpdateSubscriptionAttributes
{
    [JsonPropertyName("product_id")]
    public int? ProductId { get; init; }

    [JsonPropertyName("variant_id")]
    public int? VariantId { get; init; }

    [JsonPropertyName("pause")]
    public LsSubscriptionPause? Pause { get; init; }

    [JsonPropertyName("cancelled")]
    public bool? Cancelled { get; init; }
}
