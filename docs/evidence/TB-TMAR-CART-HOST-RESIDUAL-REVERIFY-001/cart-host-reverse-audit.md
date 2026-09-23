# TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001 — Host → Cart Reverse Audit

Scope: `src/backend/Host/Tooba.Host/**/*.cs` (production only; `Tooba.Host.Tests`, `bin`, `obj`, `Migrations` excluded).
Order STRUCTURE-LOCK-001 work was not touched.

## Discovery

`Tooba.Host` production `.cs` files: enumerated exhaustively. Cart relation discovered by
filename regex, type/method declarations, `/cart` route registration, `Tooba.Cart.{Application,
Infrastructure,Domain,Contracts,Endpoints}`, `ICart*`, `CartLifetime*`, Cart global usings,
background workers, and cross-module policy ports.

13 production files name Cart. All are classified below. Route registration scan for
`Map*(` `/cart…` in Host: none.

## Classification table

| Host file | Symbol / method | Cart dependency | Business decision? | Cart state read/write? | Classification | Action | Final owner | Why allowed if retained |
|---|---|---|---|---|---|---|---|---|
| `Program.cs` | `AddCartEndpointPresentation()`, `MapCartEndpoints()`, `AddHostedService<CartExpiryHostedService>`, `AddScoped<ICartPersistenceHoursResolver, HostCartPersistenceHoursResolver>` | Cart.Endpoints, Cart.Application assembly | No | No | ALLOWED_THIN_HOST_COMPOSITION | Kept | Cart + Host composition | Endpoint/module/MediatR composition + Cart-owned seam registration only |
| `CartExpiryHostedService.cs` | `ReconcileOnceAsync` | `ICartExpiryReconciler` | No | No | ALLOWED_HOST_EXECUTION_SHELL | Reduced to shell (tenant loop, config, telemetry, logging, cancellation) | Cart (`CartExpiryReconciler`) | Worker no longer resolves `ICartDirectory`, no `UtcNow`, no batch policy |
| `CartExpiryHostOptions.cs` | options class | Host scheduling knobs | No | No | ALLOWED_HOST_EXECUTION_SHELL | Doc clarified as execution-only | Host | Contains only Enabled/PollInterval/BatchSize execution knobs |
| `CommerceHoldPolicy.cs` | `ResolveCartPersistenceHours` | `ICartPersistenceHoursSource` | No (removed) | No | ALLOWED_THIN_HOST_COMPOSITION | Removed `CartLifetimeOptions`, platform clamp/fallback, `ICartPersistenceHoursSource` implementation; now forwards Cart-owned value | Cart owns policy; Catalog owns stored override | Payment/Order hold adapter only; forwards Cart-owned persistence value |
| `CheckoutReservationHoldPolicy.cs` | Order hold port adapter | none (Payment options only) | No | No | ALLOWED_THIN_HOST_COMPOSITION | Unchanged | Order | Order hold port; no Cart authority |
| `Composition/ToobaModuleComposition.cs` | `AddToobaModules` | `Tooba.Cart.Infrastructure.DependencyInjection` | No | No | ALLOWED_THIN_HOST_COMPOSITION | Import narrowed to DI-only | Host composition | Module list composition only |
| `Composition/HostCartPersistenceHoursResolver.cs` | `ICartPersistenceHoursResolver` | Cart port + Catalog Contracts reader | No | No | ALLOWED_THIN_HOST_COMPOSITION | New Host seam | Cart (`CartPersistenceHours`) / Catalog (stored value) | Forwards Catalog-owned store override; no clamp/fallback/policy |
| `Admin/HoldPolicySettingsEndpoints.cs` | `BuildViewAsync` | `ICartPersistenceHoursSource` | No (removed) | No | ALLOWED_THIN_HOST_COMPOSITION | Replaced `CartLifetimeOptions` + inline clamp with Cart-owned source | Cart | Settings admin UX; displays Cart-owned value |
| `Admin/ProductWorkspaceDevelopmentBootstrap.cs` | `CartDbContext` migration | Cart.Infrastructure.Persistence | No | Development migration only | ALLOWED_THIN_HOST_COMPOSITION (dev-only) | Unchanged | Cart (schema) | Development-only migrations under `Development` gate; no runtime business authority |
| `AccessControl/AccessControlDevelopmentSeed.cs` | `ICartDirectory` guest-cart seeding | Cart.Application/Ports/Contracts | No | Development seed only | ALLOWED_THIN_HOST_COMPOSITION (dev-only) | Unchanged | Cart | Development sample data via Cart directory; no runtime authority |
| `Storefront/StorefrontModels.cs` | `StorefrontCartPage` / `CartId` | CartId wire field | No | No | NON_CART_HOST_CONCERN | Unchanged | Storefront presentation | Wire DTO carries CartId; no Cart logic |
| `Storefront/StorefrontComposer.cs` | `CartMutationEnabled: true` | flag | No | No | NON_CART_HOST_CONCERN | Unchanged | Storefront presentation | Storefront read flag |
| `Storefront/StorefrontEndpoints.cs` | `cartAnonymousAllowed` | flag name | No | No | NON_CART_HOST_CONCERN | Unchanged | Catalog checkout identity policy | Flag derived from Catalog policy; no Cart authority |
| `Order/HostOrderStorefrontActor.cs` | `BuildCartAccess` | `CartAccess` (Contracts) | No | No | ALLOWED_THIN_HOST_COMPOSITION | Unchanged | Cart (Contracts) | Session → CartAccess adapter |
| `Customer/HostFulfillmentCustomerAuthorizer.cs` | `ICartQueryGateway.GetCartAsync` | Cart.Contracts | No | Read-only ownership probe | ALLOWED_THIN_HOST_COMPOSITION | Unchanged | Cart (Contracts) | Guest ownership check through Cart contracts |
| `GlobalUsings.SettlementApp.cs` | global usings | Cart imports removed | No | No | NON_CART_HOST_CONCERN | Renamed from `GlobalUsings.CartSettlementApp.cs`; Cart usings deleted | Settlement | Settlement-only global usings |
| `GlobalUsings.SettlementDomain.cs` | global usings | Cart Domain imports removed | No | No | NON_CART_HOST_CONCERN | Renamed from `GlobalUsings.CartSettlementDomain.cs`; Cart Domain usings deleted | Settlement | Settlement-only global usings |
| `OfferGlobalUsings.cs` | Offer alias | none | No | No | NON_CART_HOST_CONCERN | Unchanged | Offer | Not Cart |
| `UnpaidOrderExpiryHostedService.cs` / `.Options` | Order worker shell | none | No | No | NON_CART_HOST_CONCERN | Unchanged | Order | Order worker, no Cart authority |
| `PaymentReconciliationHostedService.cs` / `.Options` | Payment worker shell | none | No | No | NON_CART_HOST_CONCERN | Unchanged | Payment | Payment worker, no Cart authority |
| `FulfillmentReturnsGridAliases.cs` | grid aliases | none | No | No | NON_CART_HOST_CONCERN | Unchanged | Fulfillment/Returns | Not Cart |

## Counts

| Classification | Count |
|---|---|
| ILLEGAL_CART_AUTHORITY | 0 |
| DEAD_CART_RESIDUE | 0 |
| ALLOWED_THIN_HOST_COMPOSITION | 9 |
| ALLOWED_HOST_EXECUTION_SHELL | 2 |
| NON_CART_HOST_CONCERN | 11 |

PASS requires `ILLEGAL_CART_AUTHORITY = 0` and `DEAD_CART_RESIDUE = 0` → satisfied.

## Removed illegal authority

1. **Cart expiry reconciliation** — Host resolved `ICartDirectory` and called `ExpireDueCartsAsync` with `DateTimeOffset.UtcNow` + batch policy. Now Cart owns `ICartExpiryReconciler.ReconcileAsync(batchSize, ct)`; the implementation (`Tooba.Cart.Infrastructure/Lifetime/CartExpiryReconciler.cs`) uses `IClock.UtcNow` and owns batching. Host worker resolves exactly one Cart-owned reconciler.
2. **Cart persistence policy** — `CommerceHoldPolicy` implemented `ICartPersistenceHoursSource`, read `CartLifetimeOptions`, and computed platform fallback + clamp (`168`, `1..24*90`). Cart now owns `CartPersistenceHours` (fallback + clamp + `MaxHours`); Host only forwards a Catalog-owned store override through `HostCartPersistenceHoursResolver`.
3. **Cart Domain global imports** — `Tooba.Cart.Domain.{Aggregates,Entities,Events}` removed from production Host global usings.
4. **Broad Cart Application/Infrastructure global imports** — removed; only explicit composition imports remain.

## Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs` — explicit, non-wildcard allowlists; detects `CartDbContext`, `Tooba.Cart.Domain` imports, `Tooba.Cart.Infrastructure.Persistence`, Cart domain types (`ShoppingCart`, `CartLine`, `CartStatus`, `CartConversionIntent`), Cart-named Host files, Cart global usings, duplicate Host `/cart` routes, and Host-owned expiry orchestration.
