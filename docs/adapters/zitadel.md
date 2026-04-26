# Compendium.Adapters.Zitadel

[Zitadel](https://zitadel.com/) OIDC identity adapter. Implements the identity provider port from `Compendium.Abstractions.Identity`, including org provisioning, user management, and token introspection.

## Install

```bash
dotnet add package Compendium.Adapters.Zitadel
```

You need a Zitadel instance and either a service-account JSON key or a Personal Access Token (PAT).

## Configuration

```json
{
  "Zitadel": {
    "Authority": "https://zitadel.example.com",
    "ServiceAccountKeyPath": "/etc/zitadel/sa.json",
    "ProjectId": "239820934820",
    "RedirectUriTemplate": "https://{organization}.admin.example.com/api/auth/callback/zitadel",
    "PostLogoutUriTemplate": "https://{organization}.admin.example.com"
  }
}
```

```csharp
builder.Services.Configure<ZitadelOptions>(
    builder.Configuration.GetSection("Zitadel"));
```

Options (`ZitadelOptions`):

| Option | Default | Description |
|---|---|---|
| `Authority` | _required_ | Base URL of the Zitadel instance |
| `ServiceAccountKeyJson` | `null` | Service-account key JSON inline (use for K8s secrets) |
| `ServiceAccountKeyPath` | `null` | Path to service-account JSON (alternative to inline) |
| `ClientId` / `ClientSecret` | `null` | OAuth2 client credentials flow (alternative to SA key) |
| `PersonalAccessToken` | `null` | PAT used directly as Bearer (simplest auth path) |
| `ProjectId` | `null` | Required for some management operations |
| `DefaultOrganizationId` | `null` | Default org for operations not bound to a tenant |
| `TimeoutSeconds` | `30` | HTTP timeout |
| `MaxRetries` | `3` | Retry attempts on transient failures |
| `InternalBaseUrl` | `null` | Cluster-local URL to bypass hairpin NAT |
| `SkipSslValidation` | `false` | Dev-only — never enable in production |
| `RedirectUriTemplate` | `null` | OIDC redirect URI template; must contain `{organization}` |
| `PostLogoutUriTemplate` | `null` | OIDC post-logout URI template |

The `{organization}` placeholder in the URI templates is substituted with the org name at provision time. Constants are exposed via `ZitadelOptions.OrganizationPlaceholder`.

## Usage

```csharp
public sealed class CreateTenantHandler(IIdentityProvider identity)
    : ICommandHandler<CreateTenantCommand, TenantId>
{
    public async Task<Result<TenantId>> Handle(CreateTenantCommand cmd, CancellationToken ct)
    {
        var orgResult = await identity.CreateOrganizationAsync(cmd.Name, ct);
        if (orgResult.IsFailure) return orgResult.Error;

        return new TenantId(orgResult.Value.Id);
    }
}
```

The adapter exposes endpoints for organizations, users, projects, OIDC apps, and token introspection — see [`src/Adapters/Compendium.Adapters.Zitadel/Services/`](https://github.com/sassy-solutions/compendium/tree/main/src/Adapters/Compendium.Adapters.Zitadel/Services).

## Gotchas

- **Authentication priority.** When both `PersonalAccessToken` and `ServiceAccountKey*` are set, the PAT wins. Avoid setting both in the same environment.
- **PATs vs service-account keys.** PATs are single-credential and rotate manually; SA keys can be issued and revoked through the Zitadel admin UI. Prefer SA keys for production.
- **`SkipSslValidation` is poison in prod.** If your Zitadel uses a private CA, mount it via a CA bundle in the container instead.
- **`InternalBaseUrl` for in-cluster traffic.** When the gateway routes Zitadel via a public host, internal calls hit the same hostname and may hairpin through your ingress. Setting `InternalBaseUrl` to the cluster-local service skips that loop while keeping the `Host` header correct for routing.
- **Redirect URIs and tenant placeholders.** If you forget `{organization}` in `RedirectUriTemplate`, provisioning fails — Zitadel rejects identical redirect URIs across orgs.

## See also

- [API Reference: Compendium.Adapters.Zitadel.Configuration](../api/Compendium.Adapters.Zitadel.Configuration.html)
- [Zitadel docs](https://zitadel.com/docs)
- [ADR 0004 — Multi-tenancy strategy](../adr/0004-multi-tenancy-strategy.md)
