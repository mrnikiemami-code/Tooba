# RESULT — TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Parent-Task: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Bridge-Task-Row: b2dc1b7e-317c-44c5-af54-a593ffb97d9e
Status: PARTIAL (honest: coupons proven live end-to-end; Marketplace edition live trial NOT executed)
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: bd3a6c077e0d74165a452f63e3f7783ca770e190
Base-SHA: bd3a6c077e0d74165a452f63e3f7783ca770e190

## 0. Preserved handoff (unchanged, never automated)

- User trial handed off in 237-R1 is untouched: FE `http://127.0.0.1:3100`, Host `127.0.0.1:5188`, dedicated
  Chrome profile `D:\temp\tb238-handoff-chrome` (CDP 9337) opened at `/fa/login`. Not enumerated, read,
  automated, reloaded or closed by this task. The original checkout `3000`/`5088` was likewise untouched.
- New work happened only in the isolated worktree `D:\temp\tb238` (branch `main` from the issuance commit) on
  free ports: SingleStore Host `127.0.0.1:5189`, FE `127.0.0.1:3101`.

## 1. Coupons — REAL live path (single store, existing UI/API, no fake port)

DEV coupon created through the actual seller/admin promotion API (not a fake promotion port):
seller actor `seller-actor-a@tooba.local`, seller party `01a030d1-40cb-7000-8abe-6d31739956c5`,
code `TB238-264452`, percentage 10, activated. Buyer cart is a real guest cart against the real SingleStore
`store-alpha` tenant; all calls are normal HTTP to the task Host, no `/v1` interception.

| stage | request | observed |
|---|---|---|
| read without coupon | `POST /v1/storefront/shipping/projection` | 200, group goods **9,485,771** IRR, payable 9,485,771 |
| read with valid coupon | same + `couponCode=TB238-264452` | 200, group goods **8,537,194** IRR (10% = 948,577 discount), freight 120,000 + packaging 5,000 |
| selection | `PUT /v1/storefront/shipping/selection` | 200, persisted draft, `selectionRevision` **1** |
| payment preview | `GET /v1/storefront/shipping/payment-draft` | 200, discount **870,254**, shipping 125,000, payable **8,662,194**, selectionRevision 1 |
| final click | `POST /v1/storefront/cart/{cartId}/start-payment` | 200, checkout `01a18165-04b7-7000-8764-09fb8c0bf4d4`, status **Pending** (manual), 1 reservation, amount 8,662,194 IRR |

- Invalid coupon: shipping read stays 200 (read is tolerant by design) and the **selection/commit** refuses with
  the typed code `checkout.coupon.not_applicable` (409, fa/en) — the discount never silently disappears.
- Explicit clear / another code: the response identity is now `destination + coupon intent`
  (`shippingResponseIdentityKey`), so clearing or changing the code makes the previous priced answer
  "pending", not "current": no card money/date and no selectable confirmation from the old coupon context, and
  the existing coordinator issues ONE new read for the new identity. Typed recipient/address is preserved
  (`preserveTypedAddress: true`).

## 2. Double click — measured, not assumed

- Two `start-payment` calls fired in the same tick for one cart both failed at the Inventory outbox write
  (`DbUpdateException` 22001 on `inventory.outbox_messages.correlation_id`, varchar(128)) and produced **no
  checkout and no second reservation**; a later single normal call created exactly ONE checkout
  (`01a18164-d2f1-…`) with one reservation, and the replay of that same cart returned the original checkout.
- Client fix (section 3A): `payInFlightRef` closed **before** the first `await` and released in `finally`, so a
  second click in the same tick can no longer send a second request at all. The server idempotency was not
  weakened, and no automatic retry was added.

## 3. Source changes (existing paths only)

A. `app/payment/storefront-payment-handoff.tsx` — synchronous `payInFlightRef` gate before the first `await`,
   released in `finally` (also on the success path that navigates away), so the busy render is no longer the only
   protection. The early-invalid-revision branch no longer needs its own `setPaying(false)`.
B. `app/storefront/storefront-shipping-destination-refresh.ts` + `storefront-shipping.tsx` — the coordinator now
   keys on the **response identity** (destination + coupon intent, `NO_DESTINATION_IDENTITY` for the empty
   destination so failed/Retry works without a destination) instead of the destination key alone;
   `capturedPreviewForIntent` is `null` unless `destinationPreviewMode === "current"`, so `isValid` and
   `buildCapturedSelectionIntent` can never send a stale priced context while the cards are pending/failed.
   Save refuses explicitly instead of falling back to the raw projection. Typed address is still never
   re-hydrated. No second fetch framework, no client repricing, no "missing ⇒ 0".
C. `app/payment/storefront-payment-handoff.tsx` — payment stepper only: `flex-wrap`, `min-w-0`,
   `text-[11px] sm:text-sm`, `px-2.5 sm:px-4`, `w-3.5 sm:w-4` icons and the chevron hidden below `sm`, so a
   360/390 viewport cannot be expanded by the stepper. No global CSS, no clipping, no `overflow:hidden` mask.

## 4. Viewport proof (measured + native captures)

Measured in the real page (`Runtime.evaluate`), and every image inspected with its real pixel size:

| requested | `window.innerWidth` | `documentElement.clientWidth` | `documentElement.scrollWidth` | `visualViewport.width` | capture |
|---|---|---|---|---|---|
| 1440 | 1440 | 1425 | 1425 | — | `screens/ship-1440.png` **1440×900** |
| 390 | 390 | 390 | 390 | 390 | `screens/ship-390.png` **390×844** |
| 360 | 360 | 360 | 360 | 360 | `screens/ship-360.png` **360×800** |

No horizontal scroll at any of the three widths; the shipping page renders one column at both mobile widths with
the real priced group card (freight 120,000 + packaging 5,000) and the per-day interval. The earlier invalid
"360 CSS = 378 image / 390 CSS = 393 innerWidth" condition reported in 237-R1 is not reproduced: the requested
width is exactly the measured `innerWidth` and the image bytes are exactly the requested size.

## 5. NOT delivered (prominent, no waiver)

- **Marketplace edition live trial is NOT executed.** There is no `3102/5190` Marketplace target, no
  Marketplace-owned Platform group with two real Seller business scopes on one binding, no merchant
  Manage-vs-View denial, and no Marketplace multi-group read → save → preview → final order in this run.
  Nothing in this Result may be read as Marketplace evidence; the edition claim stays OPEN.
- No Payment page native screenshot at 360/390 was captured (the payment stepper fix is proven by code +
  typecheck + the shipping-page measured captures only). The measured payment-page metrics are NOT claimed.
- No partial-applicability coupon case on a real mixed cart (single-seller cart only).
- No post-order publication change + Order GET immutability receipt was produced.
- Manual `Pending` only: no PSP, no real funds, no paid success, no Visual ACCEPT, no activation.

## 6. Checks / time / migrations

- `npx tsc --noEmit` — clean (0 errors) in `D:/temp/tb238/src/frontend`.
- `node --test app/storefront/storefront-shipping-destination-refresh.test.ts` **8/8**, including the new
  "coupon change or clear is a NEW response identity" fact; `app/payment/storefront-payment-page.test.ts` +
  `storefront-payment-grouped-summary.test.ts` **8/8**.
- `npm run test:critical-storefront` run ONCE: one pre-existing unrelated red
  (`lib/storefront-appearance/component-compliance.guard.test.ts` → `storefront-landing-blocks.tsx:286 bg-white`),
  a file this task never touched. Recorded, not repaired.
- No migration, schema, baseline or seed was added or applied. Applied state from 237-R1 (231/232/236) is
  unchanged. Checked-in gates stay OFF; only the named task processes run with preview/selection/checkout ON.
- Elapsed ≫ HARD40: the coupon path and the live single-store trial consumed the budget and the Marketplace
  setup was not started. Recorded as NONCOMPLIANT, not a process PASS. One concrete blocker: driving the real
  shipping form through the automation surface (controlled React inputs + custom comboboxes) repeatedly reset
  typed values, so the payment page (which needs a completed selection) could not be reached inside the budget.

## 7. Preservation

`231`/`232`/`236` migrations already applied survive untouched; the stale EF snapshot gap is retained; TB185
stays paused/default-OFF/PendingAllocation; the 237-R1 user window, its cart `01a1807a-e7ef-7000-82d1-285f4bd5c982`
and the `230` DEV publication/draft/rates/week/pointer were not modified. Known 227 mobile empty-note and the
225-R1 lost local edits remain as recorded.
