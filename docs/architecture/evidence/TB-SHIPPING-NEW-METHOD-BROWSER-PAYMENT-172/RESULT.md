# TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172

Mode: BOUNDED_AUTHENTICATED_PRIVATE_BROWSER_AND_PAYMENT_DRAFT_ACCEPTANCE
Base: `c36d2f8004f74cea00eae119f60514ca83ca9178` (HEAD == origin/main before work)
Verdict: **PASS**

## Verdict scope

Real private authenticated browser selection and reload of a newly created canonical-only method
(`tipax:scratch172`, no legacy option row), plus a read-only payment draft whose method identity,
single group identity and `185000 IRR` owned rate survive a full document reload, plus fail-closed
negatives and the gate-OFF control. No production code was changed.

## Isolation

| Item | Value |
| --- | --- |
| PostgreSQL | disposable container `tooba-tb172-pg` (postgres:16-alpine), `127.0.0.1:55491`, DBs `tooba_172_scratch` + `tooba_172_messaging` |
| Host | private `http://127.0.0.1:5426`, SingleStore mode, single tenant `tenant-172-scratch`, in-memory access control |
| Frontend | private Next dev `http://127.0.0.1:3121`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5426` only |
| Gate | `Tooba:ShippingOwnedRateAuthority=true` **only** in the disposable overlay for the main leg; `false` for the OFF leg |
| Deployed gate state | committed configuration untouched (`false` everywhere outside the overlay) |
| Shared | `:3000`, `:5088`, `:5432`, `tooba_alpha`, store-alpha, Redis/RabbitMQ, worker runtime **not** modified |
| Guard | `prepare-isolated-target-172.js` fails closed on scratch-port collision / multi-tenant / gate drift; `isolation-guard-on-172.json` |
| Credentials | never written to evidence (`sanitize-172.js` reports `leaked=false`) |

## A — real browser (genuine, executed)

1. `/fa/login` with the sanctioned Development fixed-OTP fixture for `09111111111`; header then showed
   the authenticated account control `09111111111`.
2. `/fa/shipping` with the real cart (2 lines, one seller, `51,000,000 IRR` subtotal).
   With no destination every method was honestly listed as "destination not resolvable / not selectable";
   after choosing `تهران` / `تهران` the server projection returned:
   `پیشتاز`, `معمولی`, and **`روش تازهٔ ۱۷۱` (selectable)**, while `post:*`, `snapp_courier`,
   `store_courier`, `in_person` stayed in "no active rate defined" (never FREE).
3. Selecting `روش تازهٔ ۱۷۱` produced `روش: روش تازهٔ ۱۷۱ · tipax:scratch172`,
   `اولین نوبت ممکن: 1405/07/30`, checked window `۹ صبح تا ۱۲ ظهر`, fee `۱۸۵٬۰۰۰ ریال`,
   total `۵۱٬۱۸۵٬۰۰۰ ریال`, and enabled `ادامه به پرداخت`.
4. Continue → `/fa/payment?cartId=…` rendered `روش ارسال: روش تازهٔ ۱۷۱`,
   `هزینه ارسال: ۱۸۵٬۰۰۰ ریال`, `قابل پرداخت: ۵۵٬۷۷۵٬۰۰۰ ریال`, `در انتظار پرداخت`, and the
   "no order created yet" statement.
5. Fresh full document reload of the same URL reproduced the identical method, fee, payable amount and
   pending state.

Screenshots: `browser-shipping-page-172.png`, `browser-payment-page-172.png`,
`browser-payment-reload-172.png` (OFF control: `browser-off-control-172.png`).
DOM + inner-text assertions: `browser-dom-assertions-172.txt`.

## B — real HTTP/DB (same customer/cart)

`verify-after-browser-172.js` (7/7 PASS) and `acceptance-172.js on` (37/37 PASS):

- projection → `tipax:scratch172` at `185000 IRR`, `leadDays=3`, `isFree=false`, canonical identity
  `serviceCode=tipax` / `optionCode=scratch172`, no sibling/provider amount inherited.
- selection with the server-returned date/window persisted; readback `selectedMethodCode=tipax:scratch172`,
  `selectedShippingAmount=185000`.
- read-only `GET /v1/storefront/cart/{cartId}/payment-draft` → 200, `shippingMethodCode=tipax:scratch172`,
  one shipping group `01a14869-212b-7000-acd1-8d0cfd374944`, `shippingAmount=185000`,
  `subtotalExclusiveOfTax=51000000`, `discountAmount=0`, `taxAmount=4590000`,
  `payableAmount=55775000` = `51000000 − 0 + 4590000 + 185000`.
- DB readback: one shipping group, exactly 2 membership lines, `checkouts=0`, `payments=0`,
  `seller_orders=0`; no payment initiated, no checkout row, no order.

Traces: `http-trace-on-172.txt`, `http-after-browser-172.json`, `projection-on-172.json`,
`db-readback-172.txt`, `assertion-matrix-on-172.json`.

## C — negatives (fail closed, no fabricated rate)

| Negative | Observed |
| --- | --- |
| Active canonical method, owned rate absent | absent from projection (`visibleCodes=tipax:express,tipax:standard`), no zero/FREE price |
| Method-specific coverage `Restricted` with zero own grants | absent from projection; `PUT selection` → 400 `shipping.method.unavailable` |
| Rejected selection integrity | no zero/legacy amount stored; accepted draft preserved (`185000`, method unchanged) |
| Delivery date earlier than server minimum | 400 `shipping.delivery.too_early`; accepted draft intact |
| Restored rate/coverage | method visible again at `185000`; other methods unchanged |

Matrix: `negative-and-off-matrix-172.json`.

## D — gate OFF control (same scratch data)

`acceptance-172.js off` 6/6 PASS on the same cart: canonical-only method **absent**,
legacy methods keep their committed quotes (`tipax:express 260000`, `tipax:standard 180000`),
no `UnavailableMethods` block. Browser reload confirmed the same (`hasScratch=false`,
no "روش‌های ارسال بدون نرخ معتبر" section). Committed flags unchanged.

## Harness (all task-owned, under this folder)

`prepare-isolated-target-172.js`, `bootstrap-scratch-172.ps1`, `relaunch-isolated-host-172.ps1`,
`scratch-172-base-fixture.sql`, `scratch-172-tax-fixture.sql` (scratch reference data so the read-only
payment draft is not legitimately blocked by `checkout.tax.unavailable`), `reset-scratch-172.sql`,
`acceptance-172.js`, `verify-after-browser-172.js`, `sanitize-172.js`, `cleanup-172.ps1`.

## Cleanup

`cleanup-172.txt`: container removed, no process listening on `5426/55491/3121`,
shared `3000/5088/5432` untouched.

## Notes / explicit non-goals

- No production or contract change; the browser journey used the existing storefront UI and the
  existing server-returned date/window/fee.
- Not covered here (task-declared out of scope): same-seller incompatible origins, carrier working-day
  capacity/timezone, settlement share/payout, release performance.
- No final purchase, PSP, SMS, or real payout was executed.

STOP_FOR_ARCHITECT.
