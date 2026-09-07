# R1 Runtime — Split Delivery Return Clock

Checkout `01a07bcb-0292-7000-8154-5b8eccf0d2ac`, line qty=5.

| Slice | Shipment | Qty | State |
|-------|----------|-----|-------|
| A | `060b7abd-ffb4-4ee9-9fa0-cd97fec2e46e` | 2 | Delivered |
| B | `2512485a-057f-4316-b193-7bc0cd7f246c` | 3 | Delivered (later) |

After A only:

- UI: `تحویل‌شده ۲: تا ۱۴۰۵/۰۶/۲۳ · تحویل‌نشده ۳: ساعت مرجوعی شروع نشده`
- `returnStatusCode=partial_eligible`
- Eligibility: `deliveredQuantity=2`, `remainingReturnableQuantity=2`

After B:

- UI shows two delivered slices with their own deadlines
- Eligibility: delivered=5, remaining=5
- Undelivered remainder never started the clock before delivery
