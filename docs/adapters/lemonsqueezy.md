# Compendium.Adapters.LemonSqueezy

> LemonSqueezy billing adapter: subscriptions, license keys, customer portal, and webhook handling.

## Install

```bash
dotnet add package Compendium.Adapters.LemonSqueezy
```

## Configuration

`appsettings.json`:

```json
{
  "LemonSqueezy": {
    "ApiKey": "ls_...",
    "StoreId": "12345",
    "WebhookSigningSecret": "whsec_...",
    "BaseUrl": "https://api.lemonsqueezy.com/v1/",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "TestMode": false
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.LemonSqueezy;

builder.Services.AddLemonSqueezy(options =>
{
    builder.Configuration.GetSection("LemonSqueezy").Bind(options);
});

// Or bind directly from a configuration section:
builder.Services.AddLemonSqueezy(builder.Configuration.GetSection("LemonSqueezy"));
```

### `LemonSqueezyOptions`

| Property | Default | Description |
|---|---|---|
| `ApiKey` | `""` | LemonSqueezy API key. Required. |
| `StoreId` | `""` | Your LemonSqueezy store ID. Required. |
| `WebhookSigningSecret` | `""` | Webhook signing secret for HMAC-SHA256 validation. **Empty disables validation — dev only.** |
| `BaseUrl` | `"https://api.lemonsqueezy.com/v1/"` | API base URL. |
| `TimeoutSeconds` | `30` | HTTP request timeout. |
| `MaxRetries` | `3` | Retry attempts for transient failures (incl. HTTP 429). |
| `TestMode` | `false` | Convenience flag for test mode. |

## Usage

Resolve the billing/subscription/license ports from DI:

```csharp
public class CheckoutHandler
{
    private readonly IBillingService _billing;
    private readonly ILicenseService _licenses;

    public CheckoutHandler(IBillingService billing, ILicenseService licenses)
    {
        _billing = billing;
        _licenses = licenses;
    }

    public Task<Result<Uri>> StartCheckout(
        string variantId,
        string customerEmail,
        TenantId tenantId,
        CancellationToken ct) =>
        _billing.CreateCheckoutSessionAsync(
            new CheckoutSpec(
                variantId: variantId,
                customerEmail: customerEmail,
                metadata: new() { ["tenant_id"] = tenantId.ToString() }),
            ct);

    public Task<Result<LicenseStatus>> Validate(string licenseKey, CancellationToken ct) =>
        _licenses.ValidateLicenseAsync(licenseKey, ct);
}
```

### Webhooks

Inject `IPaymentWebhookHandler` (resolved to `LemonSqueezyWebhookHandler`) into a minimal endpoint:

```csharp
app.MapPost("/webhooks/lemonsqueezy", async (
    HttpRequest request,
    IPaymentWebhookHandler handler,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(ct);
    var signature = request.Headers["X-Signature"].ToString();

    var result = await handler.ProcessWebhookAsync(payload, signature, ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
});
```

The handler verifies the HMAC-SHA256 signature (with or without the `sha256=` prefix) and parses the documented event types: `subscription_created`, `subscription_updated`, `subscription_cancelled`, `subscription_resumed`, `subscription_expired`, `subscription_paused`, `subscription_unpaused`, `subscription_payment_*`, `order_created`, `order_refunded`, `license_key_created`, `license_key_updated`.

### Multi-tenant

Pass `metadata["tenant_id"]` into checkout sessions; the webhook handler propagates the metadata onto the resulting events.

## Gotchas

- **LemonSqueezy does not support direct customer creation.** `IBillingService.UpsertCustomerAsync` returns an unsupported-operation error — customers are created automatically during checkout. Use `GetCustomerByEmailAsync` to look existing customers up.
- **Customer portal URL requires an active subscription.** `CreateCustomerPortalUrlAsync` looks up the customer's active subscription to derive the portal URL; if the customer has no active subscription it returns a `NoActiveSubscription` error.
- License activation has a per-key activation limit (configured in LemonSqueezy). The adapter detects this in upstream error messages and surfaces a `LicenseActivationLimitReached` error so callers can show a useful message.
- The HTTP client retries on transient errors and HTTP 429 with exponential backoff (`2^attempt` seconds), and breaks the circuit after 5 consecutive transient failures for 30 seconds.
- If `WebhookSigningSecret` is empty, signature validation is bypassed. Never deploy without a signing secret.

## See also

- [API Reference](../api/Compendium.Adapters.LemonSqueezy.html)
- [LemonSqueezy webhooks documentation](https://docs.lemonsqueezy.com/help/webhooks)
- Sample app — coming via POM-182.
