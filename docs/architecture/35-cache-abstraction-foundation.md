# Tooba — Cache Abstraction Foundation

Status:

```text
IN_PROGRESS — TB-P01-T008 awaiting Architect ACCEPT
```

Task:

```text
TB-P01-T008
```

```text
Cache != Source of Truth
Redis is deferred, not rejected
Tenant isolation is mandatory
```

Initial provider is in-process `Microsoft.Extensions.Caching.Memory`. It is not shared across Host instances. A future Redis provider must preserve the same logical key contract, invalidation semantics, tenant isolation, and expiration policy semantics without rewriting business modules.

Business modules consume only:

```text
ICache
ICacheKeyBuilder
ICacheInvalidator
CachePolicy
CacheKey / CacheKeyParts
```

`IMemoryCache`, Redis types, and Host-derived keys are not part of the module contract. The Host owns a private `MemoryCache` instance and does not register `IMemoryCache` for injection into modules.

P00 conceptual cache notes remain in `docs/architecture/19-caching-infrastructure-abstractions.md` (the T008 envelope named `20-caching-infrastructure.md`; that file is the frontend/UX document). This document is the P01 implementation lock for the foundation.

## Key dimensions

`CanonicalCacheKeyBuilder` encodes only dimensions that affect the value:

```text
Namespace
Edition
DeploymentId
ResourceType
ResourceId
TenantId when TenantScoped
Market when market-specific
Locale when locale-specific
Currency when currency-specific
Theme when theme-specific
AuthorizationScope when authorization-sensitive
Version when versioned
```

Hard rules:

```text
Tenant-specific value must include TenantId
Market-specific value must include Market
Locale-specific value must include Locale
Currency-specific value must include Currency
Locale != Market != Currency != Tax Jurisdiction
```

Marketplace keys must not carry a SingleStore `TenantId`. SingleStore tenant-scoped keys require a durable `TenantId`. Hostname is routing input, not a cache-key segment. Machine name is not included. Secrets and PII must not appear in raw keys.

Edition and Deployment prefixes prevent Marketplace / SingleStore / environment collisions.

Segments are escaped (`|` and `\`). Locale is lowercased; Currency is uppercased. Keys longer than 512 characters are replaced by a namespace + edition + SHA-256 digest of the canonical form.

Version/revision segments are optional and intended for content, theme, SEO config, and catalog projections — not forced onto every key.

## Tenant isolation

```text
Tenant A cache entry != Tenant B cache entry
```

even when resource identifiers match. No implicit default tenant. The caller supplies durable `TenantId`; the memory provider never derives it from Host.

## Expiration and negative caching

`CachePolicy` requires absolute and/or sliding expiration. There is no infinite default for mutable data and no single global TTL.

Not-found is not cached globally. Negative caching is explicit (`CacheNull` + short `NullAbsoluteExpiration`). Authorization failures and transient infrastructure errors must not be cached.

Authorization-sensitive output must include an authorization/user/scope dimension or must not be cached. This foundation does not introduce a general user cache model. SpiceDB is out of scope.

## GetOrCreate / stampede

The memory provider uses a per-key `SemaphoreSlim` single-flight. No global lock. `CancellationToken` is respected. Failed factories are not stored.

Distributed stampede protection requires Redis/distributed locking later. In-memory single-flight does not coordinate across Host instances.

## Invalidation

Writes/updates/deletes must explicitly invalidate relevant entries. TTL alone is not sufficient for correctness-sensitive data.

Tags and namespace invalidation are the durable seam. The memory provider tracks tag-to-key reverse indexes and cleans them on eviction so expired entries do not leave broken tag references. Future Redis may use another internal technique; business code must not depend on the index structure.

Future event-driven invalidation may consume MassTransit integration events. This task does not add business handlers.

## Serialization

The memory provider stores typed objects directly. Cached contracts must remain serialization-safe for a future distributed provider.

```text
do not cache EF tracked entities
do not cache HttpContext/request objects
do not cache DbContext
```

Cache immutable/read DTOs, projections, and value objects.

## Observability and failure

Metrics (bounded labels: provider, namespace, edition): hit, miss, set, remove, eviction/invalidation, factory duration, stampede wait.

Do not use full cache key, TenantId, user id, or resource id as metric dimensions. TenantId may appear in traces/logs only when necessary. Do not log cached values, secrets, or full keys.

```text
Technical Log != Business Audit != Security Audit != Analytics
```

Cache is optimization. Authoritative truth remains in owning modules. Memory provider failures are not expected; future distributed providers may fail-open or fail-closed by usage. This task does not add business fallback logic.

Size contract: each entry `Size = 1`; `EntryCountLimit` (default 10_000) is `MemoryCache` `SizeLimit`. Compaction percentage 0.25. Tag maps are cleaned on eviction callbacks.

Configuration (`Tooba:Cache`): `Enabled`, `Provider` (`Memory` | `None`), `EntryCountLimit`, `StampedeProtection`. Configuration cannot disable tenant isolation in the key builder. Redis provider values fail validation.

## Packages

```text
Microsoft.Extensions.Caching.Memory = 8.0.1
StackExchange.Redis = NOT ADDED
Microsoft.Extensions.Caching.StackExchangeRedis = NOT ADDED
```

MassTransit remains 8.5.10 with PostgreSQL SQL Transport. RabbitMQ is not used.

## Deferred

- Redis / distributed cache and distributed stampede locks
- Event-driven invalidation handlers
- Business-specific cache categories (catalog, pricing, tax, cart, …)
- SpiceDB / identity-aware cache products
- Stale-while-revalidate
