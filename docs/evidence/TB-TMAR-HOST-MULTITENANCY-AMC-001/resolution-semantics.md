# resolution-semantics — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Authority chain

1. `ControlPlaneRegistry.Edition` / `DeploymentId` (process-locked config)
2. Marketplace: `MarketplaceConnectionReference` + `DeploymentStoreCommerce`
3. SingleStore: normalize `Request.Host` via `HostNormalizer` → `registry.Hosts` allowlist → `TenantStatus.Active` → connection resolve → `record.StoreCommerce`
4. Tenant header is **not** source of truth (file comment + implementation)

## Fail-closed matrix

| Condition | HTTP | Code |
|---|---|---|
| Edition Unset | 503 | `platform.edition.unconfigured` |
| Marketplace connection null | 503 | `platform.connection.unconfigured` |
| Unknown host | 404 | `platform.resolution.failed` |
| Disabled / Suspended / non-Active | 404 | `platform.resolution.failed` (same `FailClosed()`) |

No fail-open path. Connection details not written to response.

## SkipPrefixes

| Prefix | Why |
|---|---|
| `/health` | Liveness without DB/tenant open |
| `/ready` | Readiness probe without tenant open |
| `/__platform-error` | Diagnostic probe (Testing) |
| `/__platform-conflict` | Diagnostic probe (Testing) |

Matching: `Path.StartsWithSegments` — auth/business routes do not share these prefixes. Bounded and safe; do not expand without Architect decision.

## Forwarded Host

Comment documents Forwarded Host only when proxy allowlist enables it in pipeline — resolution uses `httpContext.Request.Host` after that pipeline stage.
