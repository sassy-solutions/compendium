# Compendium.Adapters.AspNetCore

> ASP.NET Core integration: security headers, CORS, tenant validation middleware, and health checks.

## Install

```bash
dotnet add package Compendium.Adapters.AspNetCore
```

## Configuration

This adapter is configured primarily via DI extension methods rather than `appsettings.json`. There are no required configuration sections; defaults are tuned for either an API or a web app.

### DI registration

```csharp
using Compendium.Adapters.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Security headers (HSTS, CSP, X-Frame-Options, ...)
builder.Services.AddCompendiumSecurityHeaders(options =>
{
    options.HstsMaxAgeSeconds = 31_536_000;
    options.HstsIncludeSubDomains = true;
    options.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
});

// CORS — strict allowlist
builder.Services.AddCompendiumStrictCors(allowedOrigins: new[]
{
    "https://app.example.com",
});

// Tenant validation middleware (in-memory store for dev)
builder.Services.AddTenantValidationWithInMemoryStore(
    new TenantInfo("tenant-a", "Tenant A"),
    new TenantInfo("tenant-b", "Tenant B"));

// Health checks for PostgreSQL and Redis (pass null to skip a check)
builder.Services.AddCompendiumHealthChecks(
    postgresConnectionString: builder.Configuration.GetConnectionString("EventStore"),
    redisConnectionMultiplexer: null);

var app = builder.Build();

app.UseCompendiumHsts();
app.UseCompendiumSecurityHeaders();
app.UseCompendiumCors();

app.UseAuthentication();
app.UseTenantValidation();
app.UseAuthorization();

app.MapCompendiumHealthChecks();
```

### `SecurityHeadersOptions`

| Property | Default | Description |
|---|---|---|
| `EnableHsts` | `true` | Enable HTTP Strict Transport Security. |
| `HstsMaxAgeSeconds` | `31_536_000` | HSTS `max-age` (1 year). |
| `HstsIncludeSubDomains` | `true` | Include subdomains in HSTS. |
| `HstsPreload` | `false` | Opt in to the HSTS preload list. |
| `EnableNoSniff` | `true` | `X-Content-Type-Options: nosniff`. |
| `EnableFrameOptions` | `true` | Emit `X-Frame-Options`. |
| `FrameOptionsValue` | `"DENY"` | `DENY` or `SAMEORIGIN`. |
| `EnableContentSecurityPolicy` | `true` | Emit `Content-Security-Policy`. |
| `ContentSecurityPolicy` | `"default-src 'none'; frame-ancestors 'none'"` | CSP value (API-strict default). |
| `EnablePermittedCrossDomainPolicies` | `true` | Emit `X-Permitted-Cross-Domain-Policies`. |
| `EnableReferrerPolicy` | `true` | Emit `Referrer-Policy`. |
| `ReferrerPolicyValue` | `"strict-origin-when-cross-origin"` | Referrer policy value. |
| `EnablePermissionsPolicy` | `true` | Emit `Permissions-Policy`. |
| `RemoveServerHeader` | `true` | Strip the `Server` response header. |
| `RemoveXPoweredByHeader` | `true` | Strip the `X-Powered-By` response header. |

The `TenantValidationMiddlewareOptions` and `TenantConsistencyOptions` types expose the knobs for the tenant pipeline; pass configuration actions to `AddTenantValidation` if you need non-default behavior.

## Usage

The middleware pipeline order matters: security headers and CORS go before authentication, tenant validation goes between authentication and authorization, and health checks are mapped on the endpoint router.

```csharp
app.UseCompendiumHsts();
app.UseCompendiumSecurityHeaders();
app.UseCompendiumCors();

app.UseAuthentication();
app.UseTenantValidation(); // populates TenantContext from claims/headers
app.UseAuthorization();

app.MapCompendiumHealthChecks(); // /health (liveness) + /health/ready (readiness)
```

`TenantContext` is the primary consumer-facing surface for the current tenant once validation has run; resolve it from DI in your handlers.

## Gotchas

- `UseTenantValidation()` must run **after** `UseAuthentication()` and **before** `UseAuthorization()`, otherwise the tenant claim is not available when policies execute.
- `AddCompendiumSecurityHeaders` defaults to API-strict CSP (`default-src 'none'`). If you serve HTML, override `ContentSecurityPolicy` or your pages will fail to load assets.
- `MapCompendiumHealthChecks` exposes `/health` (liveness, no checks) and `/health/ready` (readiness, filtered by the `ready` tag). Don't put either behind authentication if your orchestrator probes them anonymously.
- Health check registration is opt-in per dependency: pass `null` for `postgresConnectionString` or `redisConnectionMultiplexer` to skip it.

## See also

- [API Reference](../api/Compendium.Adapters.AspNetCore.html)
- Sample app — coming via POM-182.
