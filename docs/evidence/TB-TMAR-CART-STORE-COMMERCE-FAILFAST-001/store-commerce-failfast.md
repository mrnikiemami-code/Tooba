# StoreCommerce Startup Fail-Fast — Evidence

Task: `TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001`
Parent: `TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1`
Track: `STORE_COMMERCE_FAILFAST`
Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`

## Architect defect

After the Cart commerce-authority repair (R1), the platform owned effective
`StoreCommerceContext` (Market/Currency/SalesChannel), but production could still
start with missing or invalid `Tooba:StoreCommerce` configuration and only fail
later when Cart was first used (`cart.commerce.*` at request time).

The required behavior is startup fail-fast: an invalid commerce configuration must
prevent the process from starting, not surface as a late runtime failure.

## Files changed

- `src/backend/Host/Tooba.Host/Configuration/ToobaPlatformOptions.cs`
  - `using Tooba.Offer.Contracts.Dtos;` added (host graph already references it
    transitively through `Tooba.Offer.Application`/`Endpoints`/`Infrastructure`;
    no new `ProjectReference` was added).
  - `PlatformOptionsValidator.ValidateProductionRequirements` now ends with
    `return ValidateStoreCommerce(registry);`.
  - New `ValidateStoreCommerce(ControlPlaneRegistry)`: validates the deployment
    context for `Marketplace`, and each **Active** tenant's resolved context for
    `SingleStore`.
  - New `ValidateStoreCommerceRecord(scope, StoreCommerceContext)`: fails when
    effective Market or Currency is blank, then validates SalesChannel.
  - New `ValidateSalesChannel(scope, raw)`: parses against the canonical
    `Tooba.Offer.Contracts.Dtos.SalesChannel` enum; unknown values fail.
  - `TenantRecordOptions.StoreCommerce` XML comment corrected to state the real
    inheritance: Market may fall back to `DefaultMarketReference`; Currency and
    SalesChannel require explicit tenant values.

- `src/backend/Host/Tooba.Host.Tests/TenantResolutionTests.cs`
  - Added the required focused `PlatformOptionsValidatorTests` cases.
  - `SampleSingleStore()` now carries `DefaultMarketReference = "IR"` and a
    `Tenant:alpha` connection reference so the validation assertion is specific.

- `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs`
  - `nextTask` expectation updated to `USER_REVIEW_CART_STORE_COMMERCE_FAILFAST_001`
    and `lastAcceptedTask` expectation to `TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001`.

- `docs/architecture/tmar-current-state.json`
  - `lastAcceptedTask` / `nextTask` updated; `hostCartBoundary` gained
    `storeCommerceStartupValidation` and `storeCommerceSalesChannelValidation`.
  - Cart post-certification note records the follow-up fail-fast hardening.

Read-only (not changed): `src/backend/BuildingBlocks/Tooba.BuildingBlocks/CommerceContext.cs`,
`src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs`.

## Final validation behavior

Production Marketplace:

- Startup fails when `Tooba:StoreCommerce` Market, Currency, or SalesChannel is
  missing/blank.
- Startup fails when `Tooba:StoreCommerce:SalesChannel` is not a canonical
  `SalesChannel` value (e.g. `Marketplce`).

Production SingleStore:

- Startup fails when any **Active** tenant's effective StoreCommerce has no
  Currency or no/invalid SalesChannel.
- Effective Market is `StoreCommerce.Market` when set, otherwise the existing
  `DefaultMarketReference` fallback; if both are absent, startup fails.
- `Disabled` / `Suspended` tenants are skipped (no runtime commerce validation).
- Validation is independent of connection references: a missing/invalid commerce
  value surfaces even when all connection references are present.

Currency: non-empty configured value required; no currency list, no hardcoded
`IRR`/`USD`/`EUR` fallback.

Market: no new Cart-side fallback; SingleStore behavior preserved.

SalesChannel: validated at startup against the canonical
`Tooba.Offer.Contracts.Dtos.SalesChannel`. No second enum introduced; no
SalesChannel ownership moved into BuildingBlocks; no hardcoded `Direct` or
`Marketplace` fallback.

## Runtime inheritance semantics

Unchanged. `ResolveStoreCommerce` and `BuildRegistry` were not modified; this task
only adds startup validation and corrects the misleading comment.

## Preserved

- Cart owns no commerce policy default; `CartCommerceDefaultsOptions` remains removed.
- Host owns zero Cart-specific implementation.
- Cart remains `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
- Order remains certified.
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`.
- Frontend frozen (`ARCH-FE-FREEZE-001`); no production frontend change.

## Validation results

- Focused `PlatformOptionsValidatorTests`: PASS (13/13).
- `HostCartResidualGuardTests`: PASS.
- `TmarDurableGuardTests`: PASS (5/5) after SoT stamp.
- `dotnet build src/backend/Tooba.slnx`: succeeded, 0 errors.
- No broad unrelated suites run; no new production file; no new project reference.
