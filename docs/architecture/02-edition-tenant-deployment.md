# Tooba — Edition, Deployment & Tenant Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T003
```

Documentation only. No middleware, schemas, themes, caches, or infrastructure implementation.

## A. Terminology

| Term | Meaning |
| --- | --- |
| Edition | Commercial product mode: Marketplace or Single-Store. Shared source; different composition/policy. |
| Deployment | A published runtime: one Marketplace deployment **or** one Single-Store shared deployment. Not the same as Edition source. |
| Store | Commercial customer shop in Single-Store edition (not a User). |
| Tenant | Durable store identity in Single-Store (opaque id). Marketplace does not pretend every request is a Single-Store tenant lookup. |
| Domain / Host | Public hostname used as a **resolution key/alias**. Not TenantId. A store may have multiple domains. |
| Market | Commercial market context. Not Locale. Not Tenant. |
| Locale | Language/presentation locale. Not Market. Not Currency. |
| Theme | Visual/brand presentation for a store. Not tenant identity. |
| Database | Persistence unit. Marketplace: one marketplace DB (current model). Single-Store: one DB per store. |
| Tenant Context | Immutable per-request (or per-job) identifiers/references after successful resolution. |
| Request Context | Ambient request scope including tenant/deployment context, correlation, and later auth subject. |

Forbidden conflations:

```text
Tenant != Domain
Tenant != Market
Market != Locale
Store != User
Theme != Tenant identity
TenantId != DomainName
```

## B. Recommended Edition Architecture

**Recommendation:** one shared modular-monolith product (shared solution/modules) composed into **two commercial deployments**, not two forked codebases.

| Difference belongs in | Examples |
| --- | --- |
| Composition root | Which modules are wired (Seller/Marketplace enabled or not) |
| Feature/edition policy | Capability set on Commerce/Deployment context |
| Configuration | Connection to marketplace DB vs control-plane + per-store DBs |
| Module enablement | Seller module DISABLED in Single-Store overlay |
| UI navigation/exposure | Hide vendor-marketplace nav in Single-Store |
| Deployment configuration | Host allowlists, trusted proxies, pool settings |

Must **not** scatter arbitrary `if (isMarketplace)` through Catalog, Pricing, Order, or other business modules. Edition differences are policy/composition, not duplicated domain logic.

Do **not** interpret the requirement as one runtime serving Marketplace and Single-Store simultaneously.

```text
Tooba Marketplace deployment
Tooba Single-Store shared deployment
```

## C. Single-Store Tenant Resolution Flow

```text
HTTP Request
↓
trusted Host extraction (only from configured trusted proxy chain)
↓
host normalization (lowercase, strip default port, IDNA as required)
↓
allowlisted domain mapping lookup (control plane)
↓
tenant/store resolution
↓
tenant status validation
↓
immutable tenant context creation
↓
database connection resolution (by ConnectionReference, never by hostname)
↓
theme/configuration reference resolution
↓
market/locale/currency according to separate policies
↓
application request execution
```

Unknown hosts **fail closed** and **never** fall through to another store’s database.

## D. Domain Mapping Model (conceptual)

Control-plane owned (not tenant business data):

```text
Tenant/Store Registry
Domain Aliases
Primary Domain
Tenant Status
Database Connection Reference
Theme Reference
Edition/Capabilities
Configuration Reference
```

No SQL tables in this task.

Per-store databases **cannot** be the only discovery source for Host→DB: you cannot open the correct DB until you already know which store it is. Host mapping must live in a **control plane** reachable before tenant data-plane connections.

## E. Control Plane vs Tenant Data Plane

**Control plane (Single-Store shared deployment):** store registry, domain aliases, connection-reference metadata, tenant status, deployment config, theme **identity/reference**, feature/edition flags, migration/version metadata.

**Tenant data plane:** that store’s business/domain data (catalog, orders, parties, content, etc.).

Do **not** centralize customer business data in the control plane.

Security: control plane is high-value (it points at every tenant DB). Compromise of control plane is a fleet risk; compromise of one tenant DB is isolated if routing fail-closes.

Availability: if control plane is down, new Host resolution fails closed (no guess). Cached positive mappings, if used, must be tightly TTL’d and never serve a mapping for an unknown host.

## F. Database Resolution

Candidate contracts (names not code):

```text
ITenantResolver              Host → TenantId (control plane)
ITenantContextAccessor       immutable context for this request/job
ITenantConnectionResolver    ConnectionReference → pooled connection
```

Rules:

- Business modules must not parse Host headers.
- Business modules must not pick connection strings.
- Database selection is infrastructure/application scope.
- Tenant context is immutable after successful resolution; mid-request switch is forbidden.
- Connection pools partitioned by actual connection target.
- No tenant data in process-wide singletons.

Do not pick ORM APIs yet.

Marketplace: a **Deployment/Commerce Context** with a single marketplace database; no Host→per-store DB lookup.

## G. Tenant Context Contents

Minimal immutable context (identifiers/references, not secrets):

```text
TenantId / StoreId          identifier
Edition                     identifier
Status                      identifier
PrimaryDomain               identifier (display/canonical host, not identity)
ResolvedDomain              identifier (the host that matched)
DatabaseKey / ConnectionReference  reference (not the secret)
ThemeId                     reference
ConfigurationVersion        reference
DefaultMarket               identifier (policy default; not Market module)
DefaultLocale               identifier
Feature/Capability Set      policy snapshot or reference
Correlation metadata        trace/request ids
```

Do **not** put full config blobs, connection strings, or secrets in a globally shared mutable object. Loaded configuration is fetched via references after context exists.

## H. Security / Host Header Threat Model

Risks: Host spoofing, untrusted forwarded headers, proxy/CDN, domain takeover/misconfiguration, cache poisoning, cross-tenant DB selection, open redirects, canonical-domain confusion.

Principles:

- Trust `X-Forwarded-Host` / similar **only** from configured trusted proxies.
- Normalize and validate hosts; match **allowlisted** registered mappings only.
- Never construct DB credentials from hostname.
- No wildcard fallback to an arbitrary tenant.
- Secrets separate from public domain metadata.
- Log resolution failures without leaking secrets.

## I. Cache Isolation

Store-varying cache entries must be tenant-scoped. Example (not mandatory syntax):

```text
tenant:{TenantId}:...
```

Additional dimensions when the value varies:

```text
Tenant
Market
Locale
Currency
Theme
User/Authorization context
```

Do **not** use domain string alone as the durable cache namespace (aliases would fragment or collide).

Marketplace: deployment namespace plus Market/Locale/User where relevant.

## J. Theme Resolution

- Each Single-Store tenant may have its own theme.
- One shared publish/deployment; **no per-tenant build**.
- Theme is not unrestricted executable code in DB.
- Driven by safe tokens, brand assets, approved component variants, layout/composition config, tenant theme configuration.
- Must remain compatible with accessibility, responsive UI, SEO, Core Web Vitals.
- Theme identity is a reference on tenant context; assets resolved at runtime and cacheable by Tenant+Theme(+locale).

## K. Market / Locale / Currency Resolution

```text
Locale != Market != Currency
```

Candidate order (not SEO URL lock):

1. Tenant resolved (or Marketplace deployment context).
2. Market: tenant default and/or host-specific market mapping (control-plane or tenant config **reference**), never equal to Host.
3. Locale: user selection, then Accept-Language fallback, then tenant default locale; SEO locale routes remain a later P00 task.
4. Currency: market-allowed currencies; user/display currency is not Market and not Locale.

Tenant architecture must not block later canonical/hreflang design.

## L. Background Jobs / Messaging

Request-scoped ambient context is insufficient for workers.

Explicit immutable **TenantId/StoreId** (and Marketplace deployment id where applicable) in metadata for:

```text
background jobs
scheduled jobs
outbox/inbox messages
integration events
tenant-specific email/SMS
search indexing
analytics processing
media work
AI indexing/knowledge jobs
```

Workers must never guess tenant from process ambient state. No secrets or connection strings in payloads; workers resolve connections via the same infrastructure contracts.

## M. Observability

Safe dimensions: TenantId/StoreId, Edition, Deployment, Market, CorrelationId/TraceId.

Never: connection string, secret, sensitive personal data.

Tenant dimensions must diagnose **wrong-store routing** as a first-class failure class.

## N. Migration / Schema Versioning Implications

Database-per-store implies:

```text
schema version tracking
tenant-by-tenant migration
deployment compatibility windows
failed migration isolation
migration retries
rolling upgrades
control-plane view of tenant DB health/version
backup/restore per tenant
```

No tooling in this task. Commercially required later.

## O. Tenant Lifecycle

Candidate states (not locked enum): Provisioning, Active, Suspended, MigrationRequired, Disabled, Archived.

Capabilities: provision, activate, bind domain, change domain, rotate DB credential/reference, change theme, suspend, upgrade/migrate, backup/restore, decommission.

Unknown/disabled/suspended must not open the data-plane DB for customer traffic.

## P. Marketplace Deployment Differences

Marketplace is a **dedicated deployment** with **one marketplace database** (current model) and multi-seller behavior.

Do not run Single-Store Host→tenant→DB lookup on every Marketplace request.

Shared abstractions may expose a stable:

```text
Commerce Context / Deployment Context
```

with Edition=Marketplace, a single DatabaseKey, and Market/Locale/Currency policies — without Store-per-host mechanics in marketplace business logic.

## Q. Cross-Domain Boundary Rule

Host/tenant/database resolution is **platform/infrastructure**.

Business modules may consume resolved tenant/store identity and edition/capability context when business-relevant.

They must not: read Host, look up the tenant registry, or resolve connection strings.

Minimal consistency with the capability map: Tenant/Host/DB routing is PLATFORM, not a sales bounded context.

## R. Failure Matrix

| Condition | Fail closed? | Behavior direction | Telemetry/Audit | Customer data access |
| --- | --- | --- | --- | --- |
| Unknown Host | YES | Generic failure page; no store content | Audit miss + reason=unknown_host | NO |
| Tenant Disabled | YES | Unavailable; no data-plane | Audit | NO |
| Tenant Suspended | YES | Suspended UX; no catalog/orders | Audit | NO |
| Domain Conflict | YES | Do not pick a winner silently | Alert + audit | NO |
| DB Reference Missing | YES | Platform error | Alert | NO |
| DB Unreachable | YES | Platform error; retry infra | Alert | NO |
| Theme Missing | YES for storefront (or safe platform fallback **without** another tenant’s theme) | Never another tenant’s theme | Audit | NO cross-tenant |
| Market Invalid | YES for commerce operations that require market | Do not invent a market | Audit | NO silent other-market |
| Locale Unsupported | Fail to configured default locale **of this tenant only** | Never another tenant | Trace | Same tenant only |
| Migration Version Incompatible | YES | Tenant in MigrationRequired; no mixed-schema traffic | Control-plane health | NO until compatible |

Exact HTTP codes deferred.

## S. Decision Summary

### RECOMMENDED_FOR_ADR

1. Two deployment modes from shared code/composition, not forked products and not one mixed runtime.
2. Single-Store: one shared publish; one database per tenant/store.
3. Control-plane tenant registry for Host→tenant→connection **reference**.
4. Domain is alias/resolution key, not durable TenantId.
5. Fail-closed domain resolution; never another store’s DB.
6. Infrastructure-owned DB resolution; business modules do not parse Host or pick connections.
7. Tenant-scoped caches; domain string is not the durable namespace.
8. Explicit tenant id on async work; no ambient guess.
9. Runtime theme-per-store without per-tenant builds; no arbitrary DB-exec code.
10. Per-tenant migration/version orchestration is a required future operational architecture.

### NEEDS_LATER_P00_DETAIL

- Trusted-proxy and CDN header contract.
- Control-plane storage technology and HA.
- Connection-reference secret store.
- Exact tenant status enum and provisioning workflow.
- Host-specific market mapping vs tenant-default market.
- Theme token/variant catalog.
- Cache key grammar and Redis introduction.
- Compatibility windows for rolling schema.

### DEFERRED

- Final ADR document.
- SQL/control-plane schemas.
- Middleware/code.
- SEO URL/hreflang architecture.
- ORM/framework APIs.
- Exact HTTP status map.
- Identity/SpiceDB.
- Shopeiva integration.
