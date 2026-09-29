# TB-TMAR-HOST-SELLER-AMC-001-R5 — Dev-context ownership

## Route

```text
GET /v1/seller/dev-contexts
```

Single owner after R5: `Tooba.AccessControl.Endpoints.Seller.Development.SellerDevContextEndpoints`
mapped on the AccessControl-owned `sellerDevelopment = app.MapGroup("/v1/seller")` group inside
`AccessControlEndpointModule`. Registered exactly once (`SellerDevContextEndpoints.Map(` count = 1);
`Program.cs` no longer mentions `dev-contexts` and maps `MapAccessControlModuleEndpoints()`.

Endpoint layering:

```text
Endpoints (IHostEnvironment + ISender only)
  -> Application GetSellerDevContextsQuery / GetSellerDevContextsQueryHandler
     -> Application port ISellerDevContextStore
        -> Infrastructure SellerDevContextBootstrap
```

The endpoint has no `DbContext` reference and no `Tooba.AccessControl.Infrastructure` reference.

## Behavior parity (preserved exactly)

| Aspect | Preserved value |
| --- | --- |
| Path / verb | `GET /v1/seller/dev-contexts` |
| Outside Development | `404` with stable code `seller.dev.unavailable` |
| Snapshot not ready | `503` with stable code `seller.dev.not-ready` |
| Response field set | `actors[]`: `actorUserId`, `actorLabel`, `sellerPartyId`, `sellerLabel`, `contextKind` |
| contextKind values | `seller-owner`, `seller-owner-alt`, `scoped-employee` |
| Demo actors | `seller-actor-a@tooba.local`, `seller-actor-b@tooba.local` |
| Demo labels | `اپراتور آرمان` (A), `اپراتور دیجی‌استایل` (B) |
| Seller labels | `فروشگاه آرمان` (A), `دیجی‌استایل نمونه` (B) |
| Membership semantics | `IPartyDevelopmentSeedGateway.FindDevelopmentOrganizationByDisplayNameAsync` with `FindDevelopmentMembershipSellerPartyAsync` fallback, then `EnsureDevelopmentMemberMembershipAsync` |
| Member tuple semantics | `IAuthorizationTupleWriter` writes `AuthorizationRelations.Member` on `AuthorizationObjectTypes.Party` for each actor |
| Scoped employee | `PublishScopedEmployee` appends the third `scoped-employee` row when present |
| Fail-closed | `InvalidOperationException` from the tuple writer (authorization engine unavailable, e.g. `Mode=Disabled`) is swallowed so startup does not break and the seller path stays fail-closed |

## Contract boundaries

| Dependency | Contract | Forbidden |
| --- | --- | --- |
| Party | `Tooba.Party.Contracts.IPartyDevelopmentSeedGateway` | `PartyDbContext`, `Tooba.Party.Domain`, `Tooba.Party.Infrastructure` from AccessControl |
| Identity | `Tooba.Identity.Contracts.IIdentityAuthenticationService` (+ `Problems.IdentityDuplicateIdentifierFault`) | `Tooba.Identity.Infrastructure` from AccessControl |
| Authorization | `Tooba.BuildingBlocks.IAuthorizationTupleWriter` | service locator, new parallel auth mechanism |

`IPartyDevelopmentSeedGateway` is implemented inside `Tooba.Party.Infrastructure`
(`PartyDevelopmentSeedGateway : IPartyDevelopmentSeedGateway`) and exposes no Party persistence or
Domain types.

## Composition

- `AccessControlModule` registers `SellerDevContextBootstrap` (scoped) and
  `ISellerDevContextStore` resolved through it.
- Host development composition resolves the same `ISellerDevContextStore` port, so the demo
  contexts are still published during Host development startup without any Host-owned seller type.

AccessControl structure certification is preserved: capability folders only, exact path↔namespace
equality, no root-artifact relaxation, no alias workaround.
