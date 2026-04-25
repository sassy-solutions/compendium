# Compendium.Adapters.Zitadel

> Zitadel identity provider integration: user/organization services, token validation, claims transformation, and identity provisioning.

## Install

```bash
dotnet add package Compendium.Adapters.Zitadel
```

## Configuration

The adapter exposes two distinct option types: `ZitadelOptions` (platform/admin operations) and `ZitadelEndUserOptions` (consumer-facing self-service flows). They are configured separately.

`appsettings.json`:

```json
{
  "Zitadel": {
    "Authority": "https://zitadel.example.com",
    "ServiceAccountKeyPath": "/run/secrets/zitadel-sa.json",
    "ProjectId": "123456789",
    "DefaultOrganizationId": "987654321",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "ZitadelEndUser": {
    "Authority": "https://zitadel.example.com",
    "ClientId": "enduser-app-client-id",
    "ClientSecret": "enduser-app-client-secret",
    "ProjectId": "123456789",
    "Audience": "enduser-app",
    "WebhookSecret": "whsec_...",
    "DefaultSubscriptionTier": "Free"
  }
}
```

DI registration:

```csharp
using Compendium.Adapters.Zitadel;

builder.Services.AddZitadel(options =>
{
    builder.Configuration.GetSection("Zitadel").Bind(options);
});

// Optional: register the Zitadel health check
builder.Services.AddHealthChecks().AddZitadelHealthCheck();
```

### `ZitadelOptions` (platform)

| Property | Default | Description |
|---|---|---|
| `Authority` | `""` | Base URL of the Zitadel instance (required). |
| `ServiceAccountKeyJson` | `null` | Service account key JSON content (M2M auth). |
| `ServiceAccountKeyPath` | `null` | File path to a service account JSON key. |
| `ClientId` | `null` | OAuth2 client ID. |
| `ClientSecret` | `null` | OAuth2 client secret. |
| `PersonalAccessToken` | `null` | PAT for M2M auth (skips client_credentials when set). |
| `ProjectId` | `null` | Zitadel project ID. |
| `DefaultOrganizationId` | `null` | Default org ID for operations that require one. |
| `TimeoutSeconds` | `30` | HTTP request timeout. |
| `MaxRetries` | `3` | Retry attempts for transient HTTP failures. |
| `InternalBaseUrl` | `null` | Cluster-local base URL (avoids NAT hairpin); `Host` header is set to `Authority`. |
| `SkipSslValidation` | `false` | Disable TLS validation. **Dev only.** |
| `RedirectUriTemplate` | `null` | OIDC redirect URI; supports `{organization}` placeholder. |
| `PostLogoutUriTemplate` | `null` | Post-logout redirect; supports `{organization}` placeholder. |

### `ZitadelEndUserOptions` (consumer)

Bound from configuration section `ZitadelEndUser`.

| Property | Default | Description |
|---|---|---|
| `Authority` | `""` | Base URL of the Zitadel instance. |
| `ClientId` | `null` | OIDC client ID for the end-user app. |
| `ClientSecret` | `null` | OIDC client secret for the end-user app. |
| `ProjectId` | `null` | Zitadel project ID for end-users. |
| `Audience` | `null` | Expected `aud` claim in end-user tokens. |
| `SelfRegistrationEnabled` | `true` | Allow self-registration. |
| `WebhookSecret` | `null` | Secret for Zitadel webhook signature validation. |
| `WebhookSignatureHeader` | `"X-Zitadel-Signature"` | Header name carrying the webhook signature. |
| `TimeoutSeconds` | `30` | HTTP request timeout. |
| `SkipSslValidation` | `false` | Disable TLS validation. **Dev only.** |
| `DefaultSubscriptionTier` | `"Free"` | Default subscription tier for new end-users. |
| `OrgToTenantMapping` | `{}` | Static map of Zitadel Org ID → Tenant ID. Empty falls back to dynamic lookup. |

## Usage

Resolve the identity ports from DI:

```csharp
public class OnboardingHandler
{
    private readonly IIdentityUserService _users;
    private readonly IOrganizationService _orgs;
    private readonly IOrganizationIdentityProvisioner _provisioner;

    public OnboardingHandler(
        IIdentityUserService users,
        IOrganizationService orgs,
        IOrganizationIdentityProvisioner provisioner)
    {
        _users = users;
        _orgs = orgs;
        _provisioner = provisioner;
    }

    public async Task<Result<TenantId>> ProvisionAsync(
        string orgName,
        string adminEmail,
        CancellationToken ct)
    {
        var org = await _orgs.CreateAsync(orgName, ct);
        if (org.IsFailure) return Result<TenantId>.Failure(org.Error);

        return await _provisioner.ProvisionAsync(org.Value.Id, adminEmail, ct);
    }
}
```

`ITokenValidator` validates incoming bearer tokens against Zitadel's JWKS or introspection endpoint; `ZitadelClaimsTransformation` projects Zitadel claims into the framework's `TenantContext` so downstream `[Authorize]` policies see the tenant.

## Gotchas

- **Two configurations, not one.** `ZitadelOptions` and `ZitadelEndUserOptions` are independent — mixing platform and end-user credentials will silently produce the wrong audience.
- The `{organization}` placeholder in `RedirectUriTemplate` / `PostLogoutUriTemplate` is a literal token substituted at provision time. If you use OIDC config with these templates, do the substitution before handing them to your auth library.
- `InternalBaseUrl` is the escape hatch for Kubernetes NAT hairpin issues: API calls go to the internal URL but the `Host` header is set to `Authority`, so Zitadel still routes by hostname.
- `SkipSslValidation` is wired through the HTTP client. Never set it in production — there is no secondary check.
- The HTTP client uses Polly: 3-attempt exponential backoff plus a circuit breaker (5 failures, 30s open). HTTP 429 is treated as transient.
- Token refresh is mutex-protected — concurrent calls share a single in-flight token request.

## See also

- [API Reference](../api/Compendium.Adapters.Zitadel.html)
- [Multi-tenancy strategy (ADR 0004)](../adr/0004-multi-tenancy-strategy.md)
- Sample app — coming via POM-182.
