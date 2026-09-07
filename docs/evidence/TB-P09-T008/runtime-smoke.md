# Runtime Smoke

Date: 2026-09-07

Host `:5088` health: 200 `{"status":"ok"}` (Host: alpha.localhost)

`POST /v1/admin/fulfillments/work-queue/query` with DevActor header → HTTP 200
Sample row includes sellerDisplayName=فروشگاه آرمان, shippingMethodLabel=ارسال پیش‌فرض فروشگاه, status=Processing, orderReference=TB-…, queue-shaped fields present.

Focused validation:
- Host.Tests AdminFulfillmentWorkQueue + AdminDbNativeGrid: 17 passed
- FE admin-fulfillment-work-queue.test.ts: 8 passed
- recovery-staleness.guard.test.mjs: 3 passed
- git diff --check: clean (CRLF warnings only)
