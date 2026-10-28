# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5 — WORKER RESULT

Status: **PASS for the remaining functional scope** of this slice (§3 A/B/C/D executed on real fresh carts),
with the honest limits recorded below. Stop-State: `STOP_FOR_ARCHITECT`.
`automaticNextImplementationTask: NONE`. No Visual ACCEPT, no PSP/paid, no activation.

Base-SHA: `f97b439060056ae2ae7f2a8aa0c6bd66c37fa264` (issue commit, fast-forward; expected = actual).
Code-SHA: `763506b3487de1b80a799bffe91c00b96dac4e7f` (narrow payment-status fix) plus the evidence commit HEAD.

## 0 — Elapsed / deadline ledger (not reset, arithmetic corrected)

Claim (Bridge) `2026-10-28T14:52+03:30` → this result `2026-10-28T15:13+03:30` ≈ **21 min** against a
15-minute focusbox ⇒ focusbox **exceeded by ≈6 min**, recorded as such.

**R4 arithmetic correction (append-only, no history rewrite and no new Bridge diagnostic):** R4's own ledger in
`240-R4/RESULT.md` records the claim `10:38:34Z` and the result `≈10:52Z` — that pair is **≈14 min**, not 43.
The `43 min` and the derived `candidate 286 min` cumulative therefore had **no measured basis**; they were an
arithmetic error, not an independent measurement. Consequence: the pre-slice cumulative is
`243 + 14 ≈ 257` focused minutes **only if** the `243` figure was itself actual execution time, which this slice
did not re-verify. Exact cumulative is therefore labelled **UNVERIFIED** (bounded by 257–286). The original
240-minute commitment is **MISSED either way**, and this slice does not reset or excuse the overrun.

## 1 — Source change (narrow, §4 of the task)

`loadStorefrontPendingPayments().catch(() => null)` treated an **unknown** payment status as "no payment started"
and reopened a fresh pay form for an order whose payment may already exist. Repaired without touching Payment,
the server capabilities, or inventing any `paymentId`/`status`:

- new `src/frontend/app/payment/storefront-payment-existing-state.ts`: `resolveExistingPaymentState` returns
  `none | started | unknown` and `allowsFreshPaymentAction` gates the action; a **failed read is `unknown`**, a
  verified read without this checkout is the only `none`, a pending row without a payment id is `unknown`.
- `storefront-payment-handoff.tsx`: the pending read is captured as `{status:"verified"|"failed"}` (no swallow),
  `pay()` refuses when `allowsFreshPaymentAction` is false, `unknown` renders one explicit
  `payment-status-unknown` panel with an explicit re-check button (no auto retry, no fresh submit), and the
  continuation link now stamps `awaitingManual=1` **only** when the pending row is really
  `isManualAwaitingReview` — non-manual rows no longer inherit the manual flag.
- focused facts `storefront-payment-existing-state.test.ts` **7/7**; `tsc --noEmit` clean; adjacent payment
  suites 32/32.

## 2 — Fresh authenticated session (§2)

Task-owned synthetic buyer `tb240r5-<hhmmss>@example.invalid` registered through the **public**
`POST /v1/auth/register` (201), logged in through the existing BFF `GET /api/auth/csrf` + `POST /api/auth/login`
with the `X-Tooba-Csrf` double-submit header inside the **new** task profile; `/api/auth/me` returned the same
`userId` `01a18247-54d3-7000-9f2f-219ac7de9e6d` (`Marketplace`). No user credential/session was read, no
dev-actor used, password retained only in `%TEMP%\tb240r5-buyer-pass.txt` (not in git).

The exercised cart is the **browser's own** cart: `01a1824a-7e7f-7000-aca4-a6be8db4bb1e` was created by the
product's real add-to-cart on the two real PDPs (`workspace-live-scarf` → `فروشگاه آرمان`,
`workspace-live-shirt` → `دیجی‌استایل نمونه`), 2 lines, 3,240,000 IRR. No proof/localStorage key was written by
the driver. (The API-created cart of the same account was deliberately **not** used: without the browser's own
guest proof it answers 401 `cart.guest.invalid` for the payment draft — a truth about cart scoping, not a defect
in this slice.)

## 3 — The four functional checks

**A. Shipping UI → priced groups → one selection PUT.** Destination chosen through the real controls
(province `تهران` → `shipping-city-select` city `تهران`). Two independent captured groups rendered with their
**own** money, dates and intervals: `ارسال فروشگاه آرمان ۲۴۰` freight **68,000** + packaging **3,000**
(7 lawful dates starting 2026-10-29) and `ارسال دیجی‌استایل ۲۴۰` freight **47,000** + packaging **2,500**
(dates starting 2026-10-31), both `09:00–17:00`. Recipient/mobile/address/postal typed, a date chip chosen per
group, then the real **ادامه به پرداخت** control was clicked **twice in the same tick**: **exactly ONE**
`PUT /v1/storefront/shipping/selection` (200, `expectedSelectionRevision:"0"`, both groups with their own
snapshot/revision/area/row/goods echo), then the normal navigation to
`/fa/payment?cartId=01a1824a-7e7f-7000-aca4-a6be8db4bb1e`. DB at that point: draft `selection_revision = 1`,
2 draft groups, **zero** checkout rows.

**B. Real pre-commit payment form (the primary missing acceptance).** A fresh reload of
`/fa/payment?cartId=…` (no local cache prerequisite, `payment-draft` **200**, `wallet-quote` **200**,
`payment-methods` 200) rendered `payment-cart-draft-summary`, `payment-shipping-groups` with the two own-group
rows `Arman store delivery 240 ۷۱٬۰۰۰ ریال` / `DigiStyle delivery 240 ۴۹٬۵۰۰ ریال`,
`قابل پرداخت: ۳٬۶۵۲٬۱۰۰ ریال`, `وضعیت پرداخت: در انتظار پرداخت`, the draft notice «این سفارش هنوز ثبت نشده
است…», **enabled** `payment-method-manual`, an **enabled** `payment-submit` (پرداخت نهایی و ثبت سفارش),
`payment-error` **absent**, `payment-status-unknown` **absent**, and **zero** checkout rows for that cart.

**C. Final click twice in the same tick.** Two native events on the real `payment-submit` produced **exactly ONE**
`POST /v1/storefront/cart/01a1824a-…/start-payment` (200, `expectedSelectionRevision:"1"`, `providerCode:"manual"`,
one idempotency key) and the normal redirect to
`/fa/payment/result?paymentId=01a1824e-1cff-7000-88f1-db546d9df62a&checkoutId=01a1824e-1c32-7000-9669-999b6a2a7c20&awaitingManual=1`
rendering «منتظر ثبت اطلاعات پرداخت», order `TB-20261028113839-01-bbb9bf`, `۳٬۶۵۲٬۱۰۰ ریال`.
Persisted: **1** checkout, **1** distinct `submission_idempotency_key`, **2** `seller_orders` `PendingPayment`
with their own totals (1,580,500 / 1,951,100 and own tax), **2** `order_shipping_groups` carrying accepted
evidence (freight 68,000/47,000, packaging 3,000/2,500, payable 71,000/49,500, receiving 2026-10-29 / 2026-10-31,
`Asia/Tehran`, selection versions 2/1). An explicit **same-key retry** from the same session returned **200 with
the same `checkoutId` and `paymentId`** and `checkoutReplayed` present; checkout count stayed 1.
No PSP, no paid state, no second reservation/order cycle.

**D. Genuine destination failure + explicit retry (new run, not the 230-R1 history).** On a second fresh cart
`01a18251-2042-7000-8b37-bec94335eab5` (1 line) the priced card showed `۶۸,۰۰۰ / ۳,۰۰۰`; the typed first name
was set. Real offline emulation plus a province change to `البرز` produced: **0** priced cards, the old money
**gone** from the page, the retry control `shipping-captured-destination-retry` («تلاش دوباره») visible, and the
typed recipient **preserved**. Restoring the network and clicking that retry **twice in the same tick** produced
**exactly ONE** `POST /v1/storefront/shipping/projection` (200) carrying the **current** identity
(`provinceName:"البرز"`, `administrativeAreaId:null`) after which the priced card and its dates returned.
The offline failure itself is proven by the resulting state and the two `Network.loadingFailed` events, not by a
captured 4xx/5xx response.

## 4 — Deliberate non-claims / remaining gaps

- **Two groups, not three:** this Marketplace DEV catalog exposes exactly **two** real sellable products. The
  third cart shape seen in the API-level run (platform-owned `5caf` offer for the same seller) has **no product
  surface**, so a three-line/three-group build **through the UI** was not reachable. The API-level projection of
  that third group (payable 100,000) is recorded as data, not as a UI receipt.
- No PSP/provider payment, no paid/fulfilled state, no exactly-once provider guarantee claim, no
  cross-provider/tax settlement claim, no Marketplace-vs-SingleStore certification beyond this run.
- Coupon read/write/final parity was NOT re-exercised here (R4's real coupon order stays accepted within the
  manual connected scope); the coupon code is `null` in this run's `start-payment` body.
- `unknown` payment status is proven by focused facts and by the component contract, **not** by forcing a real
  `pending-payments` 5xx in the browser during this run.
- Checked-in gates stay OFF; the local task Host runs with the task-owned env ON as inherited from R4.
- Retained unresolved: external-provider concurrency, 238 unclassified `22001`, 227 mobile empty-note,
  225-R1 unrecovered LOCAL user edits. TB185 paused/default-OFF/PendingAllocation.

## 5 — Preservation and honest deviation

- The handed-off R4 profile `D:\temp\tb240r4-review-chrome` (CDP 9342) and its old-order URL were **not**
  enumerated, automated, reloaded or closed; the R5 work ran in a **new** profile `D:\temp\tb240r5-chrome`
  (CDP 9343) against an **isolated FE 3103** started from a git worktree `D:\temp\tb240r5-fe` (node_modules
  junctioned) pointed at the **same** Host `5190`.
- **Deviation, disclosed:** the narrow source fix was committed in the same working tree the pre-existing FE
  **3102** dev server watches, so that dev server's HMR may have recompiled once. No window/page of the R4
  profile was touched, and FE 3103 is the only server this run exercised.
- USER-owned surfaces (3000/5088, handoff 3100/5188 + `tb238-handoff-chrome`, user unsaved tabs, original
  draft/rates/pointer/assignments, review-230 capture, handoff cart) were not touched. No migration, seed,
  reset, reseed, direct SQL write or cross-DB copy; only read-only SQL verification against
  `tooba_marketplace_tb239`.
