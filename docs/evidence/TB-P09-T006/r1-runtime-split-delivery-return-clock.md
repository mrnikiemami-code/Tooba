# R1 Runtime — Split Delivery Return Clock

Checkout `01a07bd2-5ef4-7000-b770-a2173d8ae25c`, line `01a07bd2-5efc-7000-aaf6-7909036a48e5`, qty=5.

| Slice | Shipment | Qty | Method | State after A-only |
|-------|----------|-----|--------|--------------------|
| A | `781a155f-332c-4529-81b3-6dc7ed56be9b` | 2 | post | Delivered |
| B | `c5de09d0-fe21-4bfe-b235-6eb412e07b28` | 3 | store_courier | Created (undelivered) |

After A only:

- UI: `تحویل‌شده ۲: تا ۱۴۰۵/۰۶/۲۳ · تحویل‌نشده ۳: ساعت مرجوعی شروع نشده`
- Remaining: `تحویل‌شده ۲: ۷ روز باقی‌مانده · تحویل‌نشده ۳: قبل از تحویل`
- `returnStatusCode=partial_eligible`
- Does **not** claim all 5 share one deadline

After B delivered later:

- UI: `تحویل‌شده ۲: تا ۱۴۰۵/۰۶/۲۳ · تحویل‌شده ۳: تا ۱۴۰۵/۰۶/۲۳`
- `returnStatusCode=eligible`
- Each slice starts return timing from its own delivery timestamp
