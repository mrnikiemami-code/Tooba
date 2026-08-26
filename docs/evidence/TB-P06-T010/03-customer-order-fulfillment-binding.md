# 03 — Customer order detail binding

File: `src/frontend/app/customer-panel/orders/[checkoutId]/page.tsx`

- Loads live fulfillments alongside order detail
- Section «وضعیت ارسال و محموله‌ها» with loading / error / empty / data states
- Uses `FulfillmentSummaryCard` (shared Shopeiva-compatible card styling)
- No CSS shell changes outside new section block
