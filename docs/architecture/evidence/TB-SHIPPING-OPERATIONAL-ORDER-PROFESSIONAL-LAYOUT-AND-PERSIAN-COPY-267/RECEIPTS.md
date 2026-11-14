# TB-267 — RECEIPTS

All commands were run on Windows/PowerShell in `D:\Users\User\source\repos\SarvNewVer`. Time: 2026-11-14/15 (UTC+3:30).

## Change set (tracked)

```
 M src/frontend/app/admin/admin-order-items-shipping-panel.tsx
 M src/frontend/app/admin/admin-order-operational-delivery-section.tsx
 M src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
 M src/frontend/app/admin/admin-order-operations-view.mount.test.ts
 M src/frontend/app/admin/orders/[checkoutId]/operations/admin-order-operations-route-screen.tsx
 M src/frontend/features/order-operational-groups/ui/operational-delivery-timeline.tsx
 M src/frontend/features/order-operational-groups/ui/order-operations-view.tsx
?? src/frontend/lib/operational-format.ts        (new, pure presentation formatters)
```

Diffstat: 7 files changed, 550 insertions(+), 321 deletions(-), plus the new 1-file formatter module.
Backend: **no** change — no Host file, no endpoint, no contract, no validator, no handler, no migration, no `appsettings`, no capability.

Untracked leftovers that are **not** part of this change set and were deliberately not staged: `scripts/tb266r1-*.mjs` (scratch probe/seed scripts from the previous TB-266-R1 run). They are reported here rather than silently deleted or committed.

## Build / typecheck / tests (once each, after the final source change)

```
npx tsc --noEmit
  exit 0, no diagnostics

npx tsx --test app/admin/admin-order-operational-delivery.mount.test.ts \
                app/admin/admin-order-operations-view.mount.test.ts
  tests 68  pass 68  fail 0

npx tsx --test "app/admin/orders/*/operations/admin-order-operations-route.mount.test.ts"
  tests 5   pass 5   fail 0

npm run build
  compiled successfully, 0 errors
  BUILD_ID EwxE4g90_VArF5iXhm6oO
```

## Runtime (frontend only; backend untouched)

```
Old Next PID on :3000 stopped (7972, then the intermediate build's PID 12896)
Start: npx next start -p 3000            (D:\temp\tb267-next.log)
New PID: 12896 -> 3000 listening
GET http://127.0.0.1:3000/fa                                 200
GET .../_next/static/EwxE4g90_VArF5iXhm6oO/_buildManifest.js  200
GET .../_next/static/chunks/7825-7e11809067fa2056.js          200  (145848 B)
Host on 5088 was NOT touched, no Host publish, no migration, no DB connection change.
```

## Live read-only verification of the user's record

Route (GET only, no mutation, no dialog submit):

```
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations
  ?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb
  &shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c
  &from=queue
```

Endpoints the page itself called (read-only): `/v1/admin/access-control/me/capabilities`,
`/v1/admin/orders/{checkoutId}`, `/v1/admin/shipping-services/shipments/capabilities?checkoutId=…`,
`/v1/admin/shipping-services/shipments?checkoutId=…`, `/v1/admin/orders/{checkoutId}/operations`.

### 1440 × 1600

```
vw 1440, document overflow 0
operational group        1086 × 699
parcel card 1            1052 × 237
parcel card 2            1052 × 117
overview: target 671 × 175 @x=449 ; recipient 384 × 175 @x=49
clipped leaf elements                0
controls in main below 44px          0
elements with text below 12px        0
h1 20px/28px ; values 14px/20px ; parcel title 14px/20px ; tracking 14px/20px
```

### 1024 × 900

```
vw 1024, document overflow 0
overview: target 379 @x=325 ; recipient 260 @x=49
```

### 390 × 900

```
vw 390, document overflow 0
overview: target 332 × 238 @x=29 ; recipient 332 × 175 @x=29   (stacked, full width)
clipped leaf elements 0 ; off-screen controls 0 ; controls in main below 44px 0
```

### Copy audit on the served text

```
raw Asia/Tehran        0
ISO YYYY-MM-DD         0
GUID-like xxxxxxxx-…-  0
«به وقت تهران»         2   (overview carrier row + group deadline block header)
Jalali examples        ۲۶ آبان ۱۴۰۵ / ۲۴ آبان ۱۴۰۵
time ranges            ۰۹:۰۰–۱۰:۰۰ / ۰۸:۰۰–۲۰:۰۰ in isolated LTR bdi + tabular-nums
```

### Finance audit

```
shipping-only route innerText money terms (مبلغ|جمع کل|مالیات|قیمت|کارمزد|تومان|ریال) : 0 matches
shipping-only table headers: # | محصول | تعداد | انتخاب تعداد | وضعیت | تخصیص ارسال | مهلت مرجوعی | عملیات
full order page /fa/admin/orders/01a1db2f-… : مبلغ کل سفارش / مبلغ قابل پرداخت / مبلغ ناخالص سفارش present
```

### Microtext before → after (literal Tailwind sizes in the rendering files)

```
admin-order-items-shipping-panel.tsx              text-[10px] 7→0   text-[11px] 16→0
admin-order-operational-delivery-section.tsx      text-[10px] 0→0   text-[11px] 10→0
order-operations-view.tsx                         text-[10px] 0→0   text-[11px]  1→0
operational-delivery-timeline.tsx                 text-[10px] 0→0   text-[11px]  0→0
```

## Explicit non-actions

- No create / dispatch / deliver / return receipt / resend / replacements / damaged resolution / courier return / refund / payment / cancel / save / publish on `01a1db2f-e36e-7000-b7d3-afb2fe86e81a`, nor on any other order.
- No `UPDATE`/`INSERT`/`DELETE`/DDL, no migration, no backup/restore, no reseed, no cleanup, no provider call, no waybill.
- No screenshot file committed (the two PNGs that existed in the evidence folder were removed; the task forbids screenshot artifacts).
- No global typography, sidebar or grid sweep; no `tailwind.config.ts` change. The `lg:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.8fr)]` utility is confirmed present in the emitted CSS because it was already used elsewhere in the compiled output.
