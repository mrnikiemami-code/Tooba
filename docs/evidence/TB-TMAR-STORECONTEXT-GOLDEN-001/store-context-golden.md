# TB-TMAR-STORECONTEXT-GOLDEN-001 — StoreContext golden certification & multi-currency-safe currency semantics

Parent task: TB-TMAR-STORECONTEXT-FOUNDATION-001 (ACCEPTED)
Track: STORE_CONTEXT_GOLDEN_HARDENING
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE (frontend untouched)

## 1. Parent accepted state

Architect verified on `main`:

- implementation commit: `48720fd3ae5bc69ccb5ae6532a2000f7a2b9d4f8`
- SoT stamp: `95706f174cbfe63f32cc84641b77ace403ab190f`

Verified facts at parent acceptance:

- `StoreCommerceContext` removed from `Tooba.BuildingBlocks`
- `Tooba.StoreContext.Contracts` + `Tooba.StoreContext.Infrastructure` exist
- request and worker assignment paths are separate
- Cart consumes `StoreContext.Contracts`
- parent production fail-fast behavior preserved
- no Shared-DB implementation was introduced

## 2. Objective (one only)

Harden StoreContext as a **GOLDEN** architecture foundation from the start and prevent its
currency field from being misread as the transaction/line/order currency.

This task does **not** implement multi-currency Cart/Order. It only makes StoreContext semantics
explicitly **DEFAULT-context** semantics and structure-certifies the new module.

## 3. Closed architecture decision

StoreContext may define the store/storefront **DEFAULT commerce context**. It must NOT define the
currency of every Offer, PriceQuote, CartLine, OrderLine, or PaymentGroup.

Therefore:

- `StoreCommerceContext.Currency` → `StoreCommerceContext.DefaultCurrency`
- Meaning: default/preferred currency used when a use-case needs an *initial* currency.
  - NOT an invariant that all lines in a cart/order must share one currency
  - NOT a settlement currency
  - NOT a payment-group currency
- No `AllowedCurrencies` model was introduced in this task.

## 4. DefaultCurrency semantics decision

`StoreCommerceContext` now reads:

```csharp
public sealed record StoreCommerceContext(
    string? Market,
    string? DefaultCurrency,
    string? SalesChannel);
```

XML documentation on the contract states explicitly:

- default storefront currency only
- consumer transaction lines may carry their own currency
- StoreContext does not impose single-currency Cart/Order semantics

No second currency type was added. No duplicated `SalesChannel` enum was added.
`SalesChannel` remains a boundary string validated against the canonical
`Tooba.Offer.Contracts.Dtos.SalesChannel`.

## 5. Canonical config key

Host configuration renamed (`src/backend/Host/Tooba.Host/Configuration/ToobaPlatformOptions.cs`):

- `StoreCommerceOptions.Currency` → `StoreCommerceOptions.DefaultCurrency`
- `ResolveStoreCommerce` reads `raw?.DefaultCurrency`
- production fail-fast message:
  `Production {scope} requires effective StoreCommerce:DefaultCurrency to be configured.`

Canonical configuration keys:

```text
StoreCommerce:DefaultCurrency
SingleStore:Tenants:<n>:StoreCommerce:DefaultCurrency
```

**No silent `Currency` fallback alias is retained.** This is pre-release architecture cleanup; one
canonical key only.

Preserved behavior:

- Market fallback through `DefaultMarketReference`
- `SalesChannel` validation against canonical `Tooba.Offer.Contracts` enum
- Disabled/Suspended tenant skip behavior
- no hardcoded currency values

Updated configuration/test data:

- `src/backend/Host/Tooba.Host/appsettings.Development.json`
- `src/backend/Host/Tooba.Host.Tests/TenantResolutionTests.cs`

Production config carries no StoreCommerce value today; no production values were invented.

## 6. Cart adapter (wording only)

Files touched:

- `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs`
- `src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs`
- `src/backend/Modules/Cart/Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs`
- `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs` (call-site only)

The resolver now reads `store.DefaultCurrency` and maps it into
`CartCommerceContext(Market, DefaultCurrency, Channel)`, with the existing fail-closed
`cart.commerce.currency_unconfigured` code unchanged. `CreateGuestCart` passes
`context.DefaultCurrency`.

This is an initial/default selection only.

Not changed: `ShoppingCart` schema/domain currency model, CartLine quoted-currency model, pricing
resolution behavior. Cart is **not** claimed to be multi-currency after this task.

## 7. Why MediatR is NOT_APPLICABLE

StoreContext is a platform context provider. It exposes read/assign seams
(`ICurrentStoreCommerceContext`, `IStoreCommerceContextAssigner`,
`IWorkerStoreCommerceContextFactory`) and has **no Command / Query / use-case** in this foundation.
There is nothing to dispatch, so no MediatR handler exists and no ceremonial MediatR was added.

## 8. Why Endpoints are NOT_APPLICABLE

StoreContext is `INTERNAL_ONLY` platform context. It owns no HTTP surface, no route, and no
endpoint module; `endpointOwnership = NOT_APPLICABLE`. Adding an Endpoints project would
misrepresent applicability.

## 9. Structure manifest

`docs/architecture/tmar-module-structure-manifests.json` now declares:

```json
{
  "module": "StoreContext",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [
    {
      "projectName": "Tooba.StoreContext.Contracts",
      "rootAllowlist": [],
      "forbiddenRootFiles": ["StoreCommerceContext.cs"],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.StoreContext.Infrastructure",
      "rootAllowlist": ["StoreContextModule.cs"],
      "forbiddenRootFiles": ["StoreCommerceContextAccessor.cs"],
      "forbiddenTopLevelFolders": []
    }
  ]
}
```

Applied locks:

- `PATH_NAMESPACE_ALIGNMENT` — capability folder `Current` ⇒
  `Tooba.StoreContext.Contracts.Current` / `Tooba.StoreContext.Infrastructure.Current`
- `ROOT_ALLOWLIST` — Contracts root has zero `.cs`; Infrastructure root has only
  `StoreContextModule.cs`
- `NO_NAMESPACE_ALIAS_WORKAROUND`
- no god-file; ARCH-SIZE locks honored

No Application project (no use-case). No Endpoints project (internal platform context). No
ceremonial MediatR.

## 10. Multi-currency protection statement

`StoreContext.DefaultCurrency` is **ONLY** a default selection input.

It MUST NOT be used as proof that:

- all offers are priced in one currency,
- all CartLines must share one currency,
- all OrderLines must share one currency,
- one payment can necessarily settle all currencies.

Existing Cart-level single-currency behavior is **NOT** repaired in this task and is reported as
residual architecture debt for a next dedicated wave.

## 11. Known Cart single-currency residual debt

`ShoppingCart.Currency` / cart pricing currency and the CartLine quoted-currency model still assume
a single cart pricing currency. `TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001` only hardened startup
validation; this task only renamed semantics at the StoreContext boundary. The Cart
single-currency transaction model remains **NOT REPAIRED** and must be addressed by a separately
authorized wave. StoreContext must never encode that debt as global transaction policy.

## 12. SoT classification

`docs/architecture/tmar-current-state.json`:

- `storeContext.state = PLATFORM_CONTEXT_REFERENCE_PATTERN`
- `storeContext.httpApplicability = INTERNAL_ONLY`
- `storeContext.endpointOwnership = NOT_APPLICABLE`
- `storeContext.cqrs = NOT_APPLICABLE_NO_APPLICATION_USE_CASE`
- `storeContext.structureCertifiedUnderArchComplete002 = true`
- `storeContext.currencySemantics = DEFAULT_CURRENCY_ONLY_NOT_TRANSACTION_SINGLE_CURRENCY_INVARIANT`
- `storeContext.canonicalConfigKey = StoreCommerce:DefaultCurrency`
- `storeContext.knownCartCurrencyDebt = CART_STILL_HAS_SINGLE_CART_PRICING_CURRENCY_RESIDUAL_DEBT_NOT_REPAIRED_HERE`
- `structureLock.certifiedModules` now `[Order, Cart, StoreContext]`

StoreContext was **not** added to the HTTP-owning `completeReferenceModules` list, which would
misrepresent applicability. Order and Cart certifications are preserved.

## 13. Architecture guards

`src/backend/Host/Tooba.Host.Tests/Architecture/`:

- `HostCartResidualGuardTests.StoreContext_is_golden_certified_with_default_currency_semantics`
  - no `string? Currency` / `AllowedCurrencies` / `SettlementCurrency` / `PaymentCurrency`
  - no StoreContext Application/Endpoints project, no MediatR reference
  - Contracts root zero `.cs`; Infrastructure root only `StoreContextModule.cs`
  - path↔namespace alignment (`...Contracts.Current`, `...Infrastructure.Current`)
  - manifest + SoT declare StoreContext, `PLATFORM_CONTEXT_REFERENCE_PATTERN`, `INTERNAL_ONLY`,
    `NOT_APPLICABLE_NO_APPLICATION_USE_CASE`
- `HostCartResidualGuardTests.Host_configuration_uses_canonical_store_commerce_default_currency_key`
  - `DefaultCurrency` property present, `Currency` gone
  - `StoreCommerce:DefaultCurrency` present, `StoreCommerce:Currency` gone
  - dev appsettings uses `DefaultCurrency`
- `HostCartResidualGuardTests.StoreContext_owns_effective_store_commerce_and_BuildingBlocks_does_not`
  - BuildingBlocks still contains no `StoreCommerceContext`
  - Cart infra references `Tooba.StoreContext.Contracts` and no `Tooba.Host`
- `Cart_owns_no_commerce_policy_default_and_consumes_platform_authority`
  - resolver reads `store.DefaultCurrency`, not `store.Currency`
  - Cart owns no `DefaultMarket`/`DefaultSalesChannel`/`CartCommerceDefaultsOptions`
- `TmarCompleteReferenceStructureGateTests` certifies the new module's root allowlists and
  path↔namespace alignment; `certifiedModules` expects `[Cart, Order, StoreContext]`

## 14. Validation results

- StoreContext structure/golden guards — PASS
- `PlatformOptionsValidatorTests` — PASS
- `HostCartResidualGuardTests` — PASS
- Cart focused create/commerce tests — PASS
- `TmarCompleteReferenceStructureGateTests` — PASS
- `TmarDurableGuardTests` — PASS
- `dotnet build src/backend/Tooba.slnx` — 0 errors

## 15. Recovery SoT

On PASS:

- StoreContext = `PLATFORM_CONTEXT_REFERENCE_PATTERN`
- StoreContext = `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED
- Cart remains `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED
- Order remains `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`
- `frontendFrozen = true`
- `nextTask = USER_REVIEW_STORECONTEXT_GOLDEN_001`

Cart multi-currency repair is not started automatically.
