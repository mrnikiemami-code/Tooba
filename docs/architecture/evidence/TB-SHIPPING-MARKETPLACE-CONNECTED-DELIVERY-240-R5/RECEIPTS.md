# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5 — RECEIPTS

All receipts are HTTP / DOM / persisted-DB facts. **No screenshots, no images, no viewport matrix** (the user
withdrew them). Sanitised: no password, token or cookie value is reproduced.

## Environment (actual)

| Item | Value |
| --- | --- |
| Host | `http://127.0.0.1:5190` (`tb239-marketplace`, Development, DB `tooba_marketplace_tb239`) |
| FE (fake cart runs) | `http://127.0.0.1:3103` — isolated worktree `D:\temp\tb240r5-fe` (HEAD `763506b34`, node_modules junction) |
| Browser profile | `D:\temp\tb240r5-chrome`, CDP `9343` (new; R4 profile `tb240r4-review-chrome`/9342 untouched) |
| Buyer | `tb240r5-<hhmmss>@example.invalid`, `userId 01a18247-54d3-7000-9f2f-219ac7de9e6d`, edition `Marketplace` |

## 1 — Registration / login

```
POST /v1/auth/register            -> 201
POST /v1/auth/login               -> 200  (accessToken issued)
GET  /v1/auth/me                  -> 200  id=01a18247-54d3-7000-9f2f-219ac7de9e6d edition=Marketplace
(browser, FE 3103 origin)
GET  /api/auth/csrf               -> 200  (tooba_csrf cookie)
POST /api/auth/login  X-Tooba-Csrf -> 200  {"userId":"01a18247-54d3-7000-9f2f-219ac7de9e6d","authenticated":true}
GET  /api/auth/me                 -> 200  same userId, edition Marketplace
```

## 2 — Browser-owned cart (§2)

Real add-to-cart on the two real PDPs:

```
GET /fa/products/workspace-live-scarf   → click «افزودن به سبد خرید»  → line offer 01a181b0-5e93-… (فروشگاه آرمان)
GET /fa/products/workspace-live-shirt   → click «افزودن به سبد خرید»  → line offer 01a181b0-5cd7-… (دیجی‌استایل نمونه)
GET /v1/storefront/cart/current → 200 {cartId:01a1824a-7e7f-7000-aca4-a6be8db4bb1e, version:5, 2 lines, 3,240,000 IRR}
```

Control (not used for the run): the API-created cart of the same account answers
`GET /v1/storefront/cart/01a18247-55ac-…/payment-draft → 401 cart.guest.invalid` inside the browser, because the
browser holds no guest proof for a cart it did not create. Recorded as a scoping truth, not as a defect.

## 3 — A: priced groups and ONE selection PUT

```
GET  /fa/shipping                       → province select «تهران»
select[0].change = تهران                → GET cities
select#shipping-city-select.change = تهران
POST /v1/storefront/shipping/projection → 200 (2 captured groups)
  group cap|seller|…5c87… goods 1,580,500  freight 68,000  packaging 3,000  dates 2026-10-29… (7)
  group cap|seller|…5c9a… goods 1,951,100  freight 47,000  packaging 2,500  dates 2026-10-31… (7)
date chip clicked per group; recipient/mobile/address/postal typed
«ادامه به پرداخت» clicked TWICE in the same tick:
PUT /v1/storefront/shipping/selection   → 200  (exactly ONE request)
  body: expectedCartVersion 5, expectedSelectionRevision "0", 2 capturedGroups with own
        snapshot/sourceRevision/selectionVersion/areaGroupId/rateRowId/goods
→ location = /fa/payment?cartId=01a1824a-7e7f-7000-aca4-a6be8db4bb1e
DB ("order".cart_shipping_drafts) selection_revision = 1 ; draft groups = 2 ; checkouts = 0
```

## 4 — B: pre-commit payment form (fresh reload)

```
GET /v1/storefront/cart/01a1824a-…/payment-draft   -> 200
GET /v1/storefront/cart/01a1824a-…/wallet-quote    -> 200
GET /v1/storefront/payment-methods                 -> 200
DOM:
  [payment-cart-draft-summary] 1
  [payment-shipping-groups] 1
  group rows: "Arman store delivery 240 ۷۱٬۰۰۰ ریال", "DigiStyle delivery 240 ۴۹٬۵۰۰ ریال"
  [payment-payable-amount] "قابل پرداخت: ۳٬۶۵۲٬۱۰۰ ریال"
  [payment-cart-draft-notice] "این سفارش هنوز ثبت نشده است…"
  [payment-method-manual] present, disabled=false
  [payment-submit] present, text "پرداخت نهایی و ثبت سفارش", disabled=false
  [payment-error] absent ; [payment-status-unknown] absent ; [payment-methods-unavailable] absent
  checkout rows for this cart at this moment: 0
```

## 5 — C: final click twice in one tick

```
before: [payment-submit] enabled, no [payment-error]
two native click sequences on the same control in one tick
POST /v1/storefront/cart/01a1824a-…/start-payment -> 200   (EXACTLY ONE)
  body {expectedCartVersion:5, idempotencyKey:"d08fd19a-…", useWallet:false, providerCode:"manual",
        couponCode:null, expectedSelectionRevision:"1"}
→ /fa/payment/result?paymentId=01a1824e-1cff-7000-88f1-db546d9df62a
                     &checkoutId=01a1824e-1c32-7000-9669-999b6a2a7c20&awaitingManual=1
  page: «منتظر ثبت اطلاعات پرداخت», «شماره سفارش: TB-20261028113839-01-bbb9bf», «مبلغ: ۳٬۶۵۲٬۱۰۰ ریال»
Explicit same-key retry (same session):
POST …/start-payment -> 200 same checkoutId 01a1824e-1c32-7000-9669-999b6a2a7c20
                          same paymentId  01a1824e-1cff-7000-88f1-db546d9df62a  (checkoutReplayed present)
```

Persisted verification (read-only SQL):

```
checkouts | processes | idem_keys          -> 1 | 1 | 1
seller_orders (PendingPayment)              -> TB-20261028113839-01-bbb9bf 1,580,500 (net 1,450,000 + tax 130,500)
                                              TB-20261028113839-02-6cb9e1 1,951,100 (net 1,790,000 + tax 161,100)
order_shipping_groups (accepted evidence)   -> 2/2 captured
  Arman store delivery 240    68,000.00 + 3,000.00 = 71,000.00  2026-10-29  Asia/Tehran  selVer 2
  DigiStyle delivery 240      47,000.00 + 2,500.00 = 49,500.00  2026-10-31  Asia/Tehran  selVer 1
cart_shipping_drafts                        -> selection_revision 1, coupon NULL
```

## 6 — D: destination failure + explicit retry (new cart)

```
new browser cart 01a18251-2042-7000-8b37-bec94335eab5 (1 line) ; typed first name "خریدار"
before: 1 priced card "کرایه ۶۸,۰۰۰ ریال · بسته‌بندی ۳,۰۰۰ ریال" ; retry control absent
Network.emulateNetworkConditions offline:true ; province select → البرز
  priced cards: 0 ; old money absent from page ; [shipping-captured-destination-retry] «تلاش دوباره» visible
  typed first name still "خریدار" ; Network.loadingFailed events: 2
Network.emulateNetworkConditions offline:false ; retry clicked TWICE in one tick
  POST /v1/storefront/shipping/projection -> 200 (EXACTLY ONE)
   body {cartId:01a18251-…, provinceName:"البرز", cityName:null,
         destination:{countryId:01a181c5-f010-…, administrativeAreaId:null, cityId:null}, couponCode:null}
  priced card + its 7 dates returned ; retry control gone
```

## 7 — Focused frontend facts

```
node --experimental-strip-types --test app/payment/storefront-payment-existing-state.test.ts  -> 7/7 pass
npx tsc --noEmit                                                                             -> 0 errors
adjacent: storefront-payment-page + grouped-summary + pending-payment-api + payment-api      -> 32/32 pass
```
