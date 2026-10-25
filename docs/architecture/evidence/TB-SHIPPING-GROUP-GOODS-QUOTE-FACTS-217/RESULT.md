# TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217 — Result

Task: add ONE read-only internal Order edge exposing real per-shipping-group goods money
(**after discount AND after tax**) for the future Shipping captured-quote consumer.
Verdict: `STOP_FOR_ARCHITECT` — financial authority only; **not wired**, no publication/activation.

## What was added (exact files)

| File | Change |
| --- | --- |
| `Modules/Order/Tooba.Order.Application/Checkout/Contracts/CheckoutShippingGroupGoodsFactsContracts.cs` | NEW: `ICheckoutShippingGroupGoodsFactsReader`, `CheckoutShippingGroupGoodsFactsRequest`, `CheckoutShippingGroupMembership`, `CheckoutShippingGroupGoodsContribution`, `CheckoutShippingGroupGoodsFacts`, `CheckoutShippingGroupGoodsFactsResult` (Contracts-only types, no Domain/EF leak). |
| `Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` | `PreviewAsync` split into two internal pure-read steps: `LoadPreviewCartAsync` (guard/access/status/empty) and `QuotePreviewSnapshotAsync` (ONE authoritative whole-cart quote). Public signature/return/legacy behavior unchanged (still no persistence). |
| `Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.ShippingGroupGoods.cs` | NEW partial: implements the reader on the **same** directory. Public stable codes `checkout.shipping_groups.invalid`, `checkout.shipping_groups.amount_invalid`. |
| `Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | NEW scoped registration: `ICheckoutShippingGroupGoodsFactsReader` → `GetRequiredService<ICheckoutDirectory>()` (same scoped instance, no second `CheckoutDirectory`/`DbContext`). |
| `Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj` | ProjectReference to `Tooba.Order.Infrastructure` (needed to use the REAL directory). No package/reference to any new external package. |
| `Modules/Order/Tooba.Order.Tests/Checkout/ShippingGroupGoodsFactsHarness.cs` | NEW: controlled Cart/Offer/Pricing/Promotion/Tax boundaries with real `CheckoutDirectory`; no-call stubs for inventory/cart-conversion/cancel gate; providerless real `OrderDbContext`. |
| `Modules/Order/Tooba.Order.Tests/Checkout/ShippingGroupGoodsFactsTests.cs` | NEW: 9 focused facts. |

## Behavior implemented

- **Exact server-resolved identity:** cart read through `ICartQueryGateway` (access) + guard, status `Active`
  and non-empty checked **before** Pricing/Promotion/Tax; `ExpectedCartVersion` must equal the real
  `CartSnapshot.Version` (stale → existing `PRICE_CHANGED` convention).
- **Membership is validated, not trusted:** every group has a non-blank, trimmed, unique `GroupKey`;
  `CartLineIds` must be non-empty, known, unique across the whole request, and one seller per group.
  The union must equal the current cart lines exactly (`count == cart.Lines.Count`). Unknown/duplicate/
  shared/omitted/foreign lines, empty group, duplicate key, mixed seller → `checkout.shipping_groups.invalid`
  **before any quote call**. No silent truncation to a smaller list.
- **ONE whole-cart quote:** the reader reuses the existing `QuotePreviewSnapshotAsync` core
  (Pricing → Promotion → Tax per line for the whole cart) with `ShippingAmount = 0` and
  `ShippingGroups = null`, so no shipping price enters goods facts and no selected-group persistence is
  provoked. No `StorefrontShippingService`/Shipping call → no price recursion, no default shipping charge.
- **Exact join:** contributions are joined through the existing copied
  `_orderLineIdByCartLineId` map, never `OfferId`/quantity matching. Two cart lines sharing an `OfferId`
  keep separate identities and values. Missing/duplicate/contradictory mapping fails closed.
- **No re-derivation:** `FinalGoodsAmountAfterDiscountAndTax` = Σ `TaxInclusiveSnapshot` of the EXACT
  members (that value already contains the line's real discount and tax). No tax added twice, no
  discount subtracted twice, no pro-rating of the whole-cart grand total, no rounding policy.
- **Currency:** sole cart currency (`StorefrontCartCurrencyCompatibility`) reused; snapshot currency must
  match; per-line currency must match; mixed/missing → fail closed (mixed cart ⇒ existing
  `StorefrontOrderException`). Negative authored amounts → `checkout.shipping_groups.amount_invalid`.
  Explicit zero-value goods stay valid (no fake non-zero).
- **Physical facts stay unknown:** `GroupWeightGrams` and `PhysicalItemCount` are `null`; `QuantityTotal`
  is reported as decimal quantity metadata only and is never cast to Shipping item count (no ceil/truncate,
  no line-count inference, no hardcoded unit).
- **Read-only:** no `SaveChanges`, no inventory reservation/release, no cart conversion, no payment call;
  `OrderDbContext` stays providerless in the harness, so any real DB access would throw.

## Executed evidence

- Build: `dotnet build src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj` → **0 errors**.
- Focused new facts: `--filter FullyQualifiedName~ShippingGroupGoodsFacts` → **9/9 passed**:
  1. two groups, same seller, 100−10+9 = **99** and 200−20+18 = **198** (not 297);
  2. two cart lines sharing `OfferId` keep separate contributions/order-line ids (1 vs 3 qty → 99 / 319);
  3. access denied / expired / empty / stale version / missing line / shared line / duplicate key /
     empty group / unknown line / blank key / untrimmed key / no groups — all fail **with 0** Pricing,
     Promotion, Tax calls;
  4. group mixing two sellers fails closed before any quote;
  5. changed coupon + tax rate reflected on the next read (no cache), caller cancellation token observed
     by both Promotion and Tax ports;
  6. foreign failure propagates (`TAX_CALCULATION_ERROR`) and a negative authored tax result → amount_invalid;
  7. mixed cart currency fails closed;
  8. no DB access/persistence, no reservation/conversion/cancel-gate call, exact foreign call counts;
  9. module DI: one scoped `ICheckoutShippingGroupGoodsFactsReader` descriptor, resolves to the **same**
     instance as `ICheckoutDirectory` in a scope and a different one in another scope.
- Narrow affected subset retained: `FullyQualifiedName~Order.Tests.Storefront|~OrderInfrastructureOrganizationGuard|~OrderInfrastructureForeignLayerBoundaryGuard`
  → **194/194 passed** (legacy preview/storefront behavior + infra organization/boundary guards).

## Worked example (exact)

Prices exclusive, 10% tax, per-line promotions: `100 − 10 = 90`, `+9` tax ⇒ **99.00**;
`200 − 20 = 180`, `+18` tax ⇒ **198.00**. Reader returns two group facts 99.00 and 198.00;
whole-cart grand total (297.00) is never read and never divided.

## Truthful limits

- **Financial authority only.** Physical weight and physical item count have **no authoritative source in
  this slice** and are reported `null`. No measurement framework was added.
- **NOT WIRED.** Nothing calls this reader yet: no Shipping captured-quote consumer, no published-revision
  selection/activation, no fulfillment owner/type/membership decision, no projection/selection/commit.
  `Storefront`/checkout runtime behavior is unchanged (gate still default-OFF).
- `CouponCode` from the command is the same input the existing preview uses; this slice adds no new coupon
  or tax policy.
- Cart-level membership validation is server-validated against the real cart, but the group **keys** are
  still caller-declared labels (data identity, not permission).
- Guard hook is the existing `IOrderUseCaseGuard` stub; no new actor/permission matrix was introduced.
- No HTTP route, no MediatR command, no foreign-module Contracts, no schema/migration, no seed, no user
  draft write, no frontend/flag change, no shared DB, no Host/FE restart.

## Compliance

- No `git add -A`; only the exact paths above are staged/committed.
- TB185 paused, default-OFF, PendingAllocation unchanged; §29 decisions, 211 FE, 213 calendar, 214 capture,
  215 quote and 216 Contracts edge were not replayed or re-tested.
- Actual elapsed exceeded the 25-minute hard box (code + two compile/fact correction rounds) — recorded
  honestly, not graded as process PASS.
