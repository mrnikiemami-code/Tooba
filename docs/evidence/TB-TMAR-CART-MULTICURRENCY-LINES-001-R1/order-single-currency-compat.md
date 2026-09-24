# TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 — Order single-currency compatibility repair

Task: `TB-TMAR-CART-MULTICURRENCY-LINES-001-R1`
Parent: `TB-TMAR-CART-MULTICURRENCY-LINES-001`
Channel: `tooba-main` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`

## Parent verdict

Parent `TB-TMAR-CART-MULTICURRENCY-LINES-001` is **NOT YET ACCEPTED**.

The Architect verified two real semantic defects on `main` (commit `f0cf9afb9ef42d2d4c5bb5aa1313a0863cb4ad84`):

1. `StorefrontShippingService` computed `cart.TotalsByCurrency.Sum(...)`, which can add unlike currencies.
2. Order checkout paths used `cart.DefaultCurrency` as transaction/order/pricing currency even when the sole Cart line currency differed.

These were semantic defects, not mechanical compile errors. This task repairs them without implementing Order multi-currency.

## Closed decision implemented

- Order multi-currency remains **deferred**.
- Until its dedicated wave, the Order boundary proceeds **only** when all Cart lines share exactly one distinct non-empty `QuotedCurrency`.
- That sole line currency is the effective Order/Checkout currency.
- `Cart.DefaultCurrency` is **never** Order transaction authority.
- A mixed-currency or currency-less Cart fails closed **before** shipping arithmetic, repricing, reservation, order persistence or any payment-facing flow.
- No FX, split-order or payment-group design was introduced.
- Stable error: `checkout.multicurrency.not_supported`, surfaced through the existing typed `StorefrontOrderException` / `StorefrontOrderErrors`.

## Typed error

`StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported = "checkout.multicurrency.not_supported"`.

Registered in the explicit Order error catalog (`OrderErrorCatalogContributor`) as `Business` / `400`, with matching `OrderErrors.resx` and `OrderErrors.fa.resx` entries. No message parsing and no localized prose in Domain/Application.

## Order-owned compatibility helper

New cohesive helper under the existing Order Storefront capability:

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontCartCurrencyCompatibility.cs`

Responsibilities only:

- inspect `CartPage` line `Currency` and `CartSnapshot` line `QuotedCurrency`;
- require non-empty line currency;
- require exactly one distinct currency using **ordinal** comparison;
- return the sole currency;
- `ResolveSoleCurrencyAndSubtotal(CartPage)` additionally reads **only** the matching `TotalsByCurrency` entry and fails closed if any totals entry is in another currency;
- mixed/missing ⇒ typed `StorefrontOrderException`;
- **never** falls back to `DefaultCurrency`.

No shipping, pricing, reservation or payment policy lives in the helper. No `DefaultCurrency` reference survives in its code (documentation only).

## Shipping

`StorefrontShippingService`:

- removed `CartSubtotalExclusiveOfTax` and its `TotalsByCurrency.Sum(...)` arithmetic;
- `ProjectAsync` resolves the sole line currency and the matching per-currency subtotal once, before rate arithmetic, and uses them for `LoadEligibleMethodsAsync` and `StorefrontShippingProjection`;
- `SaveSelectionAsync` revalidates using the same compatibility rule;
- `StorefrontShippingProjection.Currency` is the sole line currency, never `DefaultCurrency`.

`StorefrontShippingProjection` may remain single-currency only because mixed carts are rejected upstream.

## Storefront checkout

`StorefrontCheckoutService.RequireCartAsync` now calls `StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart)`, so **preview and submit** reject mixed/missing currency before Order checkout economics are entered. `DefaultCurrency` is never treated as transaction currency.

## CheckoutDirectory

`QuoteSellerOrdersAsync` resolves the sole line currency once (fail closed on mixed/missing) and uses it for:

- `FinancialRounder.MoneyPlaces(effectiveCurrency)`;
- the campaign/base Pricing selector passed into `ResolveCheckoutLineQuoteAsync` (and therefore into `TryResolveEligibleCampaignPriceAsync` / `PriceResolutionQuery`);
- `SellerOrder.Open(..., effectiveCurrency, ...)`.

`PreviewAsync` uses the same rule for `CheckoutGroup.Submit(..., effectiveCurrency, ...)`. Transaction use of `cart.DefaultCurrency` is removed.

## CheckoutSubmitHost

`PersistCheckoutAsync` reuses the same compatibility helper (no duplication) for the persisted `CheckoutGroup` currency. `cart.DefaultCurrency` is no longer used.

## Cart preserved

Untouched: optional requested add-line currency, `ShoppingCart.DefaultCurrency`, `CartLine.QuotedCurrency` authority, `CartPage.TotalsByCurrency`, `CartSnapshot.DefaultCurrency`, the `currency` DB column/schema/migrations, and Pricing contracts. Cart remains mixed-currency capable; only the Order boundary rejects mixed carts for now.

## Tests

New focused coverage: `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontCartCurrencyCompatibilityTests.cs`

- single-currency cart resolves the sole line currency, not `DefaultCurrency`;
- only the matching per-currency total is read;
- mixed currencies fail closed with the typed stable error;
- mixed currencies never produce a cross-currency subtotal;
- missing line currency fails closed without a default fallback;
- totals in another currency than the sole line currency fail closed;
- `CartSnapshot` sole currency comes from `QuotedCurrency`; mixed quoted currencies fail closed.

## Guards

Strengthened `OrderStorefrontArchitectureGuardTests` with `Order_boundary_is_single_currency_fail_closed_and_never_uses_cart_default_currency`:

- no `TotalsByCurrency.Sum` in Order shipping;
- no `cart.DefaultCurrency` in Order shipping/checkout/persistence code;
- sole-line-currency resolution is the Order currency authority;
- the typed `checkout.multicurrency.not_supported` fail-closed exists and the helper never touches `DefaultCurrency`;
- no Host business implementation was added.

## Validation

- `dotnet build src/backend/Tooba.slnx` — 0 errors.
- Focused Order tests (`StorefrontCartCurrencyCompatibilityTests` + `OrderStorefrontArchitectureGuardTests` + `OrderEndpointPresentationTests`) — 23 passed, 0 failed.
- Full `Tooba.Order.Tests` — 132 passed, 0 failed.
- Full `Tooba.Cart.Tests` — 23 passed, 0 failed.
- `HostCartResidualGuardTests` — 14 passed; `TmarCompleteReferenceStructureGateTests` — 3 passed; `TmarDurableGuardTests` — 5 passed.
- Focused Host Order/storefront integration selection (`Storefront*`, `OrderStorefront*`, `CheckoutOrderFoundation*`, `AtomicCheckout*`, `StorefrontShippingCalculator*`) — 71 passed, 3 skipped (Postgres-gated), 1 failure.
  - The single failure is `AtomicCheckoutCommitTests.Submit_uses_one_ambient_transaction_and_convert_before_complete`, which reads a **stale path** `src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutSubmitHost.cs`. That file lives at `Checkout/Persistence/CheckoutSubmitHost.cs` after the earlier `TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002` foldering task, so the assertion fails on pristine `HEAD` too (unrelated pre-existing test debt, not touched by this task).
- `TmarSourceSizeAndInfraAppTests` — 3 of 6 fail, **identically on pristine `HEAD`** (`f0cf9afb`) in a detached worktree, so they are pre-existing and not introduced here. This task still lowered its own `CheckoutDirectory.cs` deltas (net +9 LOC versus the pre-existing 929 reading, and no new growth beyond the pre-existing oversized file). Reported as residual defects only; no guard or baseline was modified.

## Recovery SoT

- Cart multi-currency line slice = `ACCEPTED_WITH_ORDER_SINGLE_CURRENCY_COMPATIBILITY_GUARD`
- Cart = `LINE_LEVEL_CURRENCY_AUTHORITY_WITH_DEFAULT_SELECTION_AND_TOTALS_BY_CURRENCY`
- Order multi-currency = `DEFERRED`
- Order boundary = `SINGLE_CURRENCY_ONLY_FAIL_CLOSED_UNTIL_DEDICATED_WAVE`
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`
- Cart / Order / StoreContext certifications unchanged
- `frontendFrozen = true`
- `nextTask = USER_REVIEW_CART_MULTICURRENCY_LINES_001_R1`
