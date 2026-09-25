# TB-TMAR-HOST-ACCESSCONTROL-PLATFORM-READER-001 — Evacuate HostPlatformEffectiveAccessReader

Mode: one bounded responsibility move. Host file removed only after destination implementation existed.

## Move

| | Before | After |
| --- | --- | --- |
| File | `src/backend/Host/Tooba.Host/AccessControl/HostPlatformEffectiveAccessReader.cs` | `src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Adapters/Security/PlatformEffectiveAccessReader.cs` |
| Type | `Tooba.Host.AccessControl.HostPlatformEffectiveAccessReader` (internal) | `Tooba.AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader` (public) |
| Namespace | `Tooba.Host.AccessControl` | `Tooba.AccessControl.Infrastructure.Adapters.Security` (matches physical path exactly) |
| Interface | `IPlatformEffectiveAccessReader` | unchanged, same interface |

Visiblity raised `internal` → `public` because the composition root (Host) must reference the type
in DI while AccessControl.Infrastructure has no `InternalsVisibleTo` to Host. No semantic change.

## Project reference added

`Tooba.AccessControl.Infrastructure.csproj` gained the canonical BuildingBlocks reference to reach
the `Tooba.BuildingBlocks.Security` seam:

```xml
<ProjectReference Include="..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj" />
```

This matches the existing pattern (`Tooba.Identity.Infrastructure` already references
`Tooba.BuildingBlocks`).

## Semantic parity (unchanged)

- `PlatformAccessOwnerKind.Seller` → `AccessOwnerScopeKind.Seller`, otherwise `Platform`.
- `ownerScopeId` propagated verbatim into `AccessOwnerScope.OwnerScopeId`.
- Single `access.GetEffectiveAccessAsync(userId, owner, ct)` call.
- `PlatformPermissionGrant(p.PermissionId, MapScope(p.ScopeKind), p.ScopeResourceId, p.DeniedByCeiling)`.
- `MapScope`: `Category/Product/Brand/Warehouse` mapped, default `GlobalWithinOwner`.

## DI

`src/backend/Host/Tooba.Host/Program.cs:202` re-pointed:

```csharp
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader, Tooba.AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader>();
```

Host remains composition root only; no service locator, no duplicate implementation.

## Verification

- Repo-wide targeted search for `HostPlatformEffectiveAccessReader`: **ZERO** references.
- Production implementations of `IPlatformEffectiveAccessReader`: **exactly one** (the new adapter).
- `dotnet build Tooba.AccessControl.Infrastructure.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.

## Boundaries honored

- `BuildingBlocks.Security` seam (`IPlatformAccessSeams.cs`) untouched.
- `Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerAuthorizer.cs` and Fulfillment tests untouched.
- `AccessControlEndpoints.cs`, `AccessControlDemoSnapshot.cs`, `AccessControlDevelopmentSeed.cs` untouched.
- No `AccessControl.Contracts` / `AccessControl.Endpoints` created; no CQRS work.
- Checkout and frontend untouched.
