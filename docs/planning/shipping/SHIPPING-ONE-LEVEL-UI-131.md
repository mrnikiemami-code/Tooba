# TB-SHIPPING-ONE-LEVEL-TABS-131 — Grouped storefront Checkout/Payment presentation

Status: implementation landed, statically validated; runtime browser acceptance DEFERRED.
Base-SHA: 010a7d18a4e04397685e1ffea2578df54fdb4711 (HEAD == origin/main at start).
Date: 2026-10-16.
Scope: `src/frontend/app/storefront/storefront-shipping*.ts(x)` and `storefront-checkout*.ts(x)` only.

## What changed (customer-facing)

| Surface | Before | After |
| --- | --- | --- |
| `/fa/shipping` — items | Not shown at all; only a cart-wide method list | Multi-seller carts render one scoped card per authoritative `sellerPartyId` with the real cart line items (thumbnail, title, quantity) and an explicit method choice **per group** |
| `/fa/shipping` — submit | Legacy scalar `shippingMethodCode` only | Multi-group carts submit incremental `shippingGroups[]` with real `cartLineIds` + authoritative `serviceCode`; single-group carts keep the legacy scalar exactly as before |
| `/fa/shipping` — summary | One shipping row | Grouped carts additionally show one fee row per group (seller display name + authoritative method label + authoritative price) |
| `/fa/payment` (checkout) | One method label; no shipping row | Order-owned `shippingGroups` from the checkout projection are listed with localized method label, scheduled date and per-group fee; a shipping total row was added |
| Return/navigation | Draft restored as one method | Saved groups are restored into per-group selection state from `draft.shippingGroups` (no automatic conversion, no commit on navigation) |

No backend contract was added: `StorefrontShippingDraftView.ShippingGroups`,
`StorefrontShippingSelectionRequest.ShippingGroups` and `StorefrontCheckoutPage.ShippingGroups`
already existed in the Order-owned projections (TB-126/127/128). This task only consumed them.

## Authority and fail-closed properties

- Seller grouping and item membership come from the real cart projection (`StorefrontCartLine.sellerPartyId` / `lineId`), never from display labels or client-invented arrays.
- Method identity, localized label, lead days and **price** always come from the authoritative `projection.methods[]` (server-evaluated eligible methods); the client never computes a fee.
- Final group admissions are still decided server-side by `CartShippingGroupPlanner` (each cart line exactly once, eligible method per group, duplicate seller+method rejected) — a client mistake fails closed with `shipping.method.unavailable`.
- No carrier tariff, seller/marketplace share, subsidy, warehouse or commercial split was invented; Payment/Settlement `PendingAllocation` behaviour is untouched.
- The grouped card renders only when the cart has more than one seller group, so the single-seller legacy experience (including its DOM/testids) is byte-for-byte the previous behaviour.

## Validation performed

| Check | Command | Result |
| --- | --- | --- |
| Type safety | `npm run typecheck` (frontend) | PASS (clean) |
| Storefront suites | `npm run test:storefront` | 76/76 PASS |
| Critical storefront | `npm run test:critical-storefront` | home/pdp-guard/listing-guard/category-plp-guard PASS; `test:surface-coverage` 19/20 |

The single `test:surface-coverage` failure is **pre-existing and unrelated**: `component-compliance.guard.test.ts` flags `app/storefront/storefront-landing-blocks.tsx:286 bg-white/70` — a clean file at the base SHA (`git status` clean, line present at `HEAD`). None of the four files changed by this task appears in the failure list.

## Explicitly NOT proven (deferred)

1. **No browser/runtime acceptance was run.** The implementation finished after the minute-32 gate, so the optional isolated browser proof was deferred rather than faked. The multi-seller grouped render is therefore static/test-validated, not visually observed.
2. **No owner-scoped method eligibility per seller.** The projection's `methods[]` is destination-scoped, not seller-scoped; the server planner accepts a per-group method only if it is in that eligible list. Real multi-seller acceptance must confirm the intended method set per seller and that a seller with no eligible method fails with an actionable message.
3. **No tax/payment-draft fixture run.** The TB-124/125 tax-applicable scratch fixture was not exercised; `checkout.tax.unavailable` behaviour from TB-123 is unchanged.
4. **One intentional visible addition**: the payment summary now shows a "هزینه ارسال" row plus per-group fee rows. This is additive and reuses existing row styling, but it does change the checkout summary's appearance for identical orders.

## Readiness checklist for the next authorized task (runtime acceptance)

1. Isolated disposable scratch with the TB-124/125 tax-applicable fixture; do NOT touch `:5088`, `:3000` or `tooba_alpha`.
2. Build a cart with ≥2 sellers, and a seller with two eligible methods. Open `/fa/shipping`; confirm one card per seller group with correct member items/quantities and per-group fee from API.
3. Select different methods per group, continue to payment; confirm the checkout group list, per-group fee, group-sum shipping total and reconciled grand total, plus a refresh/back-nav round trip that preserves selections.
4. Compare shipping + payment surfaces on desktop and mobile against the pre-change screenshots for visual non-regression (desktop/mobile DOM + bounded screenshots only).
5. Confirm no order/reservation mutation happened before the final payment click.
