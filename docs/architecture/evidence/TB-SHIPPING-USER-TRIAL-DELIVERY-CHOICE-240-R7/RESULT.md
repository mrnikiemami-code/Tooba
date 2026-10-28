# TB-SHIPPING-USER-TRIAL-DELIVERY-CHOICE-240-R7 — WORKER RESULT

Status: **PASS (scoped)** — the user's reported defect is reproduced, repaired on the EXISTING shipping page and
proven on a real two-seller cart **and** a real single-group cart: an unselected group no longer *looks* selected,
each group shows its own delivery time, and the disabled «ادامه به پرداخت» always states the short owning Persian
reason; with address + one real date per group the button enables and the click produces **exactly ONE**
`PUT /v1/storefront/shipping/selection` **200** and the normal payment form.
Stop-State: `STOP_FOR_ARCHITECT`. `automaticNextImplementationTask: NONE`.
**No backend, Contracts, DI, migration, schema, seed or reset change.** No checkout/order was created.

Base-SHA (issue commit, actual): `581c5f31aa949e92e410977dab8bab1bbefa01a8`.
Parent: TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6.

## 0 — The user's words, treated as an open defect

> «فقط یک فروشگاه بود زمان ارسال نداشت وقتی هم انتخاب کردم دکمه پرداخت روشن نشد»

Both halves were real in the shipped source, not a user-input problem:

1. **No delivery time.** A group card exposed only «تاریخ‌های مجاز همین روش» + «بازهٔ همین روز: 09:00–17:00»;
   there was no per-group «زمان تحویل» line at all, so even a chosen day never appeared as that group's delivery time.
2. **A selected-looking date that was not selected.** `CapturedMethodRow` computed
   `active = selectedDate ?? method.earliestEligibleDate ?? method.availableDates[0]?.value ?? ""` and the chip strip
   painted `active` as primary **even when no choice existed**, so the nearest day looked chosen while the method
   toggle read `aria-pressed=false` and the parent state held no choice for that group.
3. **A grey button with no reason.** `isValid` was one anonymous boolean (`disabled={!isValid || busy}`) with no
   display of *which* requirement was missing — the exact «دکمه پرداخت روشن نشد» experience.

Measured on the base build (`D:\temp\tb240r7-before.json`, real page, 2 seller groups):

| BEFORE (base) | value |
| --- | --- |
| group delivery line | `deliveryText: null` (element did not exist) |
| interval text | `بازهٔ همین روز: 09:00–17:00` |
| nearest-date chip | `styledPrimary: true`, `ariaPressed: null`, `dataSelected: null` |
| method toggle | `aria-pressed="false"` |
| Continue | `disabled: true`, `aria-describedby: null`, `blockerText: null`, `blockerId: null` |

## 1 — What was changed (existing page/cards, no new engine)

Two files changed, one small pure helper + its test added:

- `src/frontend/app/storefront/storefront-shipping-continue-state.ts` (**new**) —
  `resolveShippingContinueState()` is now the SINGLE decision for (a) whether Continue is enabled and (b) which
  short owning blocker exists, plus `describeShippingContinueBlocker()` for the Persian text. It reuses the
  EXISTING predicates (address form order, `capturedPreviewConfirmed`, and the existing
  `capturedSelectionBlocker` semantics through a group scan) and adds **no** second validator, price, calendar or
  slot engine. Codes: address field reasons, `preview_pending`, `group_unavailable`, `group_no_dates`,
  `group_date` (with the owning `groupKey`), `preview_incomplete`, and the legacy `no_preview`/`method`/`schedule`.
- `src/frontend/app/storefront/storefront-shipping.tsx` — the old inline `isValid` expression is now built once
  from `resolveShippingContinueState(...)` (`const isValid = continueState.ready`), the page resolves the owning
  group's real seller label for the message, and it renders ONE compact line under the button
  (`id/data-testid="shipping-continue-blocker"`, `aria-live="polite"`) with
  `aria-describedby="shipping-continue-blocker"` on the button while blocked. Nothing is loosened: the same
  requirements decide, and every backend/echo/revision authority is untouched.
- `src/frontend/app/storefront/storefront-shipping-group-cards.tsx` —
  * `DateChipStrip` now exposes `aria-pressed={selected}` + `data-selected` so the DOM can never disagree with state.
  * `CapturedMethodRow`: `recommended` (server earliest) is only a recommendation;
    `active = selected && selectedDate ? selectedDate : ""`. A click on the method name is an explicit user action and
    selects that method's recommended day; no render/effect auto-selects anything.
  * The interval line is labelled honestly: «زمان تحویل (بازهٔ روز پیشنهادی — هنوز انتخاب نشده): …» until a real
    day is chosen, then «زمان تحویل (بازهٔ روز انتخابی): …» — the hours themselves are always the authored
    server value (never a constant, never invented).
  * `CapturedGroupCard` renders a per-group `shipping-group-delivery-<groupKey>` line — chosen Jalali day + method
    + that day's own interval, else «هنوز انتخاب نشده» — and, while that group has no choice, the short hint
    «برای این گروه یکی از تاریخ‌های هر روش را انتخاب کنید.». Groups stay fully independent.

## 2 — Measured AFTER on the real two-seller cart (isolated target)

Isolated target per task: worktree `D:\temp\tb240r7` (base `581c5f31a`), free FE `http://127.0.0.1:3104`
(PID 46448) against the SAME task Host `127.0.0.1:5190` (PID 43548, `tooba_marketplace_tb239`, Marketplace tenant
NULL), new task-owned profile `D:\temp\tb240r7-chrome`, free CDP **9344** (Chrome PID 44116), new public synthetic
buyer `userId 01a183e3-59cd-7000-829d-a0587da45e17` (random password kept only in `%TEMP%`, never printed/committed).
Cart `01a183e5-b620-7000-bf41-97187918d843`, TWO real sellers (فروشگاه آرمان + دیجی‌استایل نمونه, real offers
`01a181b0-5e93-7000-bcc8-85f8532bd345` / `01a181b0-5cd7-7000-8ba1-a45b0df5425f`), goods 1,580,500 + 1,951,100.

| Step (§4) | DOM / state fact | HTTP |
| --- | --- | --- |
| S0 empty address, no choice | both cards: `chosenDates: []`, `methodPressed "false"`, delivery «زمان تحویل این گروه: هنوز انتخاب نشده», interval «بازهٔ روز پیشنهادی — هنوز انتخاب نشده» | one projection read |
| S1 address complete, no date | Continue disabled, blocker **«برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید.»**, `aria-describedby=shipping-continue-blocker` | — |
| S2 date for group A only (`2026-10-29`) | A: `chosenDates ["2026-10-29"]`, method `aria-pressed=true`, delivery `1405/08/07 — ارسال فروشگاه آرمان ۲۴۰ 09:00–17:00`, interval «بازهٔ روز انتخابی». B unchanged (no choice) | — |
| S2b | Continue still disabled and NOW explains B: **«برای «دیجی‌استایل نمونه» یکی از تاریخ‌های تحویل را انتخاب کنید.»** | — |
| S3 date for B (`2026-10-31`) + valid form | both groups keep their own day/interval (`1405/08/07` / `1405/08/09`), no render cleared a choice, **Continue enabled**, no blocker, `aria-describedby` removed | — |
| S4 real click | — | **exactly ONE** `PUT /v1/storefront/shipping/selection` **200** with `capturedGroups[2]` each carrying its OWN `expectedSnapshotId/expectedSourceRevision/expectedSelectionVersion/expectedAreaGroupId/expectedRateRowId/expectedGoodsAmount` and `expectedSelectionRevision` as **TEXT** (`"0"` first run, `"3"` after later saves); then `/fa/payment?cartId=…` renders the form with «روش‌های ارسال: Arman store delivery 240 ۷۱٬۰۰۰ ریال / DigiStyle delivery 240 ۴۹٬۵۰۰ ریال», «هزینه ارسال ۱۲۰٬۵۰۰ ریال», «قابل پرداخت ۳٬۶۵۲٬۱۰۰ ریال», «این سفارش هنوز ثبت نشده است» |

`net-after-continue ["PUT /v1/storefront/shipping/selection 200"]` — no second selection call, no start-payment,
no order. Typed recipient/address survived every destination and date transition (`addressFilled` stayed true for
all five fields), and the existing R1/230 coordinator still owns stale-response rejection (unchanged).

## 3 — Measured AFTER on a real SINGLE-group cart (the user's actual case)

The R6/240 review cart was one group, so the single path was proven on the SAME buyer by removing the second
seller's line through the public cart API (`DELETE …/lines/{lineId}?expectedVersion=5` → 200, cart v6, 1 line
«فروشگاه آرمان»), then using the SAME page and the SAME helper/render path (no demo UI):

| State | Fact |
| --- | --- |
| one group, nothing chosen | 1 card, `pressed: []`, «زمان تحویل این گروه: هنوز انتخاب نشده», interval «…روز پیشنهادی — هنوز انتخاب نشده», Continue disabled, blocker = name field |
| address complete | blocker = **«برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید.»** (small cart, one group, exact owner) |
| date `2026-10-29` chosen | `pressed: ["2026-10-29"]`, delivery «1405/08/07 — ارسال فروشگاه آرمان ۲۴۰ 09:00–17:00», interval «بازهٔ روز انتخابی», **Continue enabled**, blocker gone |
| real click | **ONE** `PUT /v1/storefront/shipping/selection` **200** with 1 `capturedGroup`, `expectedSelectionRevision: "2"` (TEXT), then `/fa/payment?cartId=…` |

So «یک فروشگاه» + «زمان ارسال نداشت» + «دکمه روشن نشد» is reproduced and closed on exactly the configuration
the user described, not only on a multi-seller happy path.

## 4 — Handoff (the state the user receives)

| Item | Value |
| --- | --- |
| Window | ONE task-owned **visible** installed-Chrome window, title **«اطلاعات ارسال \| توبا»**, URL `http://127.0.0.1:3104/fa/shipping` |
| Profile / CDP / PID | `D:\temp\tb240r7-chrome` / `9344` / Chrome `44116` (FE 3104 PID 46448, Host 5190 PID 43548) |
| Buyer | `GET /api/auth/me` → **200**, `userId 01a183e3-59cd-7000-829d-a0587da45e17`, edition `Marketplace`; route `GET /fa/shipping` → **200** |
| Cart | `01a183e5-b620-7000-bf41-97187918d843`, v8, **TWO** real sellers: «فروشگاه آرمان» + «دیجی‌استایل نمونه» |
| Field state | recipient «آزمون ارسال», mobile `09120000000`, استان/شهر تهران, آدرس و کد پستی filled |
| Date state | **no group date chosen** (deliberately, so the user performs the real selection) |
| Visible control | both cards read «زمان تحویل این گروه: هنوز انتخاب نشده»; Continue **disabled** with **«برای «فروشگاه آرمان» یکی از تاریخ‌های تحویل را انتخاب کنید.»** |
| Unsubmitted | payment page was only *viewed* to prove the handoff; the final window is back on `/fa/shipping`, no checkout/order/reservation exists for any final click |

After that final read the window, its profile and the FE watcher were **not** automated, reloaded or HMR-touched
again. The R6 user window (`D:\temp\tb240r5-chrome`, CDP 9343, FE 3103) and its cart/account were never enumerated,
read, automated, reloaded or closed; original 3000/5088 and the R4/handoff profiles were not touched.

## 5 — Checks (narrow, once)

- `node --experimental-strip-types --test app/storefront/storefront-shipping-continue-state.test.ts` → **13/13 pass**
  (single authority over address/group order, owning group key per group, one group never imposes another's choice,
  `group_no_dates`/`group_unavailable`, stale/unconfirmed response → `preview_pending`, foreign date refused,
  short non-technical Persian text, chips only look selected for a real choice, page renders exactly one blocker next
  to the button and wires `aria-describedby`).
- `node --experimental-strip-types --test lib/architecture/frontend-test-discovery.guard.test.ts` → **3/3 pass**
  (the new test file is inside canonical discovery; package scripts untouched).
- `tsc --noEmit` → **clean, 0 errors**.
- `frontend-source-size.guard` → **RED, PRE-EXISTING at base** (`storefront-shipping.tsx` was already **1105** lines
  vs baseline **945** at `581c5f31a`; this change makes it 1210; `storefront-shipping-api.ts` and two unrelated files
  were already over too). Not repaired here, not re-baselined — disclosed, not hidden.

## 6 — Preservation / gates / not delivered

- No migration, schema, baseline, seed, reset or direct SQL; the task Host/DB (`tooba_marketplace_tb239`) was used
  through normal APIs only. Checked-in `StorefrontCapturedPreview` gates stay **OFF**; they were ON only in the named
  task processes' local environment. TB185 stays paused/default-OFF/PendingAllocation.
- No backend waiver, no `isValid = true`, no dropped address field, no auto-submit of a first method/date, no
  invented date/hour, no price normalization, no new owner/quote/calendar/slot engine.
- NOT delivered / NOT claimed: no order/payment submit, no PSP, no paid flow, no coupon or history path exercise, no
  Marketplace-edition trial, no full solution/Order/Shipping suite run, no user Visual ACCEPT, no activation or
  release claim. The per-group delivery date label is formatted Jalali/`fa` regardless of locale (`/en` route not
  exercised). Earlier BEFORE capture of the "nearest chip looks chosen" fact was taken while that projection still
  answered `destination_required`; the chips came from the previously confirmed response, so the styling/
  `aria-pressed` mismatch is a real rendered-DOM fact but the money/date cards were not priced at that instant.
- Known older items retained, not repaired: 238 unknown `22001` inventory-outbox concurrency, 227 mobile empty-note,
  225-R1 lost user local edits, R6 Docker Desktop restart having touched the shared engine earlier (this task did not
  restart anything global).

## 7 — Elapsed / honesty

Claimed `2026-10-28T18:52:39Z`, result ≈`2026-10-28T19:25Z` ⇒ **≈32 min** vs the task's `Focus20minutes` — recorded
**NONCOMPLIANT** on the focus box, not a process PASS. The original 240 deadline stays MISSED; no new promise is made.
More than one in-task correction was needed (the first blocker text only named the field, the method-name click still
selected the *apparent* date, and the review cart had to be re-formed after a line removal), so the one-correction
budget is also exceeded and recorded.

## 8 — Merge-time Architect CORRECTION (read, NOT delivered here)

While this result was being committed, `origin/main` advanced with two docs-only Architect commits
(`4bce0a8b6`, `8c29c633e`) whose CORRECTION states that **selectable hourly receiving slots** (owner/method opening
hours + explicit slot duration ⇒ server-produced `09–10`, `10–11`, … chosen per group, validated and persisted) are a
**user-reported regression** and are **not** closed by a date/display/blocker fix. This task does not implement that:
the cards still show the method's authored single interval (`09:00–17:00`) as the delivery window text and there is no
slot picker, no slot contract, no persistence of a chosen slot. That regression stays **OPEN** for the next task; R7 is
reported as closing only the three defects it was issued for. The merge kept both the Architect CORRECTION blocks and
this worker result (no rebase/force/reset, docs-only conflict resolution, no CRLF churn).

