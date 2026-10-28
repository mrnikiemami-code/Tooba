# RESULT — TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Parent-Task: TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237
Bridge-Task-Row: 739fb2be-ee72-4297-b074-f5ae37f8647c
Channel: tooba-main
Worker-Id: tooba-worker-01
Status: PASS (scoped: one real SingleStore buyer reaches a real checkout; Marketplace/multi-owner + coupon trials open)

## 0. Usable URL and the exact completed stage

A normal buyer session, no mocks, no intercepted responses, no dev actor:

```text
http://127.0.0.1:3100/fa/shipping                                      (configured priced group + real selection)
http://127.0.0.1:3100/fa/payment?cartId=01a1807a-e7ef-7000-82d1-285f4bd5c982   (real payment preview 200)
```

Stage actually completed end to end on this runtime (cart `01a18070-940a-7000-86ed-63cfa2c12afe`):

1. read → configured group with a real price,
2. choose the server method + one allowed date,
3. `PUT /v1/storefront/shipping/selection` **200** with the exact `expected*` body,
4. existing payment preview **200** with the real per-group fee and the real total,
5. final click → `POST /v1/storefront/cart/{id}/start-payment` **200** → real Order/checkout
   `01a1807a-81f0-7000-a1be-091f589a6c1b` / order number `TB-20261028030754-01-faee55`,
   1 reservation commit, `manual` payment `Pending`, redirect to `/payment/result?...&awaitingManual=1`.

That is a committed SingleStore order from the browser, not a parser fact. The task's `not_ready` refusal is gone
for this configured path, and nothing was activated for users (checked-in gates still OFF; only the task runtime's
local env turned the three gates on).

## 1. Runtime / process / DB ledger (actual, verified this run)

| Item | Value |
| --- | --- |
| Backend | Host `127.0.0.1:5188`, PID 22404, `dotnet bin\Release\net8.0\Tooba.Host.dll`, cwd `D:\temp\tb237\src\backend\Host\Tooba.Host`, `/health` **200** |
| Frontend | `127.0.0.1:3100`, `next dev -p 3100` from `D:\temp\tb237\src\frontend` (server PID 37492), `/fa/shipping` **200** |
| Local gates (env only) | `StorefrontCapturedPreview.Enabled/SelectionEnabled/CheckoutEnabled=true`, `AvailableDateCount=7`, `ShippingOneLevelCatalog.Enabled=true` |
| DB | `tooba_alpha` on `127.0.0.1:5432` (docker `postgres-db`), tenant `store-alpha`, SingleStore storefront |
| Buyer | `tb237r1-334481@example.invalid` created through the public `POST /v1/auth/register`; session through the existing BFF `/api/auth/csrf` + `/api/auth/login` |
| Cart | `01a18070-940a-7000-86ed-63cfa2c12afe` v4, 1 line, seller `01a030d1-40cb-7000-8abe-6d31739956c5` (فروشگاه آرمان), offer `0442a1fa-1d95-4b24-b9d9-129fc19942d2` |
| Method/type | published Manual `post:standard` (DEV «آزمایش ارسال۲۳۰»), Platform type `heavy` `01a1785e-01f3-7000-b50d-d6372e756f89`, snapshot `01a17d0e-0419-7000-b575-f507d2f89abc`, source revision 2, selectionVersion 1 |
| Destination | real Geography ایران `e3451206-…`, area `3cefa472-…`, city `95a61214-…` (service area deliberately absent) |

Applied through the normal Order migration runner on this verified non-production database (no reset, no history
edit, no manual DDL): `20261027150000_AddShippingGroupAcceptedEvidence`,
`20261027180000_AddCapturedSelectionRevisionAndDestination`,
`20261027230000_AddCapturedSelectionCouponContext`. `__ef_migrations_history` read back with those three at the top;
`plan-before.json` / `apply-after.txt` are the runner's own artifacts.

No new migration, no schema edit, no seed script, no direct business SQL insert was used for the trial.

## 2. Wire — what was wrong and what actually went over the wire

`A READ confirmation* → WRITE expected*` was the defect. The captured group body is now exactly the server DTO:

```json
{"groupKey":"cap|platform|store|01a030d1…|01a1785e…",
 "serviceCode":"post","optionCode":"standard","selectedDeliveryDate":"2026-10-31",
 "expectedSnapshotId":"01a17d0e-0419-7000-b575-f507d2f89abc",
 "expectedSourceRevision":"2","expectedSelectionVersion":"1",
 "expectedAreaGroupId":"01a17d0d-f2ca-7000-824d-de91c093bf93",
 "expectedRateRowId":"01a17d0d-f2d2-7000-8f24-7f98a1007dda",
 "expectedGoodsAmount":"9485771"}
```

and the captured request top level is empty/absent legacy shorthand exactly as the DTO allows
(`shippingMethodCode:"", selectedDeliveryDate:"", selectedDeliveryTimeWindow:"", shippingGroups:null`), with
`expectedSelectionRevision` as TEXT. Request `Content-Length 986` → `200`, `selectionRevision` `"1"` in the
response; a second lawful selection `"1"` → `"2"`.

`confirmation*` names, `shippingMethodCode`, `cartLineIds` and the per-group `selectedDeliveryTimeWindow` were
removed from the WRITE body — they are read-side names only.

## 3. Root CAS revision — the real defect found by the live trial

The page could re-enter `/fa/shipping` with the legacy `draft` absent (recovery path drops a capture-only draft)
while the captured root was still stored. The old code then sent `"0"` and the server refused the correct save with
`storefront.captured_selection.stale` — reproduced live at `03:17:20Z` (400, traceId `234a94f16aceacb140c5fa26ca4b19d6`)
and again at `03:18:42Z`.

Fix (both sides, no guard weakened):

* frontend: `resolveCapturedSelectionRevision({draftRevision, previewRevision})` uses the strict-digit
  `draft.selectionRevision`, else the server-built `capturedPreview.selectionRevision`; `null` refuses locally
  instead of inventing `"0"`;
* backend: `StorefrontShippingService` now derives the exposed revision from the **persisted** draft
  (`persistedSelectionRevision`) instead of the revalidation-local variable, so a nulled/recovered draft can never
  turn a stored root into `"0"`, and `StorefrontCapturedPreviewPlanner` stamps real `selectionEnabled` /
  `checkoutEnabled` capabilities on every branch.

Live proof after the fix: `03:21:42Z` `PUT` **200**, draft `selection_revision` `1 → 2 → 3` in
`order.cart_shipping_drafts` (`01a1807a-e7ef-…`, `updated_at 03:21:42.091397Z`), with the chosen date moved to
`2026-11-01` and the group evidence re-written (`accepted_source_revision 2`, `accepted_selection_version 1`,
freight 120000 + packaging 5000 = payable 125000).

## 4. Whole-cart intent, context identity, gates, payment handoff

* `capturedSelectionBlocker` refuses the WHOLE intent when any server group has no selectable method
  (`unavailable_group`), when a group has no choice/date, when a date is outside that method's allowed set, when any
  of the five confirmations or the exact goods text is missing. `buildCapturedSelectionIntent` no longer drops
  unavailable groups, so a partial cart can never be sent.
* `capturedSelectionContextKey` is now built from the confirmed response identity — per group: group key, exact
  goods text, and per method service/option + all five confirmations + allowed dates. A price/calendar/area/row change
  under the same snapshot therefore invalidates previous confirmations.
* Synchronous `saveInFlightRef` is set before the first `await` and released in `finally` (React `busy` alone is not
  duplicate protection). Continue only saves + navigates; no commit happens on that click — proven live: the
  `01a1807a-e7ef-…` cart has **0** rows in `order.checkouts` after three successful selections.
* `storefront-final-click-api.ts` carries `expectedSelectionRevision` as TEXT into `start-payment`, and the coupon
  intent is now tri-state (`undefined` = use the stored intent, `null`/blank = explicitly cleared) instead of
  `?? stored`.
* The read-only notice is no longer unconditional: it renders only when the server's own `selectionEnabled === false`
  (measured live: notice text empty while the card's controls were active).

## 5. Real committed order (actual services only)

`POST /v1/storefront/cart/01a18070-…/start-payment` `200` in 634 ms → `order.checkouts`
`01a1807a-81f0-7000-a1be-091f589a6c1b` (market `IR`, `IRR`, channel `Marketplace`, shipping `125000.00`,
requested `2026-10-31 09:00-17:00`, submitted `03:07:54.736089Z`);
`order.seller_orders` `TB-20261028030754-01-faee55` `PendingPayment` grand total `9485771.0000`;
`order.order_shipping_groups` `01a18079-dd8f-…` `Confirmed` with the FULL accepted evidence
(snapshot `01a17d0e-…`, draft `01a17d0d-e033-…`, source revision 2, format 1, selection version 1, owner
`Store`/`store-alpha`, area `01a17d0d-f2ca-…`, row `01a17d0d-f2d2-…`, type `01a1785e-…`, freight `120000.00`,
packaging `5000.00`, payable `125000.00`, carrier collect `0.00`, receiving `2026-10-31`
`324000000000..612000000000` `Asia/Tehran`, min/max 2/3);
exactly **1** row in `order.checkout_reservation_commits`; `payment.payments`
`01a1807a-8414-7000-b1f6-cda779360d5a` `Pending`, provider `manual`, amount `9610771.0000`, redirect
`/payment/result?checkoutId=…&paymentId=…&awaitingManual=1`, and that result page returned `200`.

An earlier API-driven rehearsal on cart `01a18064-…` produced `01a1806e-c867-…` `TB-20261028025506-01-e0d232` with
the identical accepted evidence, plus an idempotent replay (`checkoutReplayed:true`, no new reservation) and a
different-key second attempt (`checkoutReplayed:false`, new payment row on the SAME checkout). No real money, no PSP,
no fake hold, no TB185 flip.

## 6. Screenshots (exact viewport, measured, this runtime)

`screens/` — all captured from the running app in this task's own browser session:

| File | Size | Measured |
| --- | --- | --- |
| `tb237r1-1440-shipping.png` | 1440×900 | innerWidth 1440, clientWidth/scrollWidth 1425, no horizontal overflow, legacy method cards 0, captured card text 322 chars, read-only notice empty |
| `tb237r1-390-shipping.png` | 390×844 | 390/390/390, no overflow, captured card 252–593 inside 844 |
| `tb237r1-360-shipping.png` | 360×800 | 360/360/360, no overflow, captured card 230–571 |
| `tb237r1-1440-payment.png` | 1440×900 | no overflow, summary shows «هزینه ارسال: ۱۲۵٬۰۰۰ ریال» and «قابل پرداخت: ۹٬۶۱۰٬۷۷۱ ریال» |
| `tb237r1-390-payment.png` | 390×844 | bodyScrollWidth 393 == innerWidth 393 (3 px vertical-scrollbar gutter), no horizontal overflow |
| `tb237r1-360-payment.png` | 360×800 | bodyScrollWidth 378 == innerWidth 378, no horizontal overflow |

Off-canvas closed drawer nodes (`w-80 absolute right-0`) exceed the viewport width by design; `body.scrollWidth`
equals `innerWidth` on every mounted width, i.e. no real horizontal scroll.

CORRECTION (recorded): the first screenshot batch in this run was captured after the CDP device-metrics override had
expired, so four of the five files were 1920×1042 device pixels and did NOT represent 1440/390/360. They were
discarded and re-captured with the override applied in the same turn; the table above is the re-captured set.

## 7. Tests actually run (once each, no broad replay)

* `npm run typecheck` — clean, 0 errors.
* `node --test app/storefront/storefront-shipping-captured-selection.test.ts` — **9/9** pass (WRITE-key body, exact
  goods text, missing/blank token block, explicit `"0"` lawful, unavailable group blocks partial cart, no
  partial intent, two owners on one binding independent, context identity invalidation, server-only revision).
* `npm run test:critical-storefront` — 4 suites green; ONE pre-existing red guard
  `lib/storefront-appearance/component-compliance.guard.test.ts` → `app/storefront/storefront-landing-blocks.tsx:286 bg-white`.
  That file is NOT modified by this task (`git status` shows it clean), so the guard is red at the base commit too.
* `dotnet test Tooba.Order.Tests --filter "FullyQualifiedName~Storefront"` — 337 total, **299 pass / 38 fail**; every
  failure is the disposable-DB guard
  (`InvalidOperationException: TOOBA_TB231_ORDER_POSTGRES must point at the disposable 127.0.0.1:15433/order_tb231
  instance`), i.e. environment, not source. No relational suite was replayed.
* Host rebuilt to `bin\Release` from this worktree and served all of the above.

## 8. Honest scope and open items

Delivered inside this scope: SingleStore, one Platform-assigned Manual method, `Store` owner (`accepted_owner_kind 0`),
no coupon, `manual` provider pending.

NOT delivered / NOT proven here:

* **Coupon trial (§4.4)** — valid, invalid and explicit-clear coupon selections were **not** exercised live; only the
  tri-state intent code changed and is unit-covered. No live coupon evidence exists.
* **Double-click duplicate suppression and the failed-refresh retry (§4.4)** — implemented (sync ref, existing
  coordinator) and unit-covered, but no live double-click/failed-refresh artifact was captured.
* **Multi-group / Marketplace per-seller owner trial** — the live order has ONE group with owner `Store`; the
  Marketplace/seller-owned path was not exercised live (it remains in source and in the 235-R1 in-process vertical).
* **Payment** — stops at the existing `manual` boundary (`Pending`, `awaitingManual=1`); no PSP redirect, no captured
  payment, no provider success.
* **Missing-group / token / date-change negatives** were proven in tests and in the earlier stale-reproduction, not as
  a fresh live matrix on this cart.
* No HTTP-level listener proof beyond the recorded real requests; no carrier call; no historical-repricing API.
* `admin draft`, rates, week, pointer and assignments were not modified by this task; the user's original FE `:3000`
  (PID 28288) and Host `:5088` (PID 24660) were left running and untouched; the user's review window was never
  enumerated, read, automated, reloaded or closed.

Known retained gaps: 230 empty-key/in-flight coordinator edge, Offer API proof gaps, 227 mobile empty note,
225-R1 lost user LOCAL edits (no recovery). TB185 stays paused/default-OFF/PendingAllocation. Checked-in gates remain
OFF; only this task's local env had them ON.

## 9. Process ledger

* Claimed `02:21:12Z`; result written ≈`03:52Z` → **≈91 min vs HARD40 — NONCOMPLIANT, recorded**, not a process PASS.
  Stage split was boot ~35 min (build + three Host restarts after boot failures), focused wire ~25 min, live trial
  ~20 min, recovery ~9 min. The overrun came from boot/build failures, not from broad test replay.
* Corrections used: `> 1` (stale-revision diagnosis plus the screenshot re-capture above were both in-task
  corrections) — budget exceeded, recorded.
* No amend / reset / force-push / rebase; three pre-existing CRLF drifts never staged.
* `tmp-order-check.sql` (scratch) is removed from the tree; no credentials, cookies or tokens are in this file, git or
  the screenshots (the trial password lives only in `%TEMP%`).

## 10. Next (Architect)

Accept or reject this booted SingleStore path, then the still-mandatory remainder: live coupon read/selection/commit
parity, the Marketplace/each-real-seller owner trial on the same UI, and the missing negative matrix (unavailable
group, token, date, double-click, failed-refresh retry). No `automaticNextImplementationTask`, no 238 envelope.

STOP_FOR_ARCHITECT.
