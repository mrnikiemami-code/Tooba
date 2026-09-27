# Validation — Host/Caching Architect Direct AMC

## Static verification completed

- exact Host/Caching inventory rechecked: 4 files
- all 4 files read completely
- no DbContext / DbSet / SaveChanges / raw SQL in Host/Caching
- no endpoint mapping in Host/Caching
- `CacheInstrumentation` uses `ToobaTelemetry.Meter`
- metric labels are provider / namespace / edition only
- `Program.cs` previously lacked `AddToobaCache()`
- runtime registration call added exactly once
- existing `CacheFoundationTests` already covers tenant/edition isolation, single-flight, invalidation, cancellation, no Redis packages, and private MemoryCache behavior
- durable `HostCachingAmcGuardTests` added

## Runtime validation

NOT EXECUTED in Architect tool environment.

Required focused validation:

`dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostCachingAmcGuardTests|FullyQualifiedName~CacheFoundationTests"`

No full solution suite is required.
