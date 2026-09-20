# Cache Architecture Plan

Foundation exists: ICache / ICacheKeyBuilder / ICacheInvalidator / CacheRegistration (Memory|None); Redis deferred.

Bypass sites (must migrate): StoreLandingPageComposer, StoreMenuComposer, StoreAppearanceProjection (+ tests) using `IMemoryCache` directly.

Target: all consumption via ICache. Redis later: distributed invalidation, stampede, jitter TTL, tenant/store/locale keys, metrics — no Redis types in modules.
