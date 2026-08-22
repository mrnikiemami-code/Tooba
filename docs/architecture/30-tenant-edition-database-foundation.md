# Tooba — Tenant, Edition & Database Resolution Foundation

Status:

```text
P01 foundation — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T003
```

This is **not** the production control-plane product and **not** per-tenant migration orchestration.

```text
TenantId != Hostname
Host is routing input, not durable identity
Tenant != Domain
Tenant != Market
Market != Locale
Store != User
Theme != Tenant identity
Locale != Market != Currency != Tax Jurisdiction
Technical Log != Business Audit != Security Audit != Analytics
```

PostgreSQL is the canonical relational database. No SQL Server. No SQLite as architecture truth.

## What was implemented

- Explicit deployment Edition: `Marketplace` | `SingleStore` | `Unset` from `Tooba:Edition` (not request headers).
- Two commercial deployments from shared Host composition. One process is one Edition. It does not serve Marketplace and Single-Store at once.
- Configuration-backed control-plane registry seam (Host-owned). Development samples only. No admin UI, provisioning, or control-plane SQL product.
- Single-Store: trusted Host → normalize → allowlisted mapping → status → immutable `TenantContext` → `ConnectionReference` → connection string.
- Marketplace: stable `CommerceContext` with deployment identity and one marketplace `ConnectionReference`. No Host→store DB lookup.
- Immutable request `CommerceContext` via `ICurrentCommerceContext` / `ICurrentEdition` / `ICurrentTenant`. No `HttpContext` leak into Domain.
- Npgsql connection-string parse seam. No domain DbContexts, no business migrations.
- Fail-closed resolution using existing ProblemDetails.
- Safe log/span tags: Edition, DeploymentId, TenantId. TenantId is not an OpenTelemetry Resource attribute.

## Edition vs deployment

| Term | Meaning |
| --- | --- |
| Edition | `Marketplace` or `SingleStore` from deployment configuration |
| DeploymentId | Stable identity of this published runtime |
| Unset | Host can start (health/ready). Other routes fail closed until Edition is configured |

Invalid Edition fails startup.

## Marketplace vs Single-Store

Marketplace requests never resolve a store from Host and never open another store’s database. `Tenant` on `CommerceContext` is null.

Single-Store uses the control-plane host allowlist only. Unknown host does not fall through to another tenant.

## Host normalization

```text
trim
strip :port when numeric
strip trailing dot
IDNA ToASCII (punycode)
lowercase
reject empty / *
```

Matching is case-insensitive because normalization lowercases ASCII.

## Trusted proxy / forwarded Host

Threats: Host spoofing, untrusted `X-Forwarded-Host`, proxy/CDN, domain misconfiguration, cache poisoning, cross-tenant DB selection.

`UseForwardedHeaders` is enabled **only** when `Tooba:TrustedProxies` lists explicit proxy IP addresses. KnownNetworks/KnownProxies are not left unrestricted. Hostname is never used to construct credentials.

## Control-plane registry seam

In-memory/config records: TenantId, hosts, primary domain, status, ConnectionReference, optional ThemeReference and DefaultMarketReference.

Customer business data is not stored here.

## Immutable context

After success, request Items hold `CommerceContext` (edition, optional tenant, connection reference key, TraceId). Mid-request tenant switch is not provided.

DefaultMarketReference is a **reference only**. This task does not resolve Market, Locale, Currency, or Tax Jurisdiction from Host.

## Connection-reference resolution

`IDatabaseConnectionResolver` maps `ConnectionReference` (e.g. `tenant-alpha`) to `Tooba:PostgreSQL:ConnectionReferences`. Empty/missing/invalid Npgsql strings fail closed (503 `platform.connection.unconfigured`). Business modules must not parse Host or pick strings.

Connection infrastructure may be shared later; business persistence stays modular.

## Fail-closed matrix

| Case | HTTP | errorCode (payload) |
| --- | --- | --- |
| Unset edition on business/probe route | 503 | `platform.edition.unconfigured` |
| Unknown host / no mapping / disabled / suspended | 404 | `platform.resolution.failed` |
| Missing connection configuration | 503 | `platform.connection.unconfigured` |
| Duplicate host or TenantId at startup | fail start | n/a |

Unknown and disabled share the same public code so tenant existence is not leaked. `/health` and `/ready` skip resolution.

Readiness is host-foundation ready, not per-tenant database ready.

## Observability / errors

Reuse TB-P01-T002 ProblemDetails (`traceId`, optional `errorCode`). Resolution failures log Warning with TraceId and errorCode, not connection strings. Wrong-store / unknown-host is a first-class technical failure class (`platform.resolution.failed`).

## Configuration examples

See `appsettings.json` (Unset, empty registry) and `appsettings.Development.json` (Single-Store samples with local placeholder credentials). Environment variables bind via ASP.NET (`Tooba__Edition`, `Tooba__PostgreSQL__ConnectionReferences__tenant-alpha`, …).

## Cache / workers (deferred products, durable rules)

```text
store-varying cache entries must be tenant-scoped
do not use domain string alone as durable cache namespace
Marketplace uses deployment namespace plus other dimensions later
workers must receive explicit TenantId/StoreId or Marketplace deployment id
workers must not guess tenant from process ambient state
no secrets/connection strings in payloads
```

## P01 concern carry-forward

- OpenTelemetry contrib package version alignment should be revisited later.
- Development/Testing diagnostic endpoints (`/__platform-error`, `/__platform-conflict`, `/__platform-commerce`) must be removed or more tightly gated before public deployment.

## Deferred

Production control-plane HA, tenant admin UX, domain-bind workflows, secret vault, EF business schemas, per-tenant migrations, Redis, outbox, bus, jobs, Identity, SpiceDB, Theme engine, Market/Locale/Currency/Tax resolvers, commercial UI, Shopeiva study.
