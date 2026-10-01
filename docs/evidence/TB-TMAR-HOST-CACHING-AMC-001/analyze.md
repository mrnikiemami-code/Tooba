# Analyze — TB-TMAR-HOST-CACHING-AMC-001

Disposition: KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE (not HOST_ZERO).

Retained allowlist (exact 4):
- CacheHostOptions.cs
- CacheInstrumentation.cs
- CacheRegistration.cs
- MemoryToobaCache.cs (includes DisabledToobaCache)

Neutral contracts remain in BuildingBlocks/Cache.cs.

Blockers repaired:
1. Path↔namespace → Tooba.Host.Caching
2. Single-flight CurrentCount==1 remove race
3. Type mismatch silent hit-null
4. Zero/negative TTL acceptance
5. Memory/None invalidation argument parity
