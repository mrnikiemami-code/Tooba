# TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001 — Cart semantic/ownership host closure

Task: `TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001`
Parent: `TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1`
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

This is a **post-certification semantic/ownership repair**, not a new structural
certification. Cart remains `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002
STRUCTURE_CERTIFIED`.

## 1. Host Cart residue found

Confirmed defects on the pre-repair main:

| Host file | Defect |
|---|---|
| `Host/Tooba.Host/Composition/HostCartPersistenceHoursResolver.cs` | Host implemented the Cart Application port `ICartPersistenceHoursResolver`; blocked on the async Catalog contract with `.GetAwaiter().GetResult()` and `CancellationToken.None` |
| `Host/Tooba.Host/CartExpiryHostedService.cs` | Cart-specific expiry worker implementation (tenant loop + Cart reconciler + Cart telemetry) owned by Host |
| `Host/Tooba.Host/CartExpiryHostOptions.cs` | Cart-specific worker options bound in Host (`Tooba:CartExpiry`) |
| `Host/Tooba.Host/Program.cs` | Registered `CartExpiryHostOptions`, `AddHostedService<CartExpiryHostedService>()`, and the Host Cart persistence resolver |
| `Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs` | Hardcoded `"IR"`, `"IRR"`, `SalesChannel.Marketplace` |
| `Cart.Application/Presentation/CartPresentationComposer.cs` | Hardcoded Persian user-facing fallbacks `"کالا"`, `"فروشنده"` |
| `Cart.Infrastructure/Directories/CartDirectory.cs` | Sync `ResolvePersistenceTtl()` over a sync port; hardcoded `"IR"`, `"IRR"`, `SalesChannel.Marketplace` inside the login-merge path |

## 2. Files moved / removed / added

Removed from Host:

- `Host/Tooba.Host/Composition/HostCartPersistenceHoursResolver.cs`
- `Host/Tooba.Host/CartExpiryHostedService.cs`
- `Host/Tooba.Host/CartExpiryHostOptions.cs`

Added to Cart (all Cart-owned, ARCH-COMPLETE-002 folder + namespace aligned):

- `Tooba.Cart.Infrastructure/Lifetime/CartExpiryWorker.cs` (moved from Host worker)
- `Tooba.Cart.Application/Lifetime/CartExpiryOptions.cs` (renamed from Host options; keeps section `Tooba:CartExpiry` for config compatibility)
- `Tooba.Cart.Infrastructure/Lifetime/CatalogCartPersistenceHoursResolver.cs` (async Catalog store override adapter)
- `Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs` (effective commerce context)
- `Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs`
- `Tooba.Cart.Application/Models/CartCommerceDefaultsOptions.cs` (Car-owned commerce fallback knobs)

Generic platform seams added (not Cart-specific), so a module-owned worker can
stay free of Host references:

- `BuildingBlocks/Tooba.Persistence/WorkerSeams.cs` — `IWorkerCommerceContextFactory`, `IBackgroundWorkerRegistry`
- Host implementations bound to those seams: `WorkerCommerceContextFactory`, `BackgroundWorkerRegistry`

## 3. Final ownership

- Cart expiry business reconciliation: **Cart** (`ICartExpiryReconciler` / `CartExpiryReconciler`).
- Cart expiry worker (tenant loop, cancellation, per-tenant isolation, telemetry): **Cart** (`CartExpiryWorker`).
- Cart expiry scheduling knobs: **Cart** (`CartExpiryOptions`).
- Cart persistence policy + platform fallback + clamp: **Cart** (`CartPersistenceHours`, `CartLifetimeOptions`).
- Catalog store persistence override: **Catalog** contract `IStoreCartPersistenceHoursReader`, consumed by Cart through the Cart-owned async adapter.
- Host: generic composition only (`new CartModule()` via module composition, generic platform seams). Host owns **zero** Cart-specific implementation classes.

## 4. Async persistence-hours path

- Port `ICartPersistenceHoursResolver.ResolveOverrideHoursAsync(CancellationToken)`.
- Port `ICartPersistenceHoursSource.ResolvePersistenceHoursAsync(CancellationToken)`.
- `CatalogCartPersistenceHoursResolver` awaits `IStoreCartPersistenceHoursReader.GetStoreCartPersistenceHoursAsync(cancellationToken)`.
- `CartPersistenceHours.ResolveAsync` clamps platform/override values; `CartDirectory.ResolvePersistenceTtlAsync` awaits the source.
- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or `CancellationToken.None` remains in this path (guarded).

## 5. Current commerce context abstraction reused

Inspected and reused existing canonical abstractions (no duplicate created):

- `Tooba.BuildingBlocks.ICurrentCommerceContext` / `CommerceContext` / `TenantContext.DefaultMarketReference`
- `Tooba.BuildingBlocks.ICommerceContextAssigner` (worker scope assignment)
- `Tooba.Persistence.IOutboxPollTargetSource` / `OutboxPollTarget` (generic tenant target iteration)
- `Catalog.Contracts.Reservation.IStoreCartPersistenceHoursReader` (Catalog-owned store settings)
- `Offer.Contracts.Dtos.SalesChannel`, `Pricing.Contracts.CurrencyCode`

## 6. Market / Currency / SalesChannel resolution path

`CreateGuestCartHandler` → `ICartCommerceContextResolver.Resolve()`:

1. Market: `ICurrentCommerceContext.Current.Tenant.DefaultMarketReference`, falling back to Cart-owned `Cart:CommerceDefaults:DefaultMarket`.
2. Currency: Cart-owned `Cart:CommerceDefaults:DefaultCurrency`.
3. SalesChannel: Cart-owned `Cart:CommerceDefaults:DefaultSalesChannel`.

Unresolvable market/currency **fails closed** with stable codes
`cart.commerce.market_unconfigured` / `cart.commerce.currency_unconfigured`
(no hardcoded literal, no arbitrary raw HTTP string trust, no finite currency list
inside Cart). A valid non-IRR currency (e.g. USD/EUR) can be configured without
touching Cart code.

## 7. Localization resolution / fallback decision

- `CartPresentationComposer` no longer embeds language: product title and seller
  display name fall back to `string.Empty` (neutral, null-safe), while real values
  still come from `ICatalogCartPresentationLookup` (Catalog-owned localized lookup)
  and `IPartyLookup`.
- No FA/EN branching, no English hardcode. Localization ownership stays with the
  canonical Catalog/Party boundaries.

## 8. Validator classification

`CreateGuestCartCommand` remains parameterless → **NO_VALIDATOR_REQUIRED**,
unchanged in `CartEndpointValidatorCoverageGuardTests` manifest. No validator was
added merely for its existence.

## 9. Host residual scan result

- `HostCartPersistenceHoursResolver` — no longer exists in Host.
- No Host production source implements a Cart Application port, names Cart business
  defaults/worker options, or references `ICartExpiryReconciler`.
- `Tooba.Cart.* -> Tooba.Host = 0` (no Cart project reference to Host; enforced by
  `CartArchitectureGuardTests`).
- Host retains only generic composition (`CartModule` registration) and generic
  platform seams.

## 10. Architecture guards added / strengthened

`Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs`:

- narrowed the Cart naming allowlist (removed the three Host Cart entries)
- `Host_has_no_Cart_specific_implementation_classes` (files absent; no port implementation; no Cart business defaults/worker options; no `ICartExpiryReconciler`)
- `Cart_owns_expiry_worker_and_its_worker_seams`
- `Cart_persistence_hours_path_is_fully_async` (no sync-over-async, no `CancellationToken.None`)
- `CreateGuestCart_uses_commerce_context_without_hardcoded_values`
- `Cart_presentation_has_no_language_hardcoded_fallbacks`

Strengthened: `CartLifetimeSeparationTests`, `UnpaidOrderExpiryTests`,
`HostFolderStructureTests`, `TmarDurableGuardTests` (Cart SoT coherence).

## 11. Frozen surfaces / non-changes

- No frontend production change (`frontendFrozen = true`); the pre-existing frontend
  mapper defaults were **not** touched (out of scope: frontend).
- No database schema/migration change.
- No Checkout W6 work; Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`.
- Order production code untouched; Order remains `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
- No route-contract change.

## 12. Validation

- `dotnet build src/backend/Tooba.slnx` — success (0 errors).
- `Tooba.Cart.Tests` — 17/17 passed.
- Host focused guards (architecture, folder structure, Cart lifetime separation,
  unpaid-order expiry, TMAR durable + structure gate, Cart foundation/expiry) — passed.
- Host Cart residual guard — passed.
