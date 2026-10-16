TB-SHIPPING-ONE-LEVEL-TABS-131 — EVIDENCE
Status: PASS (implementation + static validation); runtime browser acceptance DEFERRED
Base-SHA: 010a7d18a4e04397685e1ffea2578df54fdb4711
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE / Mode: BOUNDED_GROUPED_CHECKOUT_UI_AND_PAYMENT_DRAFT_DELIVERY

CHANGED SOURCE FILES (4 — `git diff --stat`):
  src/frontend/app/storefront/storefront-shipping-api.ts      +66
  src/frontend/app/storefront/storefront-shipping.tsx         +190 / -8
  src/frontend/app/storefront/storefront-checkout-api.ts      +40
  src/frontend/app/storefront/storefront-checkout.tsx         +37
  ------------------------------------------------ 325 insertions, 8 deletions

PRESERVED (unstaged / untouched): task receipt docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-131.task.md,
  pre-existing dirty src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx,
  pre-existing dirty src/frontend/app/admin/admin-order-detail-screen.tsx,
  pre-existing dirty docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-0{75,79}/*.txt.
  No reset / stash / rebase / force-push / shared-environment mutation.

API PROVENANCE (no new contract invented — existing Order-owned projections consumed):
  - POST /v1/storefront/shipping/projection -> draft.shippingGroups[] (StorefrontShippingGroupView)
    => mapped to StorefrontShippingGroupView in storefront-shipping-api.ts (shippingGroupId, sellerPartyId,
       methodCode, serviceCode, optionCode, methodLabel, shippingAmount, currency, minimumDeliveryDate,
       selectedDeliveryDate, selectedDeliveryTimeWindow, leadDays, lineCount).
  - PUT /v1/storefront/shipping/selection -> shippingGroups[] {shippingMethodCode, cartLineIds, serviceCode, optionCode}
    (StorefrontShippingGroupSelection) sent only when the cart has >1 authoritative seller group; otherwise the
    legacy scalar shippingMethodCode path is sent unchanged.
  - server-authoritative amounts: every rendered fee comes from projection.methods[].priceAmount; the client
    performs no fee/tax arithmetic beyond summing already-authoritative per-group prices for the summary row.
  - GET /v1/storefront/cart -> StorefrontCartLine.lineId / sellerPartyId / sellerDisplayName / title /
    mediaAssetId / quantity used verbatim as group membership identity.

UI PROVENANCE (data-testids added; existing testids for single-group carts unchanged):
  shipping page:  shipping-groups, shipping-group-<sellerPartyId>, shipping-group-line-<lineId>,
                  shipping-group-method-<sellerPartyId>-<methodCode>, shipping-group-fees
  checkout page:  checkout-shipping-groups, checkout-shipping-group-fees
  Legacy single-group DOM retained exactly: shipping-methods, shipping-method-<code>, shipping-summary,
                  shipping-continue, checkout-shipping-method, checkout-summary, checkout-submit.

VISUAL NON-REGRESSION:
  - No global CSS, token, theme, typography, radius/shadow or breakpoint change.
  - Only existing storefront classes reused (bg-surface, bg-primary/5, border-primary, border-gray-*,
    rounded-2xl, text-primary, shadow-sm, gray-50/60 surfaces, RTL grid layout).
  - Grouped card/list rows are additive and render only for multi-seller carts; single-seller carts render the
    previous DOM path unchanged (guarded by isGroupedCart).
  - Admin UI not touched.
  - Intentional additive change disclosed: checkout summary now includes a "هزینه ارسال" row and per-group
    fee rows (previously shipping was absent from the payment summary).

FOCUSED TESTS (Mission B — one initial run, one targeted rerun after a concrete failure; no broad sweeps):
  1. `npm run typecheck` (frontend, tsc --noEmit) ................................ PASS (clean output)
  2. `npm run test:storefront` ................................................... 76/76 PASS, 0 fail
       (includes storefront-shipping-api.test.ts, storefront-checkout-api.test.ts, cart/checkout/payment
        suites and storefront-cart-ui.guard.test.ts)
  3. `npm run test:critical-storefront` ......................................... FAIL at test:surface-coverage (1/20)
       home = PASS, pdp-guard = PASS, listing-guard = PASS, category-plp-guard = PASS,
       surface-coverage = 19/20.
       Sole failure is PRE-EXISTING and UNRELATED:
         component-compliance.guard.test.ts -> "customer-facing components do not introduce structural raw colors"
         actual = ['app/storefront/storefront-landing-blocks.tsx:286 bg-white']
       Proof it is not from this task: storefront-landing-blocks.tsx has no working-tree modification
       (`git status` clean, `git diff HEAD` empty) and `git show HEAD:<file>` line 286 already contains
       `bg-white/70`. None of the 4 files changed by TB-131 appears in the guard's failure list.

KNOWN GAPS / BLOCKERS (fail-closed, not simulated):
  - No browser/runtime acceptance executed (implementation finished after the minute-32 gate; the optional
    isolated browser proof was deferred, NOT faked).
  - projection.methods[] is destination-scoped, not seller-scoped: per-seller eligible-method partitioning was
    not proven at runtime; server planner still rejects an ineligible per-group method (fail-closed).
  - TB-124/125 tax-applicable fixture / payment-draft round trip not exercised (checkout.tax.unavailable
    behaviour unchanged).
  - No order/reservation mutation introduced by navigation; continue still saves draft only.

NEXT AUTHORIZED STEP: isolated multi-seller runtime acceptance checklist in
  docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-131.md ("Readiness checklist").
