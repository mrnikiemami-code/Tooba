# TB-237 — RESULT — storefront connected practical trial

Status: **PARTIAL** (source connection landed; no live buyer trial executed).
Base (issue) `f9b873eee53f8f678c96d51d6b1236744afeb5d5` · isolated worktree `D:\temp\tb237`, branch `task237`.
Original watched checkout `D:\Users\User\source\repos\SarvNewVer` left on `6d820e9ae` — never enumerated, read,
automated, reloaded or closed; intentionally behind `origin/main` after this push.

## Honest timing

HARD45 = source 25 / real trial 15 / recovery 5. Elapsed ≈95 min → **NONCOMPLIANT** by the task's own limit.
The trial slice was not started because the two source-confirmed blockers plus the real frontend connection
consumed the box. This is recorded, not excused.

## A — Section 3 blocker 1: goods echo is now mandatory and exact (done)

`StorefrontCapturedSelectionWriter.AssertGoodsEcho` no longer returns early on blank/omitted. For **every**
selected group `ExpectedGoodsAmount` must be the canonical non-negative decimal **wire text**
(`^\d+(\.\d+)?$`, `"0"` lawful). Missing/blank/grouped/signed/exponent text ⇒ owning intent refusal
`storefront.captured_selection.intent_invalid` **before** any mutation; a real mismatch ⇒
`storefront.captured_selection.conflict`. Parsing uses `NumberStyles.AllowDecimalPoint` only; no value from the
client is stored, and the server's own composed goods remain the authority.

## B — Section 3 blocker 2: rejected coupon is surfaced from the whole cart (done)

`CheckoutDirectory` quote path now accumulates, across the **whole** cart, whether `Promotion`'s own
evaluation applied any coupon-backed promotion (`Applied[].CouponCode` non-blank). If a non-blank coupon intent
was supplied and **no** line applied a coupon promotion, the quote refuses
`checkout.coupon.not_applicable` (409, `ErrorClassification.Conflict`, fa/en resources) instead of returning a
silently zero-discount cart or an unrelated automatic promotion. Partial applicability stays lawful (one applied
coupon line is enough). Promotion owns code matching/eligibility; Order compares no coupon text, so no
case/length/matching policy was invented here. `DiscountAmount == 0` is never used as the signal. No new engine,
no second evaluator call.

## C — Frontend connection (done, typechecked, unit-proven; not exercised in a browser)

Existing page/route/components only (`storefront-shipping.tsx` composes,
`storefront-shipping-api.ts` + `storefront-checkout-api.ts` own network/strict decoding,
`storefront-shipping-group-cards.tsx` renders):

- Decoder retains the real per-method confirmation echoes (`confirmationSnapshotId`,
  `confirmationSourceRevision`, `confirmationSelectionVersion`, `confirmationAreaGroupId`,
  `confirmationRateRowId`) and the root `selectionRevision` as **exact text** via a new `asExactTokenOrNull`
  (a JSON number or non-string is "absent", never `String()`-coerced). Draft mapping keeps
  `selectionRevision`.
- Cards are no longer read-only local `useState`: method + allowed date are chosen against the **server
  `GroupKey`** and hoisted to the page (`StorefrontCapturedGroupChoice`, method keyed by canonical
  service/option **within that group**, never by label or fuzzy binding). Two owners sharing a binding remain
  independent.
- `storefront-shipping-captured-selection.ts` builds the captured intent **only** from the successful server
  response: exact goods text per group, exact echoes, the chosen date's own interval, real line ids; `null`
  (no request) whenever anything is missing, malformed or not in that method's calendar.
- `saveShippingSelection` now sends `capturedGroups` + `expectedSelectionRevision` (from the successful
  preview, else the stored draft revision, else `"0"`) + `couponCode`, and never mixes legacy `shippingGroups`
  with captured groups. Projection reads carry the same coupon intent.
- A context key (destination identity · coupon · cart version · stored selection revision) resets prior
  confirmations on any change, so a changed destination/coupon/cart/revision must be re-confirmed; nothing is
  silently repriced.
- Coupon intent is `readStoredCouponCode()` (explicit clear in the cart writes `null` ⇒ no coupon; no
  `value || stored` fallback). New localized refusals: `checkout.coupon.not_applicable`,
  `storefront.captured_selection.{coupon_unavailable,conflict,date_unavailable,destination_unverified,intent_invalid}`.

## Proof actually executed

- `npx tsc --noEmit` on the isolated worktree: **clean** (0 output).
- `node --test app/storefront/storefront-shipping-captured-selection.test.ts`: **6/6 pass** — exact goods text
  + every echo verbatim; six malformed/blank goods forms never produce an intent; explicit `"0"` lawful;
  no choice / date outside that method / untrusted preview ⇒ `null`; two owners same binding keep separate
  goods; every context change invalidates confirmations.
- `dotnet build Tooba.Order.Infrastructure.csproj`: **0 errors** (after the two Order fixes).
- `StorefrontCapturedSelectionWriterTests` intent builder updated to send the real
  `FinalGoodsAmountAfterDiscountAndTax` for every group (required by A).

## NOT done (measured absence, not a claim)

- **No live buyer trial**: no migration plan/apply, no task Host, no task FE, no DEV data created through
  existing APIs, no browser/HTTP read→save→preview→StartPayment run, no screenshots, no standalone
  SingleStore or Marketplace runtime, **no usable review URL, no account handoff**.
- `StorefrontFullOwnerVerticalTests.SelectionRequest` still builds `CapturedGroups` **without**
  `ExpectedGoodsAmount`; with A it will refuse `intent_invalid`. It was **not** updated and its focused class
  was **not** run in this task (no disposable PostgreSQL was started). This is a known, disclosed red.
- No `npm run test:critical-storefront` visual review run; no desktop 1440 / mobile 390–360 inspection.
- Payment page `StartPayment` was not changed here; it already forwards `expectedSelectionRevision` from the
  accepted preview receipt via the existing adapter, but that path was not exercised.
- Checked-in gates remain OFF (`Enabled`/`SelectionEnabled`/`CheckoutEnabled` false); nothing activated.

## Preserved / untouched

Original watched checkout and its branch/disk, user visible Chrome window, `previewON/count7`, live Host/FE and
runtime data, cart/address book/draft/rates/week/pointer/assignments, shared migrations (231/232/236 remain
`sharedNOTAPPLIED`), stale EF snapshot gap, TB185 paused/default-OFF/PendingAllocation. No production
activation, no real-money payment, no Visual ACCEPT.

## Correction appended by 237-R1 (2026-10-28)

The two statements above that 237-R1 supersedes:

- The 231/232/236 migrations are **no longer** sharedNOTAPPLIED on the verified non-production 	ooba_alpha:
  237-R1 applied all three through the normal Order migration runner (recorded in __ef_migrations_history).
- The captured selection/payment path **was** exercised for real in 237-R1: PUT /v1/storefront/shipping/selection
  200, payment preview 200 and a committed checkout 01a1807a-81f0-7000-a1be-091f589a6c1b
  (TB-20261028030754-01-faee55), with screenshots at 1440/390/360. See
  ../TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1/RESULT.md.

Still open after 237-R1: live coupon trial, double-click/failed-refresh negatives, and the Marketplace/each-real-seller
owner trial. No Visual ACCEPT; nothing activated.
