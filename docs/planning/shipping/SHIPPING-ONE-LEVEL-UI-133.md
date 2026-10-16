# SHIPPING-ONE-LEVEL-UI-133 — grouped delivery-date re-projection repair

Task: `TB-SHIPPING-ONE-LEVEL-TABS-133` (parent `TB-SHIPPING-ONE-LEVEL-TABS-132`)
Base SHA: `4da88089ab10430d3f8360722c573fd49322c3e7` (HEAD == origin/main at claim; no drift)
Mode: `BOUNDED_GROUPED_DELIVERY_DATE_REPROJECTION_REPAIR` · Stop state: `STOP_FOR_ARCHITECT`

## Defect (proved by TB-132)

For a multi-seller cart the grouped method picker only changed local state, so no projection was ever requested
with a `methodCode`. The server only derives the delivery horizon when a `methodCode` is supplied
(`POST /v1/storefront/shipping/projection` without one returns `deliveryDates = 0`), therefore the delivery-date
control stayed empty, `isValid` stayed `false`, "ادامه به پرداخت" never enabled and payment was unreachable.

## Repair (frontend only, smallest surface)

1. `src/frontend/app/storefront/storefront-shipping-group-horizon.ts` (new, pure — no fetch, no React, no `Date`):
   - `deriveGroupDateHorizon()` — **intersection** of the server-returned allowed delivery dates and time windows
     across all distinct selected methods; shared minimum = **latest** server-declared minimum; the earliest
     surviving date is re-flagged as "نزدیک‌ترین". Empty intersection ⇒ empty horizon (no fabricated dates).
   - `distinctSelectedMethodCodes()`, `resolveShippingDateHorizon()`, `pickEligibleDeliveryDate()`,
     `pickEligibleDeliveryWindow()`, `isShippingDateHorizonComplete()`, `createLatestResponseGate()`.
2. `src/frontend/app/storefront/use-storefront-group-shipping-horizon.ts` (new hook) — grouped carts only:
   one `POST /v1/storefront/shipping/projection` **per distinct selected method** (deduplicated, `allSettled`),
   with a latest-wins token so a stale response can never overwrite a newer selection. Read-only.
3. `src/frontend/app/storefront/storefront-shipping.tsx` (minimal plumbing) — the grouped method click now feeds
   the hook; the delivery date/time controls, the "اولین نوبت ممکن" line and `isValid` read the derived
   `dateHorizon`; a chosen date/time is invalidated only when it is no longer in the server-returned shared set.

Unchanged: CSS/themes/tokens, typography, layout, spacing, RTL, breakpoints, group markup, the single-seller
legacy path (it still renders `projection.deliveryDates/minimumDeliveryDate/deliveryTimeWindows`), the
Order-owned shipping projection/selection contracts, and all navigation semantics (still exactly one
`PUT /v1/storefront/shipping/selection` on Continue, then `router.push` to `/payment`).

## Verification

Focused tests (`app/storefront/storefront-shipping-group-horizon.test.ts`, 7/7 PASS): shared-date intersection
across two distinct methods, empty-intersection safety, no input mutation/no fabricated dates, single-seller
horizon identity, invalid-selection invalidation, stale-response gate, and source-level wiring guards
(`shipping/commit` still absent). `npm run typecheck` PASS. Architecture guards
`frontend-import-boundary` / `frontend-flat-folder` / `frontend-feature-boundary` PASS (17/17).
Sibling `storefront-shipping-api.test.ts` + `storefront-shipping-destination-model.test.ts` PASS.

Runtime acceptance on the isolated scratch target (`:5403` Host + `:3103` frontend, disposable Postgres on
`:55456`, `tenant-133-scratch`): real two-seller guest cart (`itemCount=2`, `sellerCount=2`, subtotal `3,640,000`)
⇒ after choosing `post:express` + `tipax:standard`, the page rendered **6** server-derived shared date chips
starting `2026-10-20` (server counterfactuals: `post:express → 2026-10-19…`, `tipax:standard → 2026-10-20…`, so
`2026-10-19` is correctly excluded), the notice disappeared, and Continue became **enabled** once a time window
was chosen. One Continue click navigated to `/fa/payment?cartId=…` and the payment summary showed receiver
`رضا محمدی — تهران`, shipping `۳۸۰٬۰۰۰ ریال` (= 200,000 + 180,000 grouped fees) and payable `۴٬۳۴۷٬۶۰۰ ریال`
with "این سفارش هنوز ثبت نشده است" — i.e. the selection survived the handoff with no premature commit/order.
Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-133/` (observation, isolation guard, launch
records, api prerequisites, bounded screenshot, cleanup).

## Debt / first remaining blocker

- **Draft read-back on the shipping page is not restored on a cold reload** (empty form, no selected methods,
  0 date chips, revalidation notice "انتخاب ارسال دیگر معتبر نیست"). The projection read without a `methodCode`
  returns no draft object — the pre-existing Order-owned read contract, untouched by this repair (the repair only
  adds `methodCode` reads). The draft *did* persist (proved by the payment handoff). Smallest next step: an
  Order-owned decision on the projection draft read-back/revalidation contract, not a frontend workaround.
- `src/frontend/app/storefront/storefront-shipping.tsx` grew from 1209 → 1251 physical LOC. This file is already
  an `OVERSIZED_LEGACY` baseline violation (baseline 945) and the `frontend-source-size` guard already fails on
  `main` for 4 unrelated files; the +42 LOC is the minimal plumbing cost and is recorded here as debt. No baseline
  file was edited (baseline changes are not authorized by this task).

## Non-claims

Payment/checkout/production readiness is not claimed (no payment method chosen, no authorization, no capture, no
order). Tax figure is read from the payment summary only. Single-seller legacy was not re-run in the browser.
Visual acceptance is not claimed (one bounded screenshot). Single seller with two independently chosen methods
remains an outstanding acceptance case.
