# Compendium.Adapters.Listmonk

[Listmonk](https://listmonk.app/) is a self-hosted newsletter and mailing list manager. This adapter implements the email-provider and newsletter-management ports from `Compendium.Abstractions.Email` against a Listmonk instance.

## Install

```bash
dotnet add package Compendium.Adapters.Listmonk
```

You need a Listmonk instance reachable from your service.

## Configuration

```json
{
  "Listmonk": {
    "BaseUrl": "https://listmonk.example.com",
    "Username": "api-user",
    "Password": "<long-random-secret>",
    "DefaultFromEmail": "noreply@example.com",
    "DefaultFromName": "Acme",
    "DefaultListId": 1
  }
}
```

Options (`ListmonkOptions`):

| Option | Default | Description |
|---|---|---|
| `BaseUrl` | _required_ | URL of the Listmonk instance |
| `Username` | _required_ | Basic-auth username |
| `Password` | _required_ | Basic-auth password (Listmonk does not expose API tokens — use a strong password) |
| `DefaultFromEmail` | _empty_ | Sender address for transactional emails |
| `DefaultFromName` | _empty_ | Sender display name |
| `DefaultListId` | `null` | Default list new subscribers are added to |
| `TimeoutSeconds` | `30` | HTTP timeout |
| `MaxRetries` | `3` | Retry attempts on transient failures |
| `SkipSslValidation` | `false` | Dev-only — never enable in production |

## Usage

```csharp
public sealed class SubscribeHandler(INewsletterService newsletter)
    : ICommandHandler<SubscribeCommand>
{
    public async Task<Result> Handle(SubscribeCommand cmd, CancellationToken ct)
    {
        var subResult = await newsletter.SubscribeAsync(
            email: cmd.Email,
            listId: cmd.ListId.ToString(),
            attributes: new Dictionary<string, object> { ["source"] = "checkout" },
            ct);

        return subResult.IsSuccess ? Result.Success() : subResult.Error;
    }
}
```

The adapter also exposes `IEmailService` for transactional sends, list management (`ListMailingListsAsync`, etc.), and bulk operations.

## Gotchas

- **PII in logs.** Subscriber emails are sent to Listmonk (intended), but Compendium does not log raw emails server-side after [POM-178](https://github.com/sassy-solutions/compendium/pull/3). When extending this adapter, mirror the pattern: log `subscriber_id` post-lookup, never the raw email.
- **List IDs are integers.** Listmonk uses numeric list IDs; the adapter accepts `string` for portability and parses internally. Pass numeric strings.
- **Basic auth over HTTPS only.** Listmonk credentials over plain HTTP leak the password on every request. If `BaseUrl` starts with `http://`, fail fast in production.
- **Bulk operations are paginated.** Listing more than ~100 subscribers requires pagination — use the `page`/`per_page` parameters in the underlying client.

## See also

- [API Reference: Compendium.Adapters.Listmonk.Configuration](../api/Compendium.Adapters.Listmonk.Configuration.html)
- [Listmonk docs](https://listmonk.app/docs/)
- [`Compendium.Abstractions.Email`](../api/Compendium.Abstractions.Email.html) — port contracts
