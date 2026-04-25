# Compendium.Adapters.Stripe

> Stripe billing adapter: customers, subscriptions, checkout sessions, customer portal, and webhook handling.

## Install

```bash
dotnet add package Compendium.Adapters.Stripe
```

## Configuration

`appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSigningSecret": "whsec_...",
    "TestMode": true
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.Stripe;

builder.Services.AddStripeAdapter(options =>
{
    builder.Configuration.GetSection("Stripe").Bind(options);
});

// Or bind directly from a configuration section:
builder.Services.AddStripeAdapter(builder.Configuration.GetSection("Stripe"));
```

### `StripeOptions`

| Property | Default | Description |
|---|---|---|
| `SecretKey` | `""` | Stripe secret API key (`sk_live_...` / `sk_test_...`). Required. |
| `PublishableKey` | `null` | Optional publishable key (`pk_live_...` / `pk_test_...`). Used by the frontend; not required server-side. |
| `WebhookSigningSecret` | `""` | Webhook signing secret (`whsec_...`) for HMAC-SHA256 validation. **Empty disables signature validation — dev only.** |
| `ApiVersion` | `null` | Optional pinned Stripe API version. See gotcha below. |
| `TestMode` | `false` | Convenience flag. Actual mode is determined by the secret key prefix. |

## Usage

Resolve the billing/subscription ports from DI:

```csharp
public class CheckoutHandler
{
    private readonly IBillingService _billing;
    private readonly ISubscriptionService _subs;

    public CheckoutHandler(IBillingService billing, ISubscriptionService subs)
    {
        _billing = billing;
        _subs = subs;
    }

    public async Task<Result<Uri>> StartCheckout(
        TenantId tenantId,
        string priceId,
        string customerEmail,
        CancellationToken ct)
    {
        var customer = await _billing.UpsertCustomerAsync(
            new CustomerSpec(customerEmail, metadata: new() { ["tenant_id"] = tenantId.ToString() }),
            ct);
        if (customer.IsFailure) return Result<Uri>.Failure(customer.Error);

        return await _billing.CreateCheckoutSessionAsync(
            new CheckoutSpec(customer.Value.Id, priceId, successUrl: "https://app.example.com/done"),
            ct);
    }
}
```

### Webhooks

Inject `IPaymentWebhookHandler` (resolved to `StripeWebhookHandler`) into a minimal endpoint and forward the raw body and `Stripe-Signature` header:

```csharp
app.MapPost("/webhooks/stripe", async (
    HttpRequest request,
    IPaymentWebhookHandler handler,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(ct);
    var signature = request.Headers["Stripe-Signature"].ToString();

    var result = await handler.ProcessWebhookAsync(payload, signature, ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
});
```

The handler verifies the HMAC-SHA256 signature using `WebhookSigningSecret`, parses Stripe `Subscription`, `Customer`, `Checkout.Session`, and `Invoice` events, and extracts the `tenant_id` from object metadata for multi-tenant routing.

### Multi-tenant

Always set `metadata["tenant_id"]` when creating customers, subscriptions, and checkout sessions. The webhook handler reads this metadata to route events to the right tenant; without it, you have to maintain a customer-id → tenant-id map yourself.

## Gotchas

- `ApiVersion` is currently informational. Stripe.net pins the API version at compile time via the SDK package version; pinning a different version requires a custom `StripeClient`, which is out of scope for this adapter. Choose your SDK package version to control the effective API version.
- If `WebhookSigningSecret` is empty, signature validation is bypassed and any payload is accepted. Never deploy without a signing secret.
- `IBillingService` is a shared port across billing providers (Stripe, LemonSqueezy). `UpsertCustomerAsync` works in Stripe; in LemonSqueezy it returns an unsupported-operation error.
- Pause-then-resume of a subscription clears `pause_collection` by sending an empty string to the raw Stripe API parameter — it is not a no-op.

## See also

- [API Reference](../api/Compendium.Adapters.Stripe.html)
- [Stripe webhooks documentation](https://stripe.com/docs/webhooks)
- Sample app — coming via POM-182.
