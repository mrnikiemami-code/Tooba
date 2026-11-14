# TB-267 — layout before / after metrics

All "after" numbers are measured on the **served** build `EwxE4g90_VArF5iXhm6oO` (Next on 3000) against the user's own read-only Order B
`01a1db2f-e36e-7000-b7d3-afb2fe86e81a` with the exact queue locator
`?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue`.
"Before" numbers are static source counts at base `500c6583c` plus the rejected-screenshot description recorded in the issued task; no previous bundle was re-rendered.

## Microtext (root cause of «نوشتهها قابل تحویل نیست»)

Counted literal Tailwind sizes in the files that render this surface, at base vs now:

| File | `text-[10px]` | `text-[11px]` |
| --- | --- | --- |
| `app/admin/admin-order-items-shipping-panel.tsx` | 7 → **0** | 16 → **0** |
| `app/admin/admin-order-operational-delivery-section.tsx` | 0 → **0** | 10 → **0** |
| `features/order-operational-groups/ui/order-operations-view.tsx` | 0 → **0** | 1 → **0** |
| `features/order-operational-groups/ui/operational-delivery-timeline.tsx` | 0 → **0** | 0 → **0** |
| **total** | **7 → 0** | **27 → 0** |

Measured on the served page inside `main`: elements with text and a computed font size **below 12px = 0**;
`h1` `20px/28px`, target/recipient values `14px/20px`, parcel title `14px/20px`, tracking code `14px/20px`.

## Geometry

| Viewport | After (measured) | Before (rejected) |
| --- | --- | --- |
| 1440 | overview is ONE card, two columns: work target **671px** × 175px and recipient **384px** × 175px side by side; operational group **1086px**; each parcel card **1052px**; document overflow **0**; clipped elements **0**; `main` controls below 44px **0** | two independent full-width cards, the target card mostly empty; no shared frame |
| 1024 | two columns **379px / 260px**, overflow **0** | single full-width stacked cards |
| 390 | stacked, both **332px** wide, overflow **0**, off-screen controls **0**, controls below 44px **0**, clipped **0** | tiny nested card inside a full-width empty outer card; phone pushed to the opposite edge |

Parcel cards were previously squeezed next to a one-line product summary; now the contents block is compact and **above** a full-width parcel list, so parcel history is no longer compressed into microtext.

## Copy audit on the served route

| Check | Result |
| --- | --- |
| raw `Asia/Tehran` in visible text | **0** |
| ISO `YYYY-MM-DD` in visible text | **0** |
| GUID-like `xxxxxxxx-xxxx-` in visible text | **0** |
| human zone label «به وقت تهران» | **2** (overview carrier row; group deadline block header) |
| Jalali dates | `۲۶ آبان ۱۴۰۵`, `۲۴ آبان ۱۴۰۵` |
| time ranges | isolated LTR `bdi` with `tabular-nums`, Persian digits (`۰۹:۰۰–۱۰:۰۰`, `۰۸:۰۰–۲۰:۰۰`) |

## Finance

| Surface | Money labels found |
| --- | --- |
| `/fa/admin/orders/{id}/operations` (shipping-only) | `مبلغ`, `جمع کل`, `مالیات`, `قیمت`, `کارمزد`, `تومان`, `ریال` → **0 matches**; table headers `# / محصول / تعداد / انتخاب تعداد / وضعیت / تخصیص ارسال / مهلت مرجوعی / عملیات` |
| `/fa/admin/orders/{id}` (full order) | still shows «مبلغ کل سفارش», «مبلغ قابل پرداخت», «مبلغ ناخالص سفارش» — unchanged |
