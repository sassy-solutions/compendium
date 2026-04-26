# Compendium.Adapters.Stripe

[Stripe](https://stripe.com/) billing adapter. Implements the billing-provider port from `Compendium.Abstractions.Billing`: customers, subscriptions, checkout sessions, and webhook handling with HMAC validation.

## Install

```bash
dotnet add package Compendium.Adapters.Stripe
```

You need a Stripe account and a secret API key.

## Configuration

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSigningSecret": "whsec_...",
    "ApiVersion": "2024-06-20"
  }
}
```

Options (`StripeOptions`):

| Option | Default | Description |
|---|---|---|
| `SecretKey` | _required_ | Stripe secret key (`sk_live_…` or `sk_test_…`) |
| `PublishableKey` | `null` | Frontend-safe key; surfaced for clients |
| `WebhookSigningSecret` | _strongly recommended_ | `whsec_…` — when empty, signature validation is skipped (dev only) |
| `ApiVersion` | `null` | Pin a specific API version (e.g. `2024-06-20`) |
| `TestMode` | `false` | Convenience flag; the actual mode is determined by the secret-key prefix |

## Usage

```csharp
public sealed class StartSubscriptionHandler(IBillingService billing)
    : ICommandHandler<StartSubscriptionCommand>
{
    public async Task<Result> Handle(StartSubscriptionCommand cmd, CancellationToken ct)
    {
        var customerResult = await billing.EnsureCustomerAsync(
            email: cmd.Email,
            tenantId: cmd.TenantId,
            ct);
        if (customerResult.IsFailure) return customerResult.Error;

        var subResult = await billing.StartSubscriptionAsync(
            customerResult.Value.Id, cmd.PriceId, ct);
        return subResult.IsSuccess ? Result.Success() : subResult.Error;
    }
}
```

For webhook handling, Compendium provides a webhook handler that validates the signature using `WebhookSigningSecret` and emits domain integration events (`SubscriptionCreatedEvent`, `PaymentSucceededEvent`, `InvoicePaidEvent`, etc.) — listed in [`src/Core/Compendium.Core/Domain/Events/Integration/`](https://github.com/sassy-solutions/compendium/tree/main/src/Core/Compendium.Core/Domain/Events/Integration).

## Gotchas

- **Empty `WebhookSigningSecret` skips validation.** That is fine for local development with the Stripe CLI but a security hole in any other environment. The library warns at startup; do not ignore it.
- **Pin `ApiVersion`.** Stripe occasionally ships breaking changes to the default version. Pinning insulates you from surprise behavior changes; bump deliberately.
- **PII in logs.** Customer emails are sent to Stripe (the whole point of the call), but they should *not* be logged on your side. Compendium's logging pattern uses `customer_id` post-creation and `MaskEmail()` from `Compendium.Adapters.Shared` for pre-creation logs. See [POM-178](https://github.com/sassy-solutions/compendium/pull/3) for the GDPR rationale.
- **Idempotency keys.** Stripe supports them; pass through `Stripe-Idempotency-Key` (via `IdempotencyOptions` in your call) on writes that you might retry.
- **Test vs live mode.** A `sk_test_…` key cannot read `sk_live_…` data — fully separate environments. The `TestMode` flag is informational only; the SDK derives the actual mode from the key.

## See also

- [API Reference: Compendium.Adapters.Stripe.Configuration](../api/Compendium.Adapters.Stripe.Configuration.html)
- [Stripe docs](https://stripe.com/docs)
- [`Compendium.Abstractions.Billing`](../api/Compendium.Abstractions.Billing.html) — port contracts
