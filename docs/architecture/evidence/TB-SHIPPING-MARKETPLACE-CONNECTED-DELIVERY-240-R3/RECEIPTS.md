# RECEIPTS — 240-R3

## Environment (verified, not assumed)
Host: `D:\temp\tb240r3-build\out`, `http://127.0.0.1:5190`, Development / Marketplace / Tenant NULL /
`DeploymentId=tb239-marketplace`, DB `tooba_marketplace_tb239`.
Old listener PID 23944 stopped by port-owner lookup; `/health` 200 after restart with the same launch env.
Task FE: `http://127.0.0.1:3102` (own port; reuses no user process).
Browser: installed Chrome, own profile `D:\temp\tb240r3-review-chrome`, CDP 9341.

## Migration (normal runner) — actual target history
```
hist=20261028140000_AddCapturedGroupLegacyScopedUniqueIndexes
ux_cart_shipping_groups_cart_seller_method :: CREATE UNIQUE INDEX … (cart_id, seller_party_id, method_code)
    WHERE (accepted_snapshot_id IS NULL)
ux_order_shipping_groups_checkout_seller_method :: CREATE UNIQUE INDEX … (checkout_id, seller_party_id, method_code)
    WHERE (accepted_snapshot_id IS NULL)
```
Order history on this target before the new one ends at
`20261027230000_AddCapturedSelectionCouponContext` (231/232/236 applied here, not on shared `tooba_alpha`).

## Legacy collision control (rolled back; 0 rows left)
```
BEGIN
INSERT 0 1
ERROR: duplicate key value violates unique constraint "ux_cart_shipping_groups_cart_seller_method"
DETAIL: Key (cart_id, seller_party_id, method_code)=(01a181e2-…, 01a181b0-5c87-…, poststandard) already exists.
legacy_rows=0
```

## Captured coexistence (real rows, same cart + same seller + same method)
```
01a18205-63a7-7000-a067-b76a4060d879 | 01a181b0-5c87-… | poststandard | 100000.00 | snap 01a181ca-e8f8-… | owner 1
01a18205-63a8-7000-be9d-0142a8337c06 | 01a181b0-5c87-… | poststandard |  65000.00 | snap 01a181de-0c80-… | owner 2
01a18205-63a8-7000-8c56-33876f24555c | 01a181b0-5c9a-… | poststandard |  49500.00 | snap 01a181de-0e59-… | owner 2
total_groups=3  revision=1
```

## Flow
```
projection=200 cartVersion=7
  g=cap|platform|<SellerA> goods=2016500 payable=100000
  g=cap|seller|<SellerA>   goods=1580500 payable=65000
  g=cap|seller|<SellerB>   goods=1951100 payable=49500
selection=200 revision=1 (3 groups)
payment-draft=200 payable=5762600 shipping=214500 paymentState=PendingPayment rev=1 canPay=True
start-payment=200 checkout=01a18209-cd78-7000-a9b6-02dadf377e55 replayed=False
  payment=01a18209-d01d-7000-a1f0-d59a53b1124f status=Pending provider=manual amount=5762600
  attempt=01a18209-d01f-7000-a54f-179e532cc2dc reservationCount=3 guaranteedUntil=2026-10-28T12:24:03Z
```
First attempt (before the materializer fix) — measured RED:
```
start-payment=400 errorCode=order.shipping.group.seller_method.duplicate
```

## Persisted order (DB readback)
```
checkout=01a18209-cd78-… cart=01a181e2-… shipping=214500.00
sellerorders=2 orderlines=3 grouplines=3
og=01a18205-63a7-… sellerA charge=100000.00 snap=01a181ca-e8f8-… owner=1 pay=100000.00 date=2026-10-31
og=01a18205-63a8-…be9d sellerA charge= 65000.00 snap=01a181de-0c80-… owner=2 pay= 65000.00 date=2026-10-29
og=01a18205-63a8-…8c56 sellerB charge= 49500.00 snap=01a181de-0e59-… owner=2 pay= 49500.00 date=2026-10-31
payment=… status=Pending provider=manual amount=5762600.0000 attempts=1 cartstatus=Converted
```

## Buyer own order read
`GET /v1/storefront/checkout/{checkoutId}?cartId=…` → 200 (2 seller orders, payable 5,762,600, shipping 214,500,
`canInitiatePayment=true`). Without `cartId` the endpoint returns 500 `platform.unexpected`
(`Required parameter "Guid cartId" was not provided`) — pre-existing endpoint contract, not introduced here.

## Focused test run
`dotnet test Tooba.Order.Tests --filter "CapturedObligationKeyPlanningTests|CartShippingGroupPlannerTests"`
→ **Passed 20/20** (first run measured 19/20: the repeated-line fact threw
`DuplicateSellerMethod` instead of `DuplicateLineMembership`, i.e. the whole-commitment key already collapses two
obligations over the same line; the expectation was corrected to the truthful code, the guard was not weakened).

## UI
```
LOGIN {"status":200,"me":{"userId":"01a181b6-…","edition":"Marketplace"}}
payment-1440: innerWidth=1440 clientWidth=1440 scrollWidth=1440 visualWidth=1440
payment-390 : innerWidth=390  clientWidth=390  scrollWidth=390  visualWidth=390
payment-360 : innerWidth=360  clientWidth=360  scrollWidth=360  visualWidth=360
page text: «پرداخت سفارش / روش پرداخت فعال فروشگاه را انتخاب کنید …
            دسترسی به اطلاعات پرداخت این سفارش تأیید نشد. لطفاً از بخش سفارش‌ها دوباره وارد پرداخت شوید.»
URL: http://127.0.0.1:3102/fa/payment?checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55
```
The access proof (`resolveCommittedCheckoutAccess`) is browser-local; this order was created through the APIs, so
the guard fails closed. No bypass was attempted.

## Not run at the deadline
Publication-change immutability read, coupon (partial/invalid/cleared), foreign-owner denial on this order,
UI double-click/failed-refresh retry, provider-side exactly-once.
