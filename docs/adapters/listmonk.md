# Compendium.Adapters.Listmonk

> Listmonk integration: transactional templated email and newsletter subscriber management.

## Install

```bash
dotnet add package Compendium.Adapters.Listmonk
```

## Configuration

`appsettings.json`:

```json
{
  "Listmonk": {
    "BaseUrl": "https://listmonk.example.com",
    "Username": "api",
    "Password": "...",
    "DefaultFromEmail": "no-reply@example.com",
    "DefaultFromName": "Example",
    "DefaultListId": 1,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "SkipSslValidation": false
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.Listmonk;

builder.Services.AddListmonk(options =>
{
    builder.Configuration.GetSection("Listmonk").Bind(options);
});
```

### `ListmonkOptions`

| Property | Default | Description |
|---|---|---|
| `BaseUrl` | `""` | Base URL of the Listmonk instance. Required. |
| `Username` | `""` | Username for HTTP Basic auth. Required. |
| `Password` | `""` | Password for HTTP Basic auth. Required. |
| `DefaultFromEmail` | `""` | Default `From:` email address. |
| `DefaultFromName` | `""` | Default `From:` display name. |
| `DefaultListId` | `null` | Default list ID for new subscribers when none is specified. |
| `TimeoutSeconds` | `30` | HTTP request timeout. |
| `MaxRetries` | `3` | Retry attempts for transient failures (incl. HTTP 429). |
| `SkipSslValidation` | `false` | Disable TLS validation. **Dev only.** |

## Usage

Resolve the email/newsletter ports from DI:

```csharp
public class WelcomeHandler
{
    private readonly IEmailService _email;
    private readonly INewsletterService _newsletter;

    public WelcomeHandler(IEmailService email, INewsletterService newsletter)
    {
        _email = email;
        _newsletter = newsletter;
    }

    public async Task<Result<Unit>> WelcomeAsync(string email, CancellationToken ct)
    {
        var subscribed = await _newsletter.SubscribeAsync(
            new SubscribeSpec(email, listIds: null /* falls back to DefaultListId */),
            ct);
        if (subscribed.IsFailure) return Result<Unit>.Failure(subscribed.Error);

        return await _email.SendTemplatedAsync(
            new TemplatedMessage(
                templateId: "42", // Listmonk template ID, parsed as int
                to: email,
                data: new() { ["FirstName"] = "Friend" }),
            ct);
    }
}
```

## Gotchas

- **No raw email.** Listmonk requires a template for transactional sends. `IEmailService.SendAsync` (raw body) returns an error directing callers to `SendTemplatedAsync`. Build the template in Listmonk first, then reference its numeric ID.
- **One recipient per send.** The transactional API targets a single subscriber; the adapter sends to the first recipient in the message's To list. Use `SendBatchAsync` for multi-recipient flows.
- **No delivery status.** Listmonk transactional emails do not expose per-message delivery status. `GetMessageStatusAsync` always reports `Sent` — wire your own tracking (e.g., bounce webhooks at the SMTP layer) if you need delivery telemetry.
- **Confirmations are owned by Listmonk.** `ConfirmSubscriptionAsync` is a no-op; subscribers confirm via the link Listmonk emails them. Use the `RequireConfirmation` flag on `SubscribeSpec` to control whether new subscribers are double-opt-in or pre-confirmed.
- Subscriber statuses map as: `enabled` → Confirmed, `disabled` → Unsubscribed, `blocklisted` → Blocked.
- The adapter logs the template ID and recipient count, never the recipient address — keeping PII out of the log pipeline. Don't add address fields to your own log lines either.
- Resilience: 3-attempt exponential backoff with circuit breaker (5 failures, 30s open).

## See also

- [API Reference](../api/Compendium.Adapters.Listmonk.html)
- [Listmonk transactional API](https://listmonk.app/docs/apis/transactional/)
- Sample app — coming via POM-182.
