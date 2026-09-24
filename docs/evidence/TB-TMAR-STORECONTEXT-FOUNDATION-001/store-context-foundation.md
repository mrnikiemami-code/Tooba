# StoreContext Foundation Extraction — Evidence

Task: `TB-TMAR-STORECONTEXT-FOUNDATION-001`
Parent: `TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001`
Track: `STORE_CONTEXT_MODULE_FOUNDATION`
Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`

## Why StoreCommerce left BuildingBlocks

`Tooba.BuildingBlocks` is the platform primitive layer (TenantId, ToobaEdition,
ConnectionReference, CommerceContext, HostNormalizer). Effective storefront
commerce semantics (Market / Currency / SalesChannel) are a *store context*
concern owned by a real module boundary, not a platform primitive. Keeping
`StoreCommerceContext` in BuildingBlocks:
- let every consumer reach store commerce authority through a platform-wide type
  instead of an owned contract;
- left no module boundary a future service could be extracted along.

TenantId / ToobaEdition / ConnectionReference / CommerceContext remain platform
primitives in BuildingBlocks and were NOT moved.

## New ownership

`Tooba.StoreContext.Contracts` (`Tooba.StoreContext.Contracts.Current`):
- `StoreCommerceContext(string? Market, string? Currency, string? SalesChannel)`
- `ICurrentStoreCommerceContext` — `StoreCommerceContext? Current { get; }`
- `IStoreCommerceContextAssigner` — `void Assign(StoreCommerceContext context)`
- `IWorkerStoreCommerceContextFactory` — `StoreCommerceContext FromTarget(ToobaEdition edition, string? tenantId)`
  (references Tooba.BuildingBlocks only for `ToobaEdition`)

`Tooba.StoreContext.Infrastructure`:
- `Current/StoreCommerceContextAccessor` — scoped, implements both
  `ICurrentStoreCommerceContext` and `IStoreCommerceContextAssigner`; no
  HttpContext, no static mutable state, no AsyncLocal; null assignment guarded.
- `StoreContextModule : IToobaModule` — registers one scoped accessor and exposes
  it through both seams. Root `.cs` allowlist = `StoreContextModule.cs` only.

No DB, schema, migration, Endpoints, or MediatR request (this phase introduces no
application use-case).

`SalesChannel` remains a stable string at this boundary; no second enum was
introduced. SalesChannel ownership stays with Offer.

## Request assignment path

`TenantResolutionMiddleware.Resolve(...)` now returns
`(CommerceContext Context, StoreCommerceContext StoreCommerce)`:
- technical CommerceContext no longer carries StoreCommerce;
- Marketplace → `_registry.DeploymentStoreCommerce`;
- SingleStore → `record.StoreCommerce`;
- unknown/disabled tenant behavior unchanged (404 fail-closed);
- after resolving, the middleware assigns the store context through
  `httpContext.RequestServices.GetRequiredService<IStoreCommerceContextAssigner>()`
  (the middleware is constructed by the root provider, so the scoped assigner is
  resolved per-request rather than injected into the middleware constructor).
No raw request/header value can become Market/Currency/SalesChannel authority.

## Background-worker assignment path

- Technical `WorkerCommerceContextFactory.FromOutbox/FromPollTarget` still returns
  BuildingBlocks `CommerceContext` WITHOUT StoreCommerce.
- `OutboxDispatcher.DispatchTargetAsync` assigns technical context through
  `ICommerceContextAssigner` and store context through `IStoreCommerceContextAssigner`
  using `_workerStoreContext.FromTarget(target.Edition, target.TenantId)`.
- `CartExpiryWorker.ReconcileOnceAsync` does the same inside each created scope,
  using the target edition/tenant to resolve store context.

## Allowed temporary Host adapter

`WorkerStoreCommerceContextFactory` (implemented in the existing
`Outbox/OutboxWorkerSeams.cs`, registered in `Program.cs`) is an explicitly
allowed temporary platform composition adapter for the Foundation Phase:
- it only selects deployment StoreCommerce for Marketplace, or the active tenant's
  StoreCommerce for SingleStore;
- it contains no Market/Currency/SalesChannel business defaults, normalization, or
  hardcoded `IR`/`IRR`/`Direct`/`Marketplace` values.

## Cart dependency before/after

- Before: `CartCommerceContextResolver` depended on
  `ICurrentCommerceContext` and read `Current?.StoreCommerce`; Cart.Infrastructure
  did not reference StoreContext.
- After: the resolver depends on `ICurrentStoreCommerceContext` and reads
  `Current` directly; `Tooba.Cart.Infrastructure` references
  `Tooba.StoreContext.Contracts`. Cart keeps zero Host dependency.
- Fail-closed codes preserved exactly: `cart.commerce.context_unavailable`,
  `cart.commerce.market_unconfigured`, `cart.commerce.currency_unconfigured`,
  `cart.commerce.channel_unconfigured`. No fallback added.

## BuildingBlocks cleanup

`Tooba.BuildingBlocks/CommerceContext.cs` no longer declares `StoreCommerceContext`
and `CommerceContext` no longer has a `StoreCommerce` parameter. ToobaEdition,
TenantStatus, TenantId, ConnectionReference, EditionContext, TenantContext,
ICurrentCommerceContext, ICurrentEdition, ICurrentTenant, HostNormalizer are
untouched. BuildingBlocks now contains zero effective-store commerce semantics.

## Fail-fast preservation

`PlatformOptionsValidator` production behavior is unchanged: it still validates
Market/Currency/SalesChannel for Marketplace and every ACTIVE SingleStore tenant,
still validates SalesChannel against canonical `Tooba.Offer.Contracts.Dtos.SalesChannel`,
and still uses the same Market fallback (`StoreCommerce.Market` else
`DefaultMarketReference`). `PlatformOptionsValidatorTests` pass unchanged.

## No new default / no Shared-DB claim

No Market/Currency/SalesChannel default was introduced anywhere. Shared-DB
edition/runtime was NOT designed, implemented, or claimed in this phase.

## Validation results

- Focused guards (`HostCartResidualGuardTests` incl. new
  `StoreContext_owns_effective_store_commerce_and_BuildingBlocks_does_not`,
  `PlatformOptionsValidatorTests`, `ArchitectureBoundaryTests`,
  `TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests`): PASS (41/41).
- `Tooba.Cart.Tests`: PASS (17/17).
- `dotnet build src/backend/Tooba.slnx`: succeeded, 0 errors.
- New architecture guard asserts: BuildingBlocks has no StoreCommerceContext;
  StoreContext Contracts/Infrastructure exist with the three seams; Infrastructure
  root only allows `StoreContextModule.cs`; Contracts excludes Cart/Host; Cart
  Infrastructure references StoreContext.Contracts and no Host; Host adapter carries
  no commerce-authority fallback literal.

### Environmental note (not a task defect)

`WebApplicationFactory`-based tests (`TenantResolutionTests`, etc.) hang / crash the
test host in this local environment. This was reproduced on a pristine detached
worktree at the parent commit `ebad4757` with no task changes, confirming it is a
pre-existing environmental issue, not introduced by this task. Purely unit/guard
focused tests were run and pass.
