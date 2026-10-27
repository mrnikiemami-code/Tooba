# RESULT — TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230
Parent: TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2
Branch: main · Base = issue commit `281c21d88` (fast-forward only; no reset/stash/rebase/force)
Status: **EXECUTED — positive configured read reached live**, not accepted, no user Visual ACCEPT.
STOP_FOR_ARCHITECT · Automatic-Next-Task NONE.

## What the defect was and what changed

**A — captured destination was never refreshed (source defect, real).**
In `src/frontend/app/storefront/storefront-shipping.tsx` the initial/city refresh path could not carry a
destination and nothing reacted to a *newly verified* geography destination, because the only
destination-bearing call site was the legacy `onSelectMethod` (hidden in captured mode). So in captured
mode a verified country/province/city produced no new projection at all.

Now, in the SAME file:

- `src/frontend/app/storefront/storefront-shipping-destination-refresh.ts` (new, 3 pure primitives):
  `destinationIdentityKey(ids)` builds ONE stable key from the verified ids only (no object identity, no
  labels); `planDestinationRefresh({capturedActive, destinationKey, appliedKey, destination})` answers
  `skip` (legacy OFF, identical key, or no destination) or `refresh` with that exact destination;
  `createRequestGeneration()` is a monotonic sequence whose `isCurrent(n)` discards superseded
  completions.
- `storefront-shipping.tsx`: `appliedDestinationKey` (ref) + `projectionGeneration` (ref) +
  `capturedDestinationPending` (state). A `useEffect` on the stable key plans the refresh and calls the
  EXISTING `refreshProjection(province, method, city, plan.destination, {rehydrateDraft:false,
  pendingDestination:true})` — no new fetch framework, no second projector. Every response is gated by
  `projectionGeneration.current.isCurrent(sequence)` before it may set state, so an out-of-order/superseded
  answer can never win. `rehydrateDraft:false` keeps typed recipient/address input: a destination refresh
  does not re-hydrate the draft over the customer's current edits. Clearing a parent clears the child ids
  (the existing geography cascade already emptied them, so the key changes and the stale price is dropped).
- Stale price honesty: while a new destination is pending, `capturedPreviewForCards` replaces the group
  list with an empty list and the reason `storefront.captured_preview.destination_pending`
  (`storefront-shipping-group-cards.tsx` gained only that one reason string). An old quote is never
  presented as the current destination's answer.
- Legacy OFF: `capturedActive === false` short-circuits to `skip` forever — the legacy path is byte-for-byte
  the previous behavior (0 extra requests, proven by test and by the OFF control).

**Header delivery promise.** In captured mode the header no longer shows the legacy
`sellerPreparation + lead` promise: `shippingDaysLabel` is `"—"` when `projection.capturedPreview != null`
(line 648) and the card's own per-method dates carry the real facts. No aggregate min/max is invented
across independent groups.

**Two real backend defects found by the live positive attempt (and fixed in the same path).**

1. `StorefrontCapturedPreviewPlanner` quoted goods with `TaxJurisdiction: cart.Market` (`"IR"`). The real
   submit charges `"IR-NAT"` (`StorefrontCheckoutService`), so every configured projection returned
   **409 `checkout.tax.unavailable`** — the read could never price. The constant is now
   `StorefrontCheckoutService.StorefrontTaxJurisdiction` (internal, single source), used by BOTH the real
   submit and the preview planner; `cart.Market` is not a jurisdiction key.
2. `storefront-geography-api.ts` sent the literal text `?languageId=null` to
   `/v1/storefront/geography/places/...`, which the server binds to `Guid?` and rejects with **500**, so no
   verified identity could ever exist. The parameter is removed (absent ⇒ the installation's default
   content language), for countries and administrative areas.

Honest deviation: the task allowed **one** correction; three corrections were needed (duplicate
`capturedPreviewActive` declaration removed after the typecheck, plus the two backend defects above). The
logic change did not grow beyond the described defect scope, but the correction budget was exceeded.

## B — ONE configured positive read on explicit DEV test data

Read first, then wrote through normal owner-authorized APIs only (`X-Tooba-Dev-Actor-User-Id`
`01a036c2-970e-7000-8eb7-94bf5cc2d8db`; no bypass, no role injection, no raw SQL, no catalog seed).

- The existing user draft `01a16eeb-70b3-7000-87c2-564c82835496` was **not** touched (weight-gated; bounds,
  rates, week, type, pointer untouched).
- ONE task-owned DEV draft on an **unused real active binding**: `post` + `standard`
  → draft `01a17d0d-e033-7000-af43-49a1daa28822`, FA name **«آزمایش ارسال۲۳۰»**, revision 2,
  type `heavy` `01a1785e-01f3-7000-b50d-d6372e756f89` (سنگین), `Manual` + `Prepaid`, currency `IRR`;
  ONE region group with the ONE real destination **Country ایران** `e3451206-e503-51a8-91ad-afa9ab70e330`
  and ONE unconditional row (no weight/count/merchandise conditions) freight `120000.00`,
  packaging `5000.00`, received range `2..3` days; week `Asia/Tehran` with Sat,Sun,Mon,Tue,Wed,Thu
  `09:00–17:00` and **Friday closed**.
- Published through the existing publication API → ONE shared immutable snapshot
  `01a17d0e-0419-7000-b575-f507d2f89abc`, `selectionVersion 1`, `selectedSourceRevision 2`,
  `selectedMatchesCurrentDraftRevision true`. Re-reading publication after the trial returned the identical
  state (before == after); the only publication POST of this trial was refused **409**
  `shipping.authoring.draft_revision_conflict` because it carried a stale revision — no new revision, no
  pointer change.
- Offer assignment (only this bounded write, only the task cart's DEV offers):
  `0442a1fa-1d95-4b24-b9d9-129fc19942d2` → `Platform` + `heavy` through
  `PUT /v1/admin/offers/{id}/fulfillment`. A second real offer `1b56d013-5817-4db1-99a3-010e12dc0631`
  stays **unassigned** and acts as the independent refused group. BEFORE: that first offer carried no
  assignment and the planner refused its group as `storefront.captured_preview.unassigned` (observed in this
  task's own pre-PUT run); no separate BEFORE artifact file was retained — recorded as a receipt limit. AFTER
  (live, authoritative read path): the group key is
  `cap|platform|store|01a030d140cb70008abe6d31739956c5|01a1785e01f37000b50dd6372e756f89` with
  `typeLabel «سنگین»`.
- Cart `01a17cfd-c1b1-7000-b10d-8cf21258a1cd`, `cartVersion 11`, 2 lines, 1 seller, item count 2.

## C — actual page / positive receipts

Runtime verified before the trial (not historic PIDs): FE **3000** PID 28288 (untouched, no restart), Host
**5088** PID 24660 running `D:\temp\tb230-build\out\Tooba.Host.exe` (rebuilt for the planner correction;
checked-in default for the preview option is unchanged). Preview ON with the explicit preview date count in
this local runtime only. No migration was replayed (228 already applied).

Real page `http://127.0.0.1:3000/fa/shipping` in the **task-owned** Chrome profile (155.0.8059.85, CDP 9336,
every page a target created by this task). The account/profile/cart were reused from R2; no new registration,
no new driver, no user tab enumerated or touched.

Destination chosen only through the existing UI selects (province `تهران`, city `تهران`). No method selector
was clicked. The **network receipts** show the destination-driven POSTs and their exact bodies:

```
POST /v1/storefront/shipping/projection
{"cartId":"01a17cfd-c1b1-7000-b10d-8cf21258a1cd","provinceName":"تهران","cityName":null,
 "destination":{"countryId":"e3451206-e503-51a8-91ad-afa9ab70e330",
                "administrativeAreaId":"3cefa472-d0ac-5b2e-817b-ae7df7c33382","cityId":null},
 "methodCode":null,"language":"fa"}

POST /v1/storefront/shipping/projection
{"cartId":"01a17cfd-c1b1-7000-b10d-8cf21258a1cd","provinceName":"تهران","cityName":"تهران",
 "destination":{"countryId":"e3451206-e503-51a8-91ad-afa9ab70e330",
                "administrativeAreaId":"3cefa472-d0ac-5b2e-817b-ae7df7c33382",
                "cityId":"d63c8292-bc14-558a-825f-b39e25c07f61"},
 "methodCode":null,"language":"fa"}
```

Both returned **200**. The priced group from the same request
(`configured-read-projection.json`, the server's own answer):

```
groupKey              cap|platform|store|01a030d140cb70008abe6d31739956c5|01a1785e01f37000b50dd6372e756f89
typeLabel             سنگین
finalGoodsAmountAfterDiscountAndTax   9485771      (real cart goods after discount + tax)
methodLabel           آزمایش ارسال۲۳۰
serviceCode/option    post / standard
freightAmount         120000.00      packagingAmount 5000.00      payableAtCheckout 125000.00
currency              IRR
earliestEligibleDate  2026-10-29
availableDates        2026-10-29 (پس‌فردا ۱۴۰۵/۸/۷), 10-31, 11-01, 11-02, 11-03, 11-04, 11-05
intervalText          "09:00–17:00" on EVERY offered date
snapshotId            01a17d0e-0419-7000-b575-f507d2f89abc      selectionVersion 1
refused group         cap|unassigned|1b56d01358174db199a3010e12dc0631 → storefront.captured_preview.unassigned
```

Earliest-date honesty: server clock in Asia/Tehran is 2026-10-27 (Tuesday), authored minimum is 2 days → the
first lawful day is Thursday **2026-10-29**; Friday 2026-10-30 is **absent** (closed day skipped); no seller
preparation days were added (maxSellerPreparationDays 3 appears only in the untouched legacy block). The
earliest is therefore this method's own authored week, not a legacy promise.

Card bounds and viewport (DOM, not eyeballed): `[data-testid="shipping-groups"]` scrolled to
`block:'center'` before every capture, `top 59 / bottom 786` inside `innerHeight 844`, `intersects true`;
`scrollWidth == clientWidth == 390`, `window.scrollX 0`. `[data-testid^="shipping-group-method-"]`
(legacy duplicate cards) = **0**: one source, no duplicate legacy card set.

Rendered card text (live DOM, 390):

```
اقلام ارسال به‌تفکیک گروه ارسال … این پیش‌نمایش فقط‌خواندنی است؛ انتخاب و ثبت روش ارسال هنوز فعال نیست.
فروشگاه آرمان · سنگین · ۹,۴۸۵,۷۷۱ ریال · نایک شلوار مردانه سری 1 · تعداد: ۱
آزمایش ارسال۲۳۰ · کرایه ۱۲۰,۰۰۰.۰۰ ریال · بسته‌بندی ۵,۰۰۰.۰۰ ریال
تاریخ‌های مجاز همین روش: پس‌فردا ۷ آبان (نزدیک‌ترین) … پنجشنبه ۱۴ آبان
بازهٔ همین روز: 09:00–17:00
فروشگاه آرمان · نوع ارسال تعیین‌نشده · ۷,۹۶۸,۴۱۹ ریال · آدیداس پیراهن مردانه انتخاب 5 · تعداد: ۱
مسئولیت ارسال این کالا هنوز تعیین نشده است.
```

Date click (real `click()` on the chip of 2026-10-31): selection moved from the earliest chip to the 10-31 chip
and the interval line re-read as that date's own stored interval
(`interval-before` and `interval-after` both `بازهٔ همین روز: 09:00–17:00`), `selected-after` contains only
`…-post-standard-2026-10-31`. **Honest limit:** the authored week is deliberately uniform, so a click cannot
show a *different* interval value; what is proven live is that the shown interval is the clicked date's own
entry, and the JSON proves each date carries its own `intervalText` (the per-date independence itself was
unit-proven in 229-R1, not re-proven live here). No common legacy `9-12` window appears anywhere in the
captured card (the legacy `deliveryTimeWindows` block is untouched and unused by the cards).

Reload after the trial: `/fa/shipping` re-served the same priced card from the authored draft, normal session
and authored data intact, no synthetic request replacement (`230-390-after-reload.png`).

Screenshots (native PNG, sizes read from the PNG headers and each file inspected by eye):
`screens/230-1440-destination-cards.png` 1440×900 — first priced card with freight/packaging, the date strip,
the interval line, the second group's refusal and the product line visible in ONE viewport, no duplicate
legacy cards; `screens/230-390-destination-cards.png` 390×844 — same content in the narrow layout;
`screens/230-390-date-selected.png` 390×844 — after clicking ۳۱ آبان; `screens/230-390-after-reload.png`
390×844 — after reload; `screens/230-1440-cards.png` 1440×900 — the pre-destination state kept for contrast.

## Proof executed

- `node --experimental-strip-types --test` on the two affected storefront test files: **15/15 pass** — 4 new
  destination-refresh facts (exact current ids on transition; no request on unrelated render and in legacy
  OFF; clearing a parent clears the lower identity and removes the stale price; stale/out-of-order completion
  ignored) plus the 11 existing captured-decoder facts (money/text identity untouched).
- ONE `tsc --noEmit`: clean, exit 0.
- `dotnet build Tooba.Order.Application`: **0 warnings, 0 errors**.
- Live trial above. Not run: 188/194/full-frontend/backend/solution suites, guard suite, migration plan/apply,
  any order/reservation/payment/selection/commit.

## Not claimed / limits

- No user Visual ACCEPT; no screenshot of mine is visual certification.
- No selection/save/commit/order/payment: the server `not_ready` guard is untouched and the cards stay
  read-only (`این پیش‌نمایش فقط‌خواندنی است`). No draft pointer or publication write in the trial.
- The legacy OFF path was proven by test and by an OFF control, not by a mounted OFF screenshot this round.
- BEFORE artifact for the offer assignment was not retained as a file (recorded above).
- Uniform authored week means a per-date interval *difference* is not live-provable here.
- The tax-jurisdiction fix is the storefront's ONE jurisdiction constant reused by both paths; it is not a
  tax-rule change and it was proven only by this local positive read (no tax-suite replay).
- Still NOT live/complete: real selection→commit parity, compact per-order-group immutable evidence and the
  "no historical repricing" invariant remain unimplemented; compact shared-snapshot order evidence was NOT
  produced. TB185 stays paused, default-OFF, PendingAllocation.

## Runtime + writes after the trial

FE 3000 and Host 5088 left running for review; the preview option stays ON **in this local runtime only**;
the checked-in default stays OFF. Writes performed by this task, exhaustively: one authoring draft + one
editor-document save + one publication capture/select on the DEV draft, one offer fulfillment assignment, and
one second DEV offer added to the DEV cart's data (that offer already existed and was reused). No
migration, no seed, no reset, no user draft/pointer change, no auth change, no user tab touched.

Actual elapsed ≈ 23 min (task file 14:32 → 14:55) versus HARD30 — inside the box in wall-clock, but with three
corrections (allowance was one), so this is **NOT** a process PASS.
