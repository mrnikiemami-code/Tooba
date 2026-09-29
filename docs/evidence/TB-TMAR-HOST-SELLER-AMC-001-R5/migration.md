# TB-TMAR-HOST-SELLER-AMC-001-R5 — Migration

## Scope

Final Host/Seller closure: evacuate the last Host-owned seller surface — the Development route
`GET /v1/seller/dev-contexts` and its seller demo-actor/authorization bootstrap — out of
`Host/Seller` into the AccessControl development capability, then delete `Host/Seller` entirely.

Accepted baseline (R4):

```text
Host/Seller files   = 2  (SellerPanelEndpoints.cs, SellerDevActorBootstrap.cs)
Host seller routes  = 1  (GET /v1/seller/dev-contexts)
Host/Security/Seller = canonical + protected
```

## Decision

The remaining behavior is Development-only seller authorization/demo context. Its durable owner is
the AccessControl development capability, not Host/Seller. No separate R6 was created — the final
closure is certified in this same task.

## Target layout (AccessControl development capability, capability-first)

```text
Tooba.AccessControl.Application/Development/Seller/
    GetSellerDevContextsQuery.cs
    GetSellerDevContextsQueryHandler.cs
    ISellerDevContextStore.cs
    SellerDevContextModels.cs
Tooba.AccessControl.Infrastructure/Development/Seller/
    SellerDevContextBootstrap.cs
Tooba.AccessControl.Endpoints/Seller/Development/
    SellerDevContextEndpoints.cs
```

## Ownership moves

| Surface | Before | After |
| --- | --- | --- |
| `GET /v1/seller/dev-contexts` | `Tooba.Host.Seller.SellerPanelEndpoints` | `Tooba.AccessControl.Endpoints.Seller.Development.SellerDevContextEndpoints` |
| dev-context projection | inline anonymous objects in Host endpoint | `Tooba.AccessControl.Application.Development.Seller.GetSellerDevContextsQueryHandler` |
| snapshot port | static `SellerDevActorBootstrap.Snapshot` | `Tooba.AccessControl.Application.Development.Seller.ISellerDevContextStore` |
| seller demo/bootstrap orchestration | `Tooba.Host.Seller.SellerDevActorBootstrap` | `Tooba.AccessControl.Infrastructure.Development.Seller.SellerDevContextBootstrap` |

## Cross-module boundaries (Contracts-only)

| Dependency | Contract | Notes |
| --- | --- | --- |
| Party | `Tooba.Party.Contracts.IPartyDevelopmentSeedGateway` | organization/membership development seed; implementation stays in `Tooba.Party.Infrastructure` |
| Identity | `Tooba.Identity.Contracts.IIdentityAuthenticationService` | demo actor resolution/registration; no Identity.Infrastructure consumption |
| Authorization tuples | `Tooba.BuildingBlocks.IAuthorizationTupleWriter` | existing neutral seam; no new mechanism |

`ZERO` foreign `DbContext` / foreign `Application` / foreign `Domain` / foreign `Infrastructure` /
foreign `Persistence`.

## Host final state

Deleted (`src/backend/Host/Tooba.Host/Seller/` is now ABSENT):

```text
SellerPanelEndpoints.cs
SellerDevActorBootstrap.cs
```

Host wiring removed: `using Tooba.Host.Seller;` and `app.MapSellerPanelEndpoints();` in `Program.cs`.
Host development composition (`DevelopmentSchemaMigrator`, `MarketplaceDevelopmentBootstrap`,
`MarketplaceSellerDevBootstrap`, `SettingsFoundationDevelopmentSeed`, `SupportDevelopmentSeedHost`)
now resolves `ISellerDevContextStore` instead of the deleted Host seller bootstrap.

No sink-folder regression: `Host/Development` keeps its accepted 5-file allowlist.

`Host/Security/Seller` (R1A) is untouched and remains the canonical global Host security adapter
boundary.

## Result

```text
Host/Seller production files  2 -> 0
Host seller routes            1 -> 0
Host/Seller directory         ABSENT
duplicate route ownership     ZERO
Host/Development sink         ZERO
```

No schema/migration change. Frontend unchanged.
