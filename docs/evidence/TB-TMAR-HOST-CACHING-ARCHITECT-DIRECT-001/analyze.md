# Analyze — Host/Caching Architect Direct AMC

Execution: direct Architect Analyze → Migrate → Certify using the current three Cursor architecture skills.

## Exact folder inventory

`src/backend/Host/Tooba.Host/Caching/`

| File | Classification | Disposition |
| --- | --- | --- |
| CacheHostOptions.cs | PLATFORM_CACHE_CONFIGURATION | RETAIN |
| CacheInstrumentation.cs | PLATFORM_CACHE_OBSERVABILITY | RETAIN |
| CacheRegistration.cs | PLATFORM_CACHE_COMPOSITION | RETAIN |
| MemoryToobaCache.cs | PLATFORM_CACHE_PROVIDER | RETAIN |

Production file count before: **4**

## Ownership finding

All four files are generic platform/runtime cache infrastructure. They contain no module business policy, no module DbContext/DbSet access, no HTTP endpoint ownership, and no module-specific domain state.

This is an explicit legal Host platform seam under the Host evacuation protocol. Creating a Cache/Caching business module or moving these files into an arbitrary module would be incorrect.

## Canonical mechanisms

- Contracts/key policy: `Tooba.BuildingBlocks.Cache.cs`
- Telemetry: `ToobaTelemetry.Meter`
- Provider: private in-process `MemoryCache`
- Public DI surface: `ICache`, `ICacheInvalidator`, `ICacheKeyBuilder`
- Provider choices: Memory / None; Redis rejected in this foundation
- Tenant/Edition isolation remains in the shared canonical key builder

## Defect found

`Program.cs` configured and validated `CacheHostOptions` but did **not** call `AddToobaCache()`.

That meant the runtime Host did not register the documented cache abstraction/provider even though the cache foundation was marked complete and appsettings enabled it.

Disposition: bounded runtime composition repair in `Program.cs`; no ownership move.
