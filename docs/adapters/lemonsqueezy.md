# Compendium.Adapters.LemonSqueezy

[LemonSqueezy](https://www.lemonsqueezy.com/) billing adapter — alternative to Stripe, focused on merchant-of-record billing and license-key delivery for software products. Implements the same `Compendium.Abstractions.Billing` port as the Stripe adapter, so swapping between them is a DI change.

## Install

```bash
dotnet add package Compendium.Adapters.LemonSqueezy
```

## Configuration

```json
{
  "LemonSqueezy": {
    "ApiKey": "eyJ0eXAi...",
    "StoreId": "12345",
    "WebhookSigningSecret": "your-webhook-secret",
    "BaseUrl": "https://api.lemonsqueezy.com/v1/",
    "TimeoutSeconds": 30
  }
}
```

Options (`LemonSqueezyOptions`):

| Option | Default | Description |
|---|---|---|
| `ApiKey` | _required_ | LemonSqueezy API key (Bearer token) |
| `StoreId` | _required_ | Your store ID |
| `WebhookSigningSecret` | _required for webhooks_ | HMAC-SHA256 secret used by the webhook endpoint |
| `BaseUrl` | `https://api.lemonsqueezy.com/v1/` | API base URL |
| `TimeoutSeconds` | `30` | HTTP timeout |
| `MaxRetries` | `3` | Retry attempts on transient failures |
| `TestMode` | `false` | Whether to flag operations as test |

## Usage

The adapter exposes:

- `IBillingService` — customers, subscriptions, checkouts (same shape as Stripe)
- `ILicenseService` — license key validation, activation, deactivation (LS-specific feature)

```csharp
public sealed class ValidateLicenseHandler(ILicenseService licenses)
    : IQueryHandler<ValidateLicenseQuery, LicenseValidationResult>
{
    public Task<Result<LicenseValidationResult>> Handle(
        ValidateLicenseQuery q, CancellationToken ct)
        => licenses.ValidateLicenseAsync(q.LicenseKey, q.InstanceId, ct);
}
```

The license API is used for software activation flows — when you want to ship a downloadable app and gate it behind a key.

## Gotchas

- **JSON:API for resources, plain JSON for license endpoints.** LemonSqueezy uses JSON:API for customers/subscriptions/checkouts, but its license endpoints (`/licenses/validate`, `/licenses/activate`, `/licenses/deactivate`) return plain JSON. The adapter handles both internally; don't be surprised if you debug the wire format and see two shapes.
- **License keys are secrets — `licenseKeyId` is not.** The `licenseKeyId` returned by the API identifies a license record (like a customer ID) and is fine to log. The `licenseKey` itself is sensitive; Compendium masks it via `GetKeyShort` in service-layer logs and never includes it in URL paths.
- **Activation limits.** A license key can only be activated on N machines (configured per product variant). Hitting the limit returns a specific error code; your UX should distinguish "invalid key" from "limit reached."
- **Webhook signature.** Set `WebhookSigningSecret` and verify HMAC in your webhook endpoint. Without it, anyone can POST events to your service.

## See also

- [API Reference: Compendium.Adapters.LemonSqueezy.Configuration](../api/Compendium.Adapters.LemonSqueezy.Configuration.html)
- [LemonSqueezy docs](https://docs.lemonsqueezy.com/)
- [Stripe adapter](stripe.md) — same port, different provider
