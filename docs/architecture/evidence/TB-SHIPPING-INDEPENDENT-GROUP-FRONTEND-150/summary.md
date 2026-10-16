# TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — evidence

Parent: TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149 (`90e2dc166efac312af7a73f4485f39d6a702e9e3`)
Base: `0fde15c5e5d7d223ae4a8825ccb49f8fd17ccff3` (branch `main`, `HEAD == origin/main` at claim)
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_REAL_STOREFRONT_GROUP_CARDS_AND_SCHEDULE
Status: **PARTIAL** (implementation is real and complete for the frontend scope; the live authenticated
HTTP write/read roundtrip is UNVERIFIED — see §5)

## 1. What changed

### 1.1 Independent per-group schedule state (frontend, new pure module)

`src/frontend/app/storefront/storefront-shipping-group-schedule.ts` (new)
- `resolveGroupSchedules({ isGroupedCart, projection, groups })` — maps each **client group**
  (real cart seller × canonical method) to its own server `shippingGroupHorizons` row. A group without
  a server row becomes `resolved: false` (no guessed date, no carrier slot invention).
- `findGroupHorizon` — matching is normalized (seller id without hyphens/lowercased, method code trimmed
  lowercased); the client never fabricates a group identity.
- `pickGroupDeliveryDate` / `pickGroupDeliveryWindow` — an invalid value clears **only that group**
  (falls back to that group's own first server date).
- `mergeGroupSelections` — seeds each group from its own persisted server selection; a disappeared group is
  removed without transferring its selection to another group.
- `isGroupScheduleComplete` / `allGroupSchedulesComplete` — Continue/payment gating.

### 1.2 Independent card per real shipping group (frontend, new component)

`src/frontend/app/storefront/storefront-shipping-group-cards.tsx` (new)
- One card per real group (seller × canonical method) containing seller name, member lines + quantities,
  canonical service label + code, that group's server fee, "اولین نوبت ممکن" (that group's own minimum),
  date chips and time-window picker **inside the card**
  (`data-testid="shipping-group-{identity}"`, `shipping-group-date-{identity}-{value}`,
  `shipping-group-window-{identity}-{value}`, pending state `shipping-group-schedule-pending-{identity}`).
- Lines without a method stay in the seller's method-choosing block (previous behavior); no seller merging.
- No page-wide date/slot picker; no fetch in the component.

Split-out support components (extraction only, behavior/visuals preserved):
`storefront-shipping-single-group-section.tsx`, `storefront-shipping-delivery-card.tsx`,
`storefront-shipping-address-cards.tsx`.

### 1.3 Shipping surface rewiring

`src/frontend/app/storefront/storefront-shipping.tsx`
- Global `deliveryDate`/`deliveryTime` are used **only** for the one-group legacy cart; a multi-line cart uses
  `groupSelections: Record<groupKey, { deliveryDate, deliveryTime }>`.
- `isValid` for a grouped cart requires `allGroupsHaveMethod && allGroupSchedulesComplete(...)`; the
  single-line cart keeps the previous scalar + `isShippingDateHorizonComplete` path.
- Write payload: `shippingGroups[]` carries `selectedDeliveryDate` / `selectedDeliveryTimeWindow`
  **per group**; no global date is copied onto groups. Legacy scalars are sent only for the genuine
  one-group cart. Still a draft save — no `/shipping/commit`, no order/reservation on Continue.
- Size guard respected: `storefront-shipping.tsx` 945 (baseline) → **816** physical LOC; no new file above
  the 800-LOC threshold.

### 1.4 Removed client-side intersection semantics

- Deleted `use-storefront-group-shipping-horizon.ts` (client multi-request intersection hook).
- `storefront-shipping-group-horizon.ts` is now single-group only (`resolveSingleGroupHorizon`,
  `pickEligibleDeliveryDate`, `pickEligibleDeliveryWindow`, `isShippingDateHorizonComplete`).
- No `deriveGroupDateHorizon` / `distinctSelectedMethodCodes` / `useGroupShippingDateHorizon` remains in the
  shipping surface (asserted by test).

### 1.5 Additive, owner-correct read projection (backend) — used because the pre-save draft has no group id

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`
- `StorefrontShippingGroupHorizonView` gains `string GroupKey` (`{sellerPartyId:N}|{methodCode lowercased}`)
  and `ShippingGroupId` becomes `Guid?` (null before the first draft save).

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
- `BuildGroupHorizons(cart, methods, draft, today)` emits one row per (cart seller × eligible method):
  `minimum = today + sellerPreparationDays(that seller) + method.LeadDays`, that group's own eligible dates
  and windows; `ShippingGroupId` + persisted selections are attached only when a draft group with the same
  seller/method exists. No intersection, no global MAX. For a single-line cart no horizon rows are emitted,
  so the legacy one-group path is byte-for-byte unchanged.

## 2. Focused tests (raw output in this directory)

| evidence file | command | result |
| --- | --- | --- |
| `focused-shipping-tests-150.txt` | `node --experimental-strip-types --test app/storefront/storefront-shipping-api.test.ts app/storefront/storefront-shipping-group-schedule.test.ts app/storefront/storefront-shipping-group-horizon.test.ts app/storefront/storefront-shipping-item-groups.test.ts app/storefront/storefront-final-click-api.test.ts` | **39 tests / 39 pass / 0 fail** |
| `storefront-suite-150.txt` | `node --experimental-strip-types --test app/storefront/*.test.ts` | 148 tests / 145 pass / **3 pre-existing failures** (see §4) |
| `typecheck-150.txt` | `npx tsc --noEmit` | exit 0, **0 diagnostics** (empty file) |
| `architecture-guards-150.txt` | source-size / import-boundary / flat-folder / feature-boundary / test-discovery guards | 8 tests / 7 pass / 1 fail = FE-SIZE with **4 pre-existing** `OVERSIZED_GROWTH` files, none of them this diff |
| `critical-storefront-150.txt` | `npm run test:critical-storefront` | 39 tests / 38 pass / 1 pre-existing failure (raw color in `storefront-landing-blocks.tsx:286`) |
| `backend-order-tests-150.txt` | `dotnet test Modules/Order/Tooba.Order.Tests` | **354 passed / 0 failed** |

New test coverage in `storefront-shipping-group-schedule.test.ts` (7 tests):
1. two sellers with the **same** `tipax:standard` and different preparation → two independent horizons and
   two different "earliest possible" dates (2026-10-21 vs 2026-10-22), no global MAX/intersection;
2. a group without a server row stays unresolved, no guessed date, Continue stays false;
3. selection independence: changing group A changes no field of group B; an invalid A clears only A; a
   removed group is dropped without transferring its selection;
4. refresh/back restoration from that group's own persisted server selection + targeted clearing;
5. contract mapping of `shippingGroupHorizons` (camelCase + PascalCase, row without key/method ignored);
6. normalized seller/method matching (hyphen/case insensitive);
7. single-group path unchanged + surface wiring assertions (per-group cards, Continue condition, per-group
   payload, no `/shipping/commit`, no removed intersection helpers).

## 3. Backend contract actually consumed

`StorefrontShippingProjection.shippingGroupHorizons[]` →
`{ groupKey, shippingGroupId, sellerPartyId, methodCode, minimumDeliveryDate, eligibleDates[],
eligibleTimeWindows[], selectedDeliveryDate, selectedDeliveryTimeWindow, leadDays, sellerPreparationDays }`

Write side: `shippingGroups[] → { shippingMethodCode, cartLineIds, serviceCode, optionCode,
selectedDeliveryDate, selectedDeliveryTimeWindow }`; TB-149 validates every group against **its own**
minimum and its own allowed windows and rejects a multi-group write whose group schedule is missing.

## 4. Pre-existing, unrelated reds (recorded once, not chased)

- `storefront-product-card-skin.guard.test.ts` — asserts `StorefrontProductCardView` in
  `app/storefront/storefront-home.tsx` (file not touched by this diff).
- `storefront-token-completion.test.ts` — semantic primary tokens missing in the same home surface.
- `storefront-ssr-perf-r9r1.guard.test.ts` — `ENOENT`
  `src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs` (path removed by earlier Host evacuation).
- `component-compliance.guard.test.ts` — raw `bg-white` at `app/storefront/storefront-landing-blocks.tsx:286`.
- FE-SIZE `OVERSIZED_GROWTH`: `category-admin-screen.tsx`, `landing-pages/admin-landing-page-composer.tsx`,
  `storefront-landing-blocks.tsx`, `lib/storefront-composition/shared-composition-renderer.tsx`
  (none modified here).

## 5. Read-only smoke and the UNVERIFIED live roundtrip

`readonly-smoke-150.txt` + `shipping-150-readonly-smoke.png` + `shipping-ssr-150.html`:
GET `http://localhost:3000/fa/shipping` → 200, SSR shell renders (route compiles from this working tree, no
hang), and a read-only browser navigation shows the shipping surface with an unauthenticated header (`ورود`).

Consequence: the **authenticated multi-group UI and its live draft write/re-read were not executed
end-to-end**, because no customer session exists in the browser and no credentials/mocks were invented.
The live HTTP write/read roundtrip is therefore **UNVERIFIED**, not PASS. Grouped behavior is proven at the
deterministic level (focused frontend tests + backend per-group contract tests), and the user-visible proof
is the real rendered route, not a mockup.

## 6. Limitations / next bounded step

- No Admin-managed calendar/capacity/timezone/working-day authority exists (TB-145 §1.7): per-group promises
  are `sellerPreparationDays(seller) + method.LeadDays` + the existing static generic windows; no guaranteed
  carrier slot is promised or invented.
- Case D (same seller + same method + incompatible promise) remains untouched per TB-149 (unique index and
  `DuplicateSellerMethod` unchanged).
- Next single bounded step: one architect-authorized authenticated scratch roundtrip (isolated writable
  environment with a real two-seller cart) to convert the per-group write/re-read from UNVERIFIED to PASS.
