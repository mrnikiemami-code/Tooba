# Closure — Host/Caching Architect Direct AMC

## Final folder state

`src/backend/Host/Tooba.Host/Caching/` production file count: **4**

The folder is intentionally **not** forced to ZERO.

| File | Final disposition |
| --- | --- |
| CacheHostOptions.cs | RETAINED_ALLOWED_PLATFORM_CACHE_CONFIGURATION |
| CacheInstrumentation.cs | RETAINED_ALLOWED_PLATFORM_CACHE_OBSERVABILITY |
| CacheRegistration.cs | RETAINED_ALLOWED_PLATFORM_CACHE_COMPOSITION |
| MemoryToobaCache.cs | RETAINED_ALLOWED_PLATFORM_CACHE_PROVIDER |

## Migration/repair performed

- No cache file moved.
- No Cache/Caching module created.
- `Program.cs` now calls `builder.Services.AddToobaCache();` immediately after cache options validation registration.
- This restores the runtime composition promised by `docs/architecture/35-cache-abstraction-foundation.md`.

## Certification findings

- Module business authority: ZERO
- Module persistence authority: ZERO
- Cross-module DB access/join: ZERO
- HTTP endpoint ownership: ZERO
- Canonical telemetry: YES (`ToobaTelemetry.Meter`)
- High-cardinality metric labels: none in cache instrumentation
- Full cache-key logging: none
- Redis package/provider activation: NO
- Schema/migration changes: NONE
- Frontend changes: NONE
- Exact Host/Caching retained-file allowlist guard: ADDED

## Validation limitation

Static repository verification is complete. This Architect environment cannot execute the repository's .NET build/test toolchain, so runtime/focused test execution is **not claimed**.

Required focused command in a repository-capable environment:

`dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostCachingAmcGuardTests|FullyQualifiedName~CacheFoundationTests"`

Until that command passes, this is **implementation-complete / runtime-validation-pending**, not a fabricated test PASS.
