# Tooba — Caching & Infrastructure Abstractions Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T020
```

Documentation only. No Redis, cache packages, distributed locks, message brokers, CDN providers, schedulers, secret stores, feature-flag products, or cache code. No vendor lock of Redis as a current requirement.

Modular monolith. No cross-module DB joins. SpiceDB remains the authorization decision system. Search, Media, Analytics, and AI cache notes in existing P00 documents remain aligned; this document is the platform cache/infrastructure seam, not a competing policy.

```text
Cache != Source of Truth
Cache != Authorization Truth
Cache != Payment Truth
Cache != Inventory Reservation Authority
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
```

## A. Core Principle

Cache is an **optimization layer**. System correctness must not depend on cache containing the only copy of business truth.

Authoritative writes and decisions remain in owning modules (Catalog, Pricing, Inventory, Order, Payment, Authorization, Tenant Platform, Content, etc.). A miss, eviction, or backend outage must be recoverable by reading the source (or a declared fail-closed policy for security-sensitive lookups — never by inventing commerce truth from empty cache).

Do not use cache as a shared integration bus between modules.

## B. Initial vs Future Infrastructure

Confirmed direction:

```text
Initial:
no Redis required
```

Future:

```text
Redis or another distributed cache may be added
```

Hard architectural requirement:

```text
Business/domain modules must not depend directly on Redis APIs.
```

Use internal cache abstractions/ports (section C). No vendor choice now. In-process memory (and request-local) caches may exist initially; a distributed backend is an infrastructure swap, not a domain redesign.

Aligns with Search (`14-search-indexing.md`: Redis optional later), Pricing, Inventory, Observability (`18-observability-logging-audit.md`: no Redis required initially).

## C. Cache Abstraction

Conceptual contracts (names not code; do not implement):

```text
ICache
ICacheReader
ICacheWriter
ICacheInvalidator
```

or equivalent split (e.g. keyed get/set, conditional set, invalidate-by-key / invalidate-by-tag, category-scoped operations).

Avoid leaking into domain/application contracts:

```text
Redis key types
Redis transactions
vendor-specific TTL semantics
```

Application code speaks: key (opaque structured identity), value (versioned DTO), TTL/soft-TTL policy, category, invalidation token/version. Infrastructure adapters map those to memory, future Redis, or no-op.

Do not implement interfaces in this task.

## D. Cache Categories

Do not treat all caches as equivalent.

| Category | Typical location | Consistency / security notes |
| --- | --- | --- |
| Request-local cache | one request/job | Safe for memoizing a check/quote within one unit of work; never a shared store |
| In-process memory cache | process | Fast; not shared across instances; tenant-key still mandatory; lost on restart |
| Distributed cache | future Redis/etc. | Shared across instances; versioned payloads; not SoT |
| CDN/edge cache | edge | Public, URL/version oriented; no private user payloads |
| Browser cache | client | Cache-Control / immutable URLs; never private HTML by accident |
| Search/read-model cache | app or search tier | Query-shaped; short TTL; not catalog/pricing SoT |
| Derived asset cache | media/CDN | Immutable variants; separate from application data cache |

Each has different invalidation, TTL, and leakage rules. A CDN hit is not an Inventory reservation; an in-process hit is not a Permission grant.

## E. Cache Ownership

Caching **policy** belongs near the application / read-model / infrastructure boundary.

Domain entities must not call cache services directly. Modules may declare:

```text
what may be cached
what dimensions affect correctness
what invalidates it
```

Infrastructure decides backend (memory now, distributed later). Read-model owners (e.g. storefront composition BFF, search query cache, analytics dashboard aggregate cache) own **keys and invalidation intent**; they do not own Catalog/Pricing/Inventory business truth.

## F. Key Dimensions

Cache keys must include all dimensions that **materially change** the cached payload.

Potential dimensions (not a mandatory universal tuple):

```text
TenantId
Deployment/Edition
Locale
Market
Currency
SalesChannel
Theme
Buyer/Organization/Contract
Seller
User/Identity where private
Authorization scope/version
EntityId
Query/filter/sort
Version
```

Do not mechanically include every dimension on every key. Require explicit **cache-context design per use case**. Missing a required dimension is a correctness/security defect; extra unused dimensions waste cardinality.

Hostname is **not** a cache identity (see G).

## G. Tenant Isolation

**Single-Store:** no cache entry may be shared across tenants unless it is explicitly platform-global and proven safe (e.g. non-tenant static platform metadata). Tenant-varying entries always carry resolved TenantId (or equivalent store identity).

Hard rule:

```text
missing TenantId in a tenant-varying cache key = security/correctness defect
```

**Marketplace:** use deployment / seller / resource scope as appropriate — do not invent fake per-store TenantId semantics for marketplace catalog that is not tenant-partitioned that way.

Do **not** use hostname as canonical tenant identity. Host is a resolution alias (`02-edition-tenant-deployment.md`). Positive host→tenant mappings, if cached, are tightly TTL’d, fail-closed on unknown host, and never used as the key for storefront product/content payloads.

## H. Locale / Market / Currency

Preserve:

```text
Locale != Market != Currency
```

Examples:

- localized category title varies by **Locale**;
- price varies by **Market** / **Currency**;
- commercial availability may vary by **Market**;
- formatting may vary by **Locale**.

Do not cache a “universal” product page fragment if those contexts change output. Aligns with Search (`14`: locale analyzer, market eligibility, currency projected price — independent) and Pricing (`08`: quote-relevant dimensions).

## I. Theme-Aware Caching

Single-Store tenants may have runtime themes.

Rendered output / composed fragments may vary by:

```text
Tenant
ThemeId
ThemeVersion
Locale
Page Composition Version
```

Do not allow one tenant’s visual/branding output to leak into another tenant. Theme changes must invalidate or version relevant caches (storefront HTML/fragments, possibly CSS/asset URLs). Theme is not a substitute for TenantId.

## J. Public vs Private Cache

Separate:

```text
public anonymous content
authenticated user data
authorization-scoped data
customer order/account data
seller/admin data
```

Hard rule: private/user-scoped data must **never** enter a shared public cache key (in-process public bucket, distributed public namespace, CDN, or shared browser cache for authenticated HTML/API).

Do not cache authenticated HTML/API responses on a public CDN or anonymous key. Aligns with Media public vs private delivery (`15`) and AI private-response isolation (`17`).

## K. Authorization-Aware Caching

Authorization decisions may be cached only **conservatively**.

Consider:

```text
subject
resource
permission
tenant
relationship/version token
TTL
revocation risk
```

Security-sensitive operations may **bypass** authorization cache and call SpiceDB (or `IAuthorizationService`) directly. SpiceDB remains authority (`05-spicedb-authorization.md`).

Do **not** create a broad:

```text
user:{id}:permissions
```

cache with unbounded or long-stale semantics.

Check-result caching, if any, is short-TTL, scoped, versioned (authorization/relationship token), and treated as `NOT_CACHE_AUTHORITY`. Revocation/permission-change invalidation is required conceptually; fail closed on unknown/stale security-critical paths.

## L. Catalog Caching

Catalog **descriptive** reads (titles, attributes, publication projection, category trees) are relatively cache-friendly.

Potential invalidators (names not locked):

```text
Product changed
Variant changed
Category changed
Brand changed
translation changed
publication status changed
```

Catalog cache must **not** absorb Pricing/Inventory truth into one stale object unless using an explicit composite read-model with its own versioning and still revalidating price/availability at their authorities for critical paths.

Catalog remains owner of descriptive product identity (`03`, `07`).

## M. Pricing Caching

Pricing is highly contextual (`08-pricing-market-currency.md`).

Cache keys may require:

```text
Offer
Market
Currency
SalesChannel
Buyer/Organization/Contract
Quantity bucket
Promotion context
PricingVersion
```

Hard rule: do **not** use:

```text
product:{id}:price
```

as a universal key.

Display quotes may be cached briefly. **Critical checkout price** is still revalidated/quoted by the Pricing authority. Cache is never Payment truth and never a substitute for a checkout quote.

## N. Inventory Caching

Inventory/availability changes quickly (`09-inventory-availability-reservation.md`).

Safe cache uses may include:

```text
display availability projection
short-lived ATS read
```

Must **not** be decided from stale cache alone:

```text
reservation decision
oversell decision
```

No cached reservation acceptance. Inventory module remains reservation authority. Aligns with existing: short TTL / versioned availability cache optional; never cache across tenants/sellers.

## O. Search Caching

Search result cache dimensions may include:

```text
Tenant
Locale
Market
Currency/display context
query
filters
sort
page/cursor
SalesChannel
```

This **extends** `14-search-indexing.md` AF (tenant+locale+market+currency+query+filters, short TTL, no cross-tenant, Redis optional later); sort/page/SalesChannel are additional dimensions when they change the result set.

Personalization later may reduce cacheability. Search cache does not become source truth. Public vs admin search must not share keys (`14` AG).

## P. Content / Page Composition Caching

Published content/composition is highly cacheable.

Need invalidation/versioning for:

```text
publish/unpublish
revision change
section config change
schedule activation
locale variant change
theme change
```

Preview/draft content must not contaminate public cache. Preview keys are private/staff-scoped or uncached. Scheduled activation may require version/TTL plus event invalidation so a page does not stay “unpublished” in cache after go-live.

## Q. Media / CDN Caching

Derived media variants should prefer **immutable/versioned URLs** (`15-media-image-pipeline.md` W). Use long-lived browser/CDN cache where possible.

Asset replacement should produce:

```text
new version / URL identity
```

rather than relying only on purge.

Media cache strategy remains **separate** from application data cache. Transform versioning avoids reprocessing the same derivation. Private/draft media must not use public CDN keys.

## R. SEO / Rendered Page Caching

SEO-critical rendered pages may be cached when deterministic for context (`13-seo-architecture.md`).

Need dimensions such as:

```text
Tenant
Locale
Market where route differs
Theme
Page/Content version
```

Do not serve wrong canonical / hreflang / structured data because of insufficient cache key dimensions. Preview/staging remains fail-closed public (noindex); do not cache staging HTML onto production keys.

## S. User Dashboard Caching

Customer / Seller / Admin dashboards may cache aggregates/read models.

Must preserve:

```text
user/subject scope
tenant
authorization scope
date/filter context
freshness
```

Private dashboard cache must never be shared across users or tenants. Aligns with Analytics dashboard cache (`16` AU: Tenant, Seller, date range, Market, reporting currency, SalesChannel, filters, authz scope) and AI private isolation.

Analytics aggregates are not Order SoT; caching them does not change that.

## T. TTL vs Versioning

Do not use TTL as the **only** invalidation strategy.

Combine where appropriate:

```text
TTL
event-driven invalidation
versioned keys
immutable versioned resources
explicit purge
```

TTL is a **safety net**, not always the primary correctness mechanism. Hot public catalog/content should prefer version/event + TTL; Media prefers immutable URLs; authorization prefers short TTL + version token + optional bypass.

## U. Event-Driven Invalidation

Candidate events (names **not** finalized):

```text
ProductChanged
PriceChanged
InventoryChanged
ContentPublished
ThemeChanged
PermissionChanged
SellerChanged
```

may trigger invalidation and/or projection refresh.

Invalidation should be **idempotent**. Lost events are expected (see AB); TTL and versioning remain safety nets. Do not require a broker initially (see AL).

## V. Versioned Keys

Preserve use of versions such as:

```text
ProductVersion
PricingVersion
ThemeVersion
ContentRevision
SearchIndexVersion
AuthorizationVersion token
```

where useful.

Versioned keys can reduce difficult purge fan-out (readers naturally miss old keys). Do **not** create a single global monotonically increasing version for every change by default — versions are **per concern / per aggregate**, not a fleet-wide counter.

## W. Cache Stampede Protection

Popular keys may expire simultaneously.

Preserve strategies such as:

```text
single-flight/request coalescing
soft TTL
stale-while-revalidate
jitter
lock only where necessary
```

Do **not** introduce distributed locks as the default solution. Request-local + in-process single-flight is the initial stampede control; distributed lock is a later, optional, hot-key measure — not a domain requirement.

## X. Stale-While-Revalidate

For safe public/read-only content, SWR may improve UX/performance.

Not appropriate for all data.

Good candidates:

```text
content
catalog description
public landing
non-critical aggregates
```

Bad candidates for unbounded stale use:

```text
authorization
payment status
inventory reservation
checkout price
```

SWR must still respect tenant, locale/market/currency, theme, and public/private rules. Serving slightly stale public copy is UX; serving stale checkout price is product failure.

## Y. Negative Caching

May be useful for:

```text
missing slug
missing product
unsupported locale route
```

Must be **short/bounded** where objects may soon be created (new product publish, new locale route).

Do **not** negative-cache authorization denial or payment errors casually — that can lock out a user after a fix or hide a declined-then-retried payment path. Authorization denial is not a “404 slug”.

## Z. Cache Failure Behavior

Hard principle:

```text
Cache backend failure should usually degrade to source reads, not break commerce.
```

Exceptions may exist later for deliberate rate/protection architecture (e.g. abuse shields) — not as an excuse to fail checkout because Redis is down.

No business **write** should be lost because cache is unavailable. Writes go to module persistence / outbox; cache is best-effort side effect.

## AA. Cache Penetration / Abuse

Protect source systems from repeated requests for nonexistent or expensive keys.

Possible techniques:

```text
negative caching
request coalescing
rate limiting
bounded query complexity
```

Do **not** use probabilistic filters (Bloom, etc.) unless justified later. Search already implies bounded query complexity; storefront must not turn cache-miss storms into unbounded Catalog/Pricing fan-out.

## AB. Cache Invalidation Reliability

Invalidation events may be lost or delayed.

Need:

```text
TTL safety net
versioning
reconciliation
rebuild
idempotent invalidation
```

The system must **converge**: after a source change, cached views eventually match source (or expire). Rebuild/recompute of read models remains owned by the projection owner (Search index rebuild, analytics aggregate rebuild, content republish) — cache invalidation is not a substitute for those pipelines.

## AC. Warmup

Avoid requiring full cache warmup before the application can function.

Optional warmup may improve:

```text
top categories
homepage
popular products
tenant config
```

Cold cache must remain **correct** (source reads). Warmup is UX/latency, not a readiness gate for commerce. Shared hosting may have no dedicated warmer.

## AD. Tenant Configuration Cache

Tenant registry / config / theme references may be cached.

Security-sensitive:

```text
tenant status
canonical domain
connection reference metadata
```

must respect fail-closed rules from tenant architecture (`02`): unknown host / control-plane down does not guess tenant; cached positive mappings tightly TTL’d; never serve mapping for unknown host.

Do **not** cache raw DB credentials in unsafe general-purpose caches. Cache ConnectionReference / DatabaseKey identifiers, not secrets.

## AE. Connection / Secret Abstractions

Infrastructure abstractions should preserve:

```text
ITenantConnectionResolver
secret/config reference
provider-neutral secret access
```

Business modules must not own connection-string selection (`02`). No implementation. Secret material is resolved at the infrastructure boundary; cache of “which reference this tenant uses” ≠ cache of the secret value.

## AF. Clock / Time Abstraction

Consider an internal time provider for deterministic business/application testing.

Do not scatter direct wall-clock calls through domain logic if time affects:

```text
price validity
promotion validity
reservation expiry
content schedule
token/session expiry
```

Exact implementation deferred. Cache TTL/SWR semantics should consume the same clock abstraction later so tests can advance time without sleeping.

## AG. ID Generation Abstraction

If UUID v7 or another ID scheme is later locked by ADR, generation should be centralized behind a stable primitive/service where appropriate.

Do not let modules invent inconsistent ID formats. Current scheme remains governed by existing ADR/SoT status; this document does **not** silently alter it.

Cache keys use existing entity identifiers; they do not define a new ID scheme.

## AH. External HTTP Client Abstraction

External providers such as:

```text
Payment
FX
SMS/Email
AI
CDN
Storage
Search
```

need controlled HTTP/provider clients with:

```text
timeouts
retry policy
circuit behavior
telemetry
idempotency awareness
```

Vendor clients stay behind **adapters**. Do not build a single god HTTP wrapper. Cache of HTTP responses (if any) is adapter-local and must not become a hidden SoT for Payment/FX.

## AI. Retry Policy

Retry only **transient** failures.

Hard rule:

```text
Retry != universal error handling
```

Do not blindly retry:

```text
validation errors
authorization denial
payment declined
business conflicts
non-idempotent operations
```

Retry policy belongs near the infrastructure operation with idempotency awareness. Cache backend timeouts may retry with jitter; cache **writes** after a successful domain commit remain best-effort.

## AJ. Timeout Policy

Every external call must have a **bounded** timeout policy.

Avoid default infinite/uncontrolled waits. Timeouts should be operation-specific (cache get vs payment capture vs search query). Exact numbers later.

A cache get timeout should typically fall through to source read (Z), not hang the storefront.

## AK. Circuit Breaker / Resilience

Preserve resilience patterns where useful:

```text
timeout
retry
circuit breaker
bulkhead
fallback
```

Do not require all patterns everywhere.

Optional dependencies (AI, analytics ingestion, non-critical cache) may degrade gracefully. Critical dependencies (payment capture, inventory reservation, identity) may fail **readiness** or the specific operation — they do not fail because cache is down.

## AL. Message Bus Abstraction

Do not require a broker initially.

Architecture may support:

```text
in-process integration events
database outbox
future broker
```

Do not create a fake distributed bus abstraction that hides critical delivery semantics (at-least-once, outbox, idempotent consumers).

Clear boundary: domain/integration **events** are module contracts; **transport** is infrastructure (in-process now, broker later). Cache invalidation is a consumer of those events, not the bus itself.

Aligns with Analytics event bus (`16` AH) and Observability.

## AM. Background Job Abstraction

Background processing may include:

```text
scheduled content
search indexing
media processing
analytics aggregation
reconciliation
cache refresh
AI index rebuild
```

Need:

```text
tenant context
idempotency
retry
observability
```

No scheduler/provider selected. Jobs carry explicit tenant/scope (`18` job context). Cache refresh jobs must not be required for correctness (AC). Shared hosting may run in-process or delayed jobs only.

## AN. File/Object Storage Abstraction

Already referenced by Media (`15`). Infrastructure supports provider-neutral object storage without exposing vendor types to business modules.

No duplicate competing abstraction. Application data cache is not object storage; derived media lives in object storage + CDN, not in `ICache` as blobs by default.

## AO. Email / SMS / Notification Providers

Future notifications should use adapters. Identity OTP delivery may consume notification/provider contracts but should **not** depend on one SMS vendor.

Do not design the Notification domain fully here. OTP/secrets must not enter general-purpose cache (AV).

## AP. Feature Configuration

Preserve distinction:

```text
business configuration
tenant configuration
feature flag
runtime operational configuration
secret
```

Do not store secrets in generic config. Do not scatter direct environment-variable reads through domain code.

Cache of configuration snapshots uses versions (ConfigurationVersion / ThemeVersion) and tenant keys; it is not a secret store.

## AQ. Feature Flags

Future feature flags may help safe rollout.

Do not use flags to permanently encode edition architecture or bypass domain invariants.

Flags are rollout/operational mechanisms, not a substitute for composition/policy architecture (`02`, AR). Flags must not disable tenant isolation or public/private cache rules.

## AR. Edition Composition

Marketplace vs Single-Store differences should remain in:

```text
composition
policy
configuration
deployment
```

not cache/infrastructure `if` statements spread across modules.

Infrastructure abstractions (cache ports, connection resolver, jobs, HTTP adapters) should work for **both** editions. Key dimensions differ by use case (TenantId vs deployment/seller), not by a Redis vs memory branch in Catalog.

## AS. Shared Hosting Constraint

Initial deployment may be on public/shared hosting.

Architecture must tolerate absence of:

```text
Redis
Kafka/RabbitMQ
Kubernetes
dedicated worker cluster
distributed image service
```

without compromising future migration seams.

Do not prematurely require heavyweight infrastructure. In-process cache, in-process events, DB outbox, local/object storage adapters, and request coalescing are sufficient conceptual starting points.

## AT. Dedicated Hosting Evolution

Future dedicated deployment may add:

```text
Redis
message broker
search cluster
object storage/CDN
workers
APM collector
```

Business modules should **not** require redesign for this evolution. Swap adapters: `ICache` → distributed implementation; outbox → broker consumer; media → CDN; workers → dedicated processors. Domain contracts stay.

## AU. Cache Observability

Need metrics (conceptual; integrate with OpenTelemetry in `18-observability-logging-audit.md` Z):

```text
hit rate
miss rate
latency
backend error rate
invalidation count
stampede/coalescing
key category
stale serve count
```

Avoid high-cardinality **raw key** labels. Use category / outcome / tenant-aggregated dimensions, not UserId in metric labels.

Do not log full sensitive cache keys if they contain private data. Technical cache telemetry ≠ product analytics (`16`) ≠ audit (`18`).

## AV. Security

Do not put into general cache:

```text
passwords
OTP
raw tokens
provider secrets
PAN/CVV
sensitive AI prompts
```

Session/auth cache design must be explicitly security-reviewed later. Cache serialization must not become a hidden PII dump.

Private dashboard and user-scoped keys remain isolated (J, S). Cross-tenant key collision is a security incident (BB).

## AW. Serialization / Compatibility

Distributed caches require version-compatible payloads.

Preserve:

```text
schema/version field
backward-compatible reader where needed
safe invalidation on deployment
```

Do not cache full ORM/domain entities as opaque serialized objects by default. Prefer stable **read-model / cache DTOs**.

In-process cache should follow the same DTO rule so a later Redis adapter does not serialize graphs accidentally.

## AX. Deployment / Rolling Upgrade

Cache key/version strategy must tolerate mixed application versions during rolling deployment in the future.

Do not assume the entire fleet switches atomically. Payload schema version (AW) plus optional key namespace/app-version suffix where needed. Exact deployment model later.

Incompatible readers should miss/invalidate, not deserialize into wrong meaning.

## AY. UI / UX Performance

Caching exists partly to protect user experience.

Future performance goals should support:

```text
fast category/product pages
fast search
fast admin dashboards
responsive seller workflows
low-latency navigation
stable loading states
```

Do **not** use stale caches to hide fundamentally bad query/read-model design. Cross-module joins remain forbidden; cache is not a join substitute. Professional storefront/Admin/Seller/Customer UX still requires correct Locale/Market/Currency/Theme and no cross-tenant leak.

```text
Backend/module boundary != UI boundary
```

## AZ. Loading / Error UX

When cache or source is slow or unavailable, UI needs intentional:

```text
skeleton/loading
partial degradation
retry
freshness indication where relevant
error state
```

Do not show incorrect **private** or stale **critical** data merely to avoid an error state. Public landing may SWR; checkout price, payment status, reservation, and authorization must not.

Dashboards should communicate freshness when aggregates are cached (`16` Z/AS). RTL/LTR, accessibility, and mobile remain UI implementation concerns later — architecture must not force a single spinner that masks wrong data.

## BA. Data Ownership Matrix

Marks: `OWNER` | `CACHEABLE` | `CONDITIONAL` | `NOT_CACHE_AUTHORITY` | `NOT_OWNER`

| Fact | Caching (platform) | Catalog | Pricing | Inventory | Search | Content | Page Composition | Media/CDN | Authorization | Tenant Platform | Analytics |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Business truth | NOT_OWNER / NOT_CACHE_AUTHORITY | OWNER (descriptive product) | OWNER (quotes/book) | OWNER (stock/reservation) | NOT_OWNER | OWNER (editorial) | OWNER (composition) | OWNER (assets) | OWNER (SpiceDB) | OWNER (tenant/config) | NOT_OWNER |
| Cached product view | CACHEABLE (DTO/read-model) | OWNER of source | NOT_OWNER | NOT_OWNER | CONSUMER of projection | NOT_OWNER | CONDITIONAL fragment | NOT_OWNER | NOT_OWNER | NOT_OWNER | OBSERVATION only |
| Price quote | CONDITIONAL display cache | NOT_OWNER | OWNER | NOT_OWNER | projected field only | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Availability projection | CACHEABLE short-TTL | NOT_OWNER | NOT_OWNER | OWNER | projected field only | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Search result | CACHEABLE query cache | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER of index/query | NOT_OWNER | NOT_OWNER | NOT_OWNER | constrains visibility | NOT_OWNER | observes SearchPerformed |
| Published content | CACHEABLE | NOT_OWNER | NOT_OWNER | NOT_OWNER | may index | OWNER | consumes | NOT_OWNER | NOT_OWNER | NOT_OWNER | observes views |
| Rendered page | CACHEABLE public HTML/fragment | NOT_OWNER | CONDITIONAL if price in page | CONDITIONAL if ATS in page | NOT_OWNER | SOURCE | OWNER of composition | assets | NOT_OWNER | theme/tenant | NOT_OWNER |
| Media variant | CACHEABLE via immutable URL | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | may reference | may reference | OWNER | delivery class | NOT_OWNER | NOT_OWNER |
| Permission result | CONDITIONAL conservative | NOT_OWNER | NOT_OWNER | NOT_OWNER | visibility flags | NOT_OWNER | NOT_OWNER | private vs public | OWNER / NOT_CACHE_AUTHORITY | tenant scope | dashboard authz |
| Tenant config | CONDITIONAL fail-closed | NOT_OWNER | defaults only | NOT_OWNER | tenant tag | NOT_OWNER | theme | host/CDN | tenant in checks | OWNER | tenant on events |
| Dashboard aggregate | CACHEABLE private | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | scope | tenant | OWNER of analytics aggregates |

Caching platform is **never** `OWNER` of business truth. `CACHEABLE` means a DTO/projection **may** be stored; `NOT_CACHE_AUTHORITY` means a hit is never a decision. Inventory reservation and checkout price remain `NOT_CACHE_AUTHORITY` even if display projections are `CACHEABLE`.

## BB. Failure Matrix

| Case | Fallback? | Fail closed? | Source read? | Invalidate? | Rebuild? | Alert? | Customer-visible behavior? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Memory cache miss | Yes — load source | No (commerce) | Yes | N/A | No | No | Normal latency; skeletons if slow |
| Distributed cache down | Yes — memory and/or source | No for storefront reads | Yes | N/A | No | Yes (infra) | Slightly slower; commerce continues |
| Stale price cache | Revalidate at Pricing for checkout | Checkout must not accept stale quote | Yes for critical path | Yes on PriceChanged / version | No | Optional if systemic | Display may lag; checkout uses fresh quote |
| Stale inventory cache | Display projection only | Reservation fail-closed at Inventory | Yes at reserve | Yes on InventoryChanged | No | If oversell risk pattern | ATS badge may lag; cannot reserve from cache |
| Permission cache stale | Bypass cache / SpiceDB | Yes on security-sensitive | Yes (SpiceDB) | Yes on PermissionChanged / version | No | Yes | Deny or re-check; never keep revoked grant |
| Cross-tenant key collision | None — treat as incident | Yes | Do not serve | Purge keys; fix keying | Audit | Yes | Error/empty; never other tenant’s data |
| Invalidation lost | TTL + version miss | No for public catalog | Eventually | Retry idempotent invalidate | Projection rebuild if needed | Yes if lag SLO | Temporary staleness on safe content |
| Stampede | Single-flight / jitter / SWR | No | Coalesced source | N/A | No | If origin overload | One slow load, then shared result |
| Serialization version mismatch | Treat as miss | No | Yes | Drop incompatible payload | No | Medium | Transparent miss |
| Theme cache stale | Version/ThemeChanged | No for branding (correctness of brand) | Yes | Yes | No | Low | Wrong theme until invalidate — must not cross tenant |
| Negative cache stale | Short TTL | No for 404s | Yes after expiry | Expire/publish event | No | Low | Brief false 404; must not persist |
| Cache backend timeout | Source read | No for commerce reads | Yes | N/A | No | Yes | Loading then content; no false private data |

Permission and cross-tenant cases **fail closed**. Price/inventory **critical decisions** ignore stale cache. Cache timeout **does not** fail checkout.

## BC. Testing Strategy — Architecture Level

Future implementation must test:

```text
tenant isolation
locale/market/currency key dimensions
theme invalidation
public/private separation
authorization revocation
stale price
stale inventory
cache backend outage
stampede protection
event invalidation
versioned keys
rolling version compatibility
negative cache expiry
no-secret serialization
```

No tests now.

## BD. Decision Summary

### RECOMMENDED_FOR_ADR

1. Cache is optimization, never business truth.
2. Initial system does not require Redis.
3. Redis/distributed cache remains addable behind internal abstraction.
4. Cache policy is use-case/read-model aware, not domain-entity-driven.
5. Tenant isolation is mandatory in all tenant-varying cache keys.
6. Locale/Market/Currency/Theme/User dimensions are explicit where relevant.
7. Public and private caches are strictly separated.
8. Authorization caching is conservative and revocation-aware.
9. Pricing/Inventory critical decisions always revalidate at authority.
10. TTL alone is insufficient; versioning/event invalidation are supported.
11. Stampede protection is first-class for hot keys.
12. Cache failure normally degrades to source reads.
13. Immutable/versioned Media/CDN URLs are preferred.
14. Cached payloads use stable/versioned read DTOs, not raw domain entities.
15. Shared-hosting deployment works without heavyweight infrastructure.
16. Dedicated-hosting evolution adds infrastructure without domain redesign.
17. Retry/timeout/resilience are infrastructure concerns with idempotency awareness.
18. Feature flags do not replace edition/composition architecture.
19. Cache observability integrates with OpenTelemetry.
20. Performance optimization must preserve correctness, privacy and professional UI/UX.

### NEEDS_LATER_P00_DETAIL

- Exact cache contract names and category enumerations
- Per-use-case key tuples (beyond principles)
- TTL/SWR numeric policies
- Authorization check-cache vs bypass list
- Host-mapping cache TTL vs fail-closed interaction
- Warmup set (if any) for shared hosting
- Timeout/retry/circuit numeric budgets
- Rolling-deploy cache namespace strategy
- Session/auth cache security review
- Invalidation event names (not locked here)

### DEFERRED

- Redis/vendor selection
- Distributed locks
- Message broker / scheduler / secret-store / feature-flag products
- Implementation of ports, jobs, HTTP wrappers
- Final ADR
- TB-P00-T021 and any later task
- UI implementation
