# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4 — RECEIPTS

Sanitized. No screenshots by explicit user override. No password/token/cookie/guest secret retained here.
Task runtime: Host `127.0.0.1:5190` (Development, Marketplace, TenantNULL, DB `tooba_marketplace_tb239`),
FE `127.0.0.1:3102`, task-owned profile `D:\temp\tb240r4-review-chrome` (CDP 9342). No user 3000/5088 contact.

## R1 — authenticated order recovery (real browser, empty local cache)

```
GET /api/auth/me                                            200
GET /v1/storefront/checkout/01a18209-cd78-7000-a9b6-02dadf377e55   200   (no cartId locator)
GET /v1/storefront/cart/current                             404   (no active-cart fallback)
GET /v1/storefront/payment-methods                           200
GET /v1/storefront/checkout/<id>/wallet-quote?cartId=01a181e2-0d5e-7000-a94c-87e9b826f94d  200  (cartId from the 200 read)
POST /v1/storefront/pending-payments                         200

localBefore = { keys: [], committed: [], session: [] }
DOM: shippingGroupCards=3  submitButtons=0  pendingContinuation=1  errorNode=null  methodsUnavailable=0
     payable="قابل پرداخت: ۵٬۷۶۲٬۶۰۰ ریال"   paymentState="در انتظار پرداخت"
     continuation="/fa/payment/result?paymentId=01a18209-d01d-7000-a1f0-d59a53b1124f&checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55&awaitingManual=1"
     overflowX=false @1424 and @390 (scrollWidth==clientWidth)
reload: same facts, local cache still empty
artifact: D:\temp\tb240r4-browser-facts.json
```

Access negatives (same endpoint):

```
foreign buyer  (Authorization: another buyer)                404  checkout.missing
anonymous      (no Authorization, no guest secret)            404  checkout.missing
anonymous      (wrong X-Tooba-Guest-Secret)                   404  checkout.missing
owner          (Authorization: owner, unknown checkout id)    404  checkout.missing
owner          (Authorization: owner, explicit cartId echo)   200  3 groups
```

## R2 — coupon lifecycle (clean buyer `tb240r4-coupon@example.invalid`)

```
POST /v1/storefront/cart                                       201/200  cart=01a18230-b338-7000-ad47-1e0d515076d2
POST /v1/storefront/cart/<id>/lines  ×3                        200      version 7
POST /v1/storefront/shipping/projection                        200      groups=3  (DigiStyle goods 1951100)
POST /v1/storefront/shipping/projection  couponCode=TB240R4DIGI 200     groups=3  (DigiStyle goods 1755990)
POST /v1/storefront/shipping/projection  couponCode=TB240R4NOPE 409     checkout.coupon.not_applicable
PUT  /v1/storefront/shipping/selection   (3 echo groups + coupon) 200   selectionRevision 1
GET  /v1/storefront/cart/<id>/payment-draft?couponCode=TB240R4DIGI 200  discount 179000  shipping 214500  tax 441990
POST /v1/storefront/shipping/projection                        200      (no coupon)
PUT  /v1/storefront/shipping/selection   (no coupon, rev 1)    200      selectionRevision 2
GET  /v1/storefront/cart/<id>/payment-draft                    200      discount 0   (code not resurrected)
POST /v1/storefront/shipping/projection  couponCode=TB240R4DIGI 200
PUT  /v1/storefront/shipping/selection   (coupon, rev 2)       200      selectionRevision 3
PUT  /v1/storefront/shipping/selection   (coupon, stale rev 2) 400      storefront.captured_selection.stale
POST /v1/storefront/cart/<id>/start-payment                    200      checkout 01a18230-b7c1-7000-9983-6d588a5b0c08
                                                                       payment  01a18230-b91a-7000-a804-e26e52ec8f40
                                                                       amount   5567490   (first call, replayed=false)
GET  /v1/storefront/checkout/01a18230-b7c1-7000-9983-6d588a5b0c08 200  PendingPayment payable 5567490
                                                                       discount 179000 tax 441990 ship 214500 groups 3
artifacts: tb240r4-projection-nocoupon.json, tb240r4-projection-coupon.json,
           tb240r4-selection-coupon.json, tb240r4-draft-coupon.json, tb240r4-draft-cleared.json,
           tb240r4-selection-final.json, tb240r4-start-payment.json, tb240r4-order-with-coupon.json
```

## R3 — persisted DB facts (`tooba_marketplace_tb239`)

```
order.seller_orders  (checkout 01a18230-b7c1…)
  Arman     sub 3300000.0000  disc       0.0000  tax 297000.0000  grand 3597000.0000
  DigiStyle sub 1790000.0000  disc  179000.0000  tax 144990.0000  grand 1755990.0000
order.order_lines    (DigiStyle)
  promotion_code_snapshot TB240R4DIGI  discount_amount_snapshot 179000.0000  kind PercentageOff
  pre_discount_tax_exclusive 1790000.0000  post_discount_tax_exclusive 1611000.0000
  tax_amount_snapshot 144990.0000  tax_inclusive_snapshot 1755990.0000
order.order_shipping_groups (checkout 01a18230-b7c1…) 3 rows
  01a18230-b4ed-7000-8871… kind=2(Seller)  seller 5c87… snap 01a181de-0c80… rev 6 sel 1
                          freight 62000.00 pack 3000.00 pay 65000.00  date 2026-10-29  ticks 324000000000/612000000000
  01a18230-b4ed-7000-9c0f… kind=2(Seller)  seller 5c9a… snap 01a181de-0e59… rev 6 sel 1
                          freight 47000.00 pack 2500.00 pay 49500.00  date 2026-10-31
  01a18230-b4ed-7000-ac07… kind=1(Marketplace)      snap 01a181ca-e8f8… rev 6 sel 1
                          freight 95000.00 pack 5000.00 pay 100000.00 date 2026-10-31
  timezone Asia/Tehran on all three
payment.payments      count = 1
order.reservation_cycles count = 1
order.checkout_processes status = PaymentPending
```

## R4 — duplicate-click containment (same real cart, converted)

```
POST start-payment (same idempotency key) #1  200 checkout 01a18230-b7c1… payment 01a18230-b91a… replayed=true
POST start-payment (same idempotency key) #2  200 same checkout, same payment, replayed=true
two concurrent identical POSTs                200,200 same checkout, same payment, replayed=true,true
payment rows for that checkout = 1 ; reservation cycles = 1
```

## R5 — publication change vs original order preservation

```
baseline GET 01a18209-cd78-7000-a9b6-02dadf377e55   200  3 groups, payable 5762600
seller authoring draft 01a181dd-6bcb-7000-81eb-576c1f587eef
  GET  …/region-groups/<g>/rate-rows                200  freight 62000 (revision 6)
  PUT  …/region-groups/<g>/rate-rows                200  freight 68000 → revision 7
  POST …/publication (expectedRevision 7, selVer 1)  200  snapshot 01a18231-8fbe-7000-85fd-23705cc49084
                                                          sourceRevision 7  selectionVersion 2
fresh projection (new cart, seller-owned group)     200  freight 68000 + packaging 3000 = payable 71000
                                                          Marketplace group still 95000+5000=100000
re-GET original order                               200  diffs = 0 (8 group fields + 20 history fields ×3)
   groups 01a18205-63a7/63a8/63a8  ownerKinds Marketplace,Seller,Seller
   snapshots 01a181ca-e8f8…, 01a181de-0c80…, 01a181de-0e59…  rev 6  sel 1
   freight 95000/62000/47000  payable 5762600  dates 2026-10-31/2026-10-29/2026-10-31
foreign buyer GET                                   404  checkout.missing
anonymous GET                                       404  checkout.missing
GET without cartId locator                          200  3 groups
artifacts: tb240r4-original-before.json, tb240r4-original-after.json, tb240r4-publish-change.json
```

## R6 — build/typecheck/test

```
npx tsc --noEmit (src/frontend)                               exit 0
dotnet build Tooba.Order.Endpoints       Debug                0 errors 0 warnings
dotnet build Tooba.Order.Infrastructure  Debug                0 errors 0 warnings
dotnet test  Order.Tests (guards + storefront payment/endpoint/contract classes)   66 passed / 1 failed
  FAILED (pre-existing): OrderStorefrontArchitectureGuardTests
      .Application_storefront_uses_contracts_only_no_foreign_application
      reason: "DateTimeOffset.UtcNow" inside StorefrontCapturedPreviewPlanner (file NOT touched by this task)
```
