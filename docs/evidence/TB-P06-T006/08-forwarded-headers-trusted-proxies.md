# 08 — Forwarded headers / trusted proxies (TB-P06-T006)

## Configuration

Key: `Tooba:TrustedProxies` (string array of proxy IP addresses)

Files: `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` — default `[]`.

## Behavior (`Program.cs`)

When `TrustedProxies` is non-empty:

1. Configure `ForwardedHeadersOptions`:
   - `ForwardedHeaders`: X-Forwarded-For | X-Forwarded-Proto | X-Forwarded-Host
   - `KnownNetworks.Clear()` and `KnownProxies.Clear()`
   - Each configured IP added to `KnownProxies`
2. `app.UseForwardedHeaders()` before CORS (after exception handler)

When empty: forwarded headers middleware **not** registered.

## Rate-limit interaction

`AuthenticationRateLimitThrottleSeam.BuildKey` uses `context.Connection.RemoteIpAddress`.

After `UseForwardedHeaders`, Kestrel resolves the client IP from `X-Forwarded-For` when the request arrives through a listed trusted proxy.

## Security invariant

KnownNetworks/KnownProxies are **not** left unrestricted. Only explicitly listed proxy IPs are trusted (aligned with `docs/architecture/30-tenant-edition-database-foundation.md`).

## Ops requirement

Behind TLS-terminating load balancer or ingress: populate `Tooba:TrustedProxies` with the proxy's direct-connect IP(s) so rate limits and HSTS reflect the real client and scheme.
