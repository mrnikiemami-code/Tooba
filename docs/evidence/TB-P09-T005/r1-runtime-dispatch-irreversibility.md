# R1 Runtime — Dispatch Irreversibility + Delivery (Scenario F)

Replacement shipment `8fdd9430-d547-4490-a5c6-785c80e48cbb`:

| Step | HTTP | Notes |
| --- | --- | --- |
| assign_tracking | 200 | `TRK-R1-…` |
| dispatch_shipment | 200 | status → Dispatched |
| cancel_shipment after dispatch | 400 | FA: این عملیات در وضعیت فعلی سفارش مجاز نیست |
| unpack allocated/shipped qty | 400 | FA: بازگشت از بسته‌بندی برای تعداد تخصیص‌یافته یا ارسال‌شده مجاز نیست |
| deliver_shipment | 200 | shipment status=Delivered |
| line shipped | 2 | packed remaining unshipped kept |

Fulfillment status after partial deliver: InTransit.

## Cancellation regression

`cancel` on same seller after dispatch/deliver → HTTP 400  
`order.cancel.forbidden: لغو از این وضعیت سفارش/ارسال مجاز نیست.`

Simple seeded order dispatch hit inventory reservation mismatch (`مصرف رزرو با موجودی هم‌خوان نبود`) — pre-existing seed/reservation data issue; irreversibility already proven on multi-seller path. No T005 code change required.
