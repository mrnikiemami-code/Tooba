# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4 — WORKER RESULT

Status: **PARTIAL** (usable authenticated order page + real coupon order reached; two §3 UI items unexecuted).
Stop-State: `STOP_FOR_ARCHITECT`. `automaticNextImplementationTask: NONE`. No Visual ACCEPT, no activation, no PSP.

Base-SHA: `3fa823d33148f6c9cde0c3df12b216df17593f62` (issue commit, fast-forward; expected = actual,
no amended/forced history).

## 0 — Deadline ledger (not reset)

Original 240-minute commitment was **MISSED** before this slice (~243 focused minutes across 240/R1/R2/R3).
This recovery segment ran 14:05:38Z-task-clock → ≈14:44 (+03:30) ≈ **39 min** against the 30-minute focusbox and
the 10/25 checkpoints, i.e. the focusbox was **exceeded by ≈9 min** and is recorded as such. Cumulative actual
focused time ≈ **282 min**. No new delivery promise and no automatic 241.

## 1 — USABLE AUTHENTICATED SURFACE (the actual user-visible stage)

`http://127.0.0.1:3102/fa/payment?checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55`

Real buyer BFF session, task-owned profile `D:\temp\tb240r4-review-chrome` (CDP 9342), **browser-local
committed/payment cache EMPTY before the read** (`localBefore.keys = []`): the page performs
`GET /v1/storefront/checkout/{id}` **without any `cartId` locator** and gets **200**, then
`GET /v1/storefront/checkout/{id}/wallet-quote?cartId=01a181e2-0d5e-7000-a94c-87e9b826f94d` **200** using the
`cartId` **returned by that server response** (no active-cart fallback; `/v1/storefront/cart/current` is a real
404 in the same trace), and `POST /v1/storefront/pending-payments` **200**.
Rendered functional state (DOM, no images): 3 shipping-group cards (`Platform delivery 240 ۱۰۰٬۰۰۰ ریال`,
`Arman store delivery 240 ۶۵٬۰۰۰ ریال`, `DigiStyle delivery 240 ۴۹٬۵۰۰ ریال`), `قابل پرداخت: ۵٬۷۶۲٬۶۰۰ ریال`,
`وضعیت پرداخت: در انتظار پرداخت`, **0** fresh pay buttons, **1** `payment-started-continuation` node pointing at
`/fa/payment/result?paymentId=01a18209-d01d-7000-a1f0-d59a53b1124f&checkoutId=…&awaitingManual=1`,
`payment-error` absent, no horizontal overflow (1424/1409/1409) at desktop and 390 (`scrollWidth == clientWidth`)
widths, and the same facts survive one real reload with the local cache still empty.

So the local 403 guard is gone for an authenticated owner, `checkoutId` is only an untrusted locator, a foreign
buyer and an anonymous caller both get **404 `checkout.missing`** (no proof leak, no ownership weakening), and an
already-converted/`PendingPayment` order never offers an illegal fresh payment action.

## 2 — REAL COUPON ORDER (read → exact 3-group choice → draft → final click → persisted Order)

Task-only Promotion coupon `TB240R4DIGI` (PercentageOff, 10 %, seller-eligible for **DigiStyle only**,
seller `01a181b0-5c9a-7000-bf9b-8c8e1c47da72`), created through the existing seller promotion API. Clean buyer
`tb240r4-coupon@example.invalid` (registered through the public auth API; password only in `%TEMP%`, never in git).

| step | request | status | fact |
|---|---|---|---|
| read, no coupon | `POST /v1/storefront/shipping/projection` | 200 | 3 groups; DigiStyle goods **1,951,100** |
| read, with coupon | same `+ "couponCode":"TB240R4DIGI"` | 200 | DigiStyle goods **1,755,990**; the two Arman groups unchanged (**2,016,500** / **1,580,500**) |
| invalid coupon | same `+ "couponCode":"TB240R4NOPE"` | **409** | `checkout.coupon.not_applicable` (no group repriced) |
| exact choice | `PUT /v1/storefront/shipping/selection` (all 3 groups, echo tokens, coupon) | 200 | `selectionRevision 1` |
| draft | `GET /v1/storefront/cart/{cart}/payment-draft?couponCode=TB240R4DIGI` | 200 | `discount 179,000`, `shipping 214,500`, `tax 441,990`, 3 groups |
| clear coupon | read (no coupon) + selection without `couponCode` | 200 | `selectionRevision 2`; draft `discount 0` — the old code is **not** resurrected |
| re-apply | read + selection with coupon | 200 | `selectionRevision 3` — new confirmation required |
| stale confirmation | selection with the previous revision | **400** | `storefront.captured_selection.stale` |
| final click | `POST /v1/storefront/cart/{cart}/start-payment` | 200 | checkout `01a18230-b7c1-7000-9983-6d588a5b0c08`, payment `01a18230-b91a-7000-a804-e26e52ec8f40`, amount **5,567,490** |
| order read | `GET /v1/storefront/checkout/{new}` | 200 | `PendingPayment`, payable **5,567,490**, discount **179,000**, tax **441,990**, shipping **214,500**, 3 groups |

Persisted facts (Order storage, DB `tooba_marketplace_tb239`): `seller_orders` → Arman
`subtotal 3,300,000 / discount 0 / tax 297,000 / grand 3,597,000`, DigiStyle
`subtotal 1,790,000 / discount 179,000 / tax 144,990 / grand 1,755,990`; the DigiStyle `order_lines` row carries
`promotion_code_snapshot TB240R4DIGI`, `discount_kind_snapshot PercentageOff`, `pre 1,790,000 → post 1,611,000`,
`tax 144,990`, `incl 1,755,990`; `order_shipping_groups` 3 rows with own `accepted_owner_kind` (1 Marketplace,
2 Seller + own seller id), own snapshot/draft/sourceRevision/selectionVersion, freight 95,000 / 62,000 / 47,000,
packaging 5,000 / 3,000 / 2,500, payable 100,000 / 65,000 / 49,500, own dates and 100-ns ticks, `Asia/Tehran`;
`payment.payments` = **1** row, `reservation_cycles` = **1**, `checkout_processes` = `PaymentPending`.

Duplicate-click containment on that same real cart (converted): two sequential **and** two concurrent identical
`start-payment` calls with the stored idempotency key all returned **200** with the **same** checkout and the
**same** payment id and `checkoutReplayed=true`, and the row counts above stayed 1/1. This is a bounded
same-key/no-duplicate-row proof, **not** a PSP exactly-once claim.

## 3 — PRESERVED ORIGINAL ORDER AFTER A REAL PUBLICATION CHANGE

Baseline `GET` of `01a18209-cd78-7000-a9b6-02dadf377e55` was taken first. Then **exactly one** task-owned DEV
publication was changed through the normal seller authoring API: draft `01a181dd-6bcb-7000-81eb-576c1f587eef`
(Arman), its single rate row freight **62,000 → 68,000** (`PUT …/rate-rows` 200, revision 6 → 7) and
`POST …/publication` 200 → new snapshot `01a18231-8fbe-7000-85fd-23705cc49084`, `sourceRevision 7`,
`selectionVersion 2`. A fresh projection on a new cart immediately quotes it: freight **68,000** + packaging
**3,000** = **71,000** for the seller-owned group (and the untouched Marketplace group still 95,000 + 5,000 =
100,000).

Re-reading the original order returned **0 differences** across 3 group ids, seller ids, labels, amounts, dates
and windows plus all 20 compact history fields per group (`ownerKind`, owner seller, `snapshotId`, `draftId`,
`sourceRevision`, `formatVersion`, `selectionVersion`, area/row/type, freight/packaging/payable/collect,
`receivingDate`, open/close ticks, timezone, min/max days). The order still reports snapshots
`01a181ca-e8f8…` / `01a181de-0c80…` / `01a181de-0e59…` at `sourceRevision 6 / selectionVersion 1` with freight
95,000 / **62,000** / 47,000 and payable **5,762,600**, i.e. it is served from Order persistence, not from the
current Shipping publication. A foreign buyer and an anonymous caller read **404 `checkout.missing`**.

## 4 — SOURCE (narrow, existing paths only)

- `app/storefront/storefront-checkout-api.ts` — an authenticated owner falls back to the authorized server read
  when no browser-local committed/payment proof exists; local storage is only a hint and is never written as proof.
- `app/payment/storefront-payment-handoff.tsx` — owner read **first**, then wallet/method/pending-payment reads
  use the returned `cartId`/`checkoutId`; an already-started manual payment renders the existing continuation
  surface instead of a fresh form.
- `app/storefront/storefront-payment-api.ts` — `loadStorefrontWalletQuote(checkoutId, cartIdHint?)`.
- `StorefrontOrderEndpoints.GetCheckoutAsync` + `GetStorefrontCheckoutQueryValidator` — `cartId` locator
  optional; service still ignores it after `GetOwnedForPaymentResultAsync`, so it grants nothing.
- New read-only port `IOrderShippingGroupHistoryReader` (`Order.Application/Storefront/Ports`) +
  `OrderShippingGroupHistoryReader` (`Order.Infrastructure/Integrations/Storefront`, `AsNoTracking`) +
  `StorefrontShippingGroupHistoryView` on the existing group view, populated only from
  `order.order_shipping_groups`; no Shipping/Catalog/Pricing lookup and no new history endpoint.
- `StorefrontCheckoutService` takes that reader as a required dependency (DI in `OrderModule`); existing tests
  pass `shippingGroupHistory: null!` on paths that never read a checkout.

Verification: `npx tsc --noEmit` **0**; `Tooba.Order.Endpoints` and `Tooba.Order.Infrastructure` builds
**0 errors / 0 warnings**; focused Order tests **66 passed / 1 failed**, the single red being the
**pre-existing** architecture guard `Application_storefront_uses_contracts_only_no_foreign_application` on
`DateTimeOffset.UtcNow` inside the untouched `StorefrontCapturedPreviewPlanner` (file not modified by this task).

## 5 — NOT DELIVERED (exact remaining work)

1. **UI double-click counter on the real Continue/final-click controls** was not measured in the browser. The
   page under test ends in `payment-started-continuation` (no pay button) and the coupon cart is already
   converted, so the ≤1-request DOM/network counter is proven only at the API layer (§2: 4 identical calls →
   1 checkout / 1 payment row). Needs a fresh pre-commit cart driven through the real controls.
2. **Fresh pre-commit payment form in a normal NEW cart** was not mounted: the only browser session available is
   the original buyer's (its cart is converted), and the coupon buyer exists only as an API account whose
   password is not retained, so I did not fabricate a session. A new cart + real form for that buyer is the
   remaining step for a "current payment route" URL.
3. **Real failed-destination request + explicit retry** (stale price hidden, typed address preserved) was not
   re-executed in this slice; the functional behaviour is the one accepted in 230-R1.
4. Unchanged and still true: `checkoutReplayed=false` on the **first** call remains the existing mapping (repeats
   report `true`); `PaymentGateway.InitiateAsync` still precedes the contested `SaveChanges`, so manual one-row
   persistence is **not** provider exactly-once; no PSP/paid/activation claim.

## 6 — Preservation

No screenshot/visual work of any kind (explicit user override). No migration, reset, reseed, drop, business SQL
or cross-DB copy; `20261028140000` was not re-run. Only the task Host 5190 / FE 3102 and the task-owned Chrome
profile were used; the user's 3000/5088 windows, unsaved tabs, handoff 3100/5188, the 230 capture, the original
draft/rates/week/pointer/assignments (except the one deliberate DEV rate-row change above) and the handoff cart
were never enumerated, read, reloaded, automated or closed. No secret, password, token, cookie or guest secret is
committed. Checked-in shipping gates remain OFF; TB185 stays paused/default-OFF/PendingAllocation. Known retained:
238 unclassified 22001, 227 mobile empty-note, 225-R1 unrecovered local edit loss.

STOP_FOR_ARCHITECT · automaticNextImplementationTask NONE · no automatic 241.
