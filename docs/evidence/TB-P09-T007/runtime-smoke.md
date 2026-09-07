# Runtime Smoke — TB-P09-T007

Host `:5088` + FE `:3000` (alpha.localhost, DevActor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`).

Verified:

- Paid order `01a07a63-ab32-7000-a62a-e67df880a8ce`: financialEvents includes CustomerReceipt / دریافت از مشتری (no duplicates).
- Order `01a07bd3-ee11-7000-b51f-c5e245220244`: CustomerReceipt once; operational pack scope «فروشگاه آرمان — ۱ قلم»; actor «توسط سیستم».
- Split checkout `01a07bca-857a-7000-8cd4-c70510cfc651` operational-history shows scoped summaries e.g. بسته‌بندی — فروشگاه آرمان — ۵ قلم; ایجاد مرسوله — اسنپ / پیک آنلاین — ۲ قلم; ارسال/تحویل with tracking.

Fixture limits (no full lifecycle replay):

- Admin returns list empty on this DB → live CustomerRefund / SellerRefundAdjustment not present.
- Settlement postedCredits=0 → live SellerPayout accrual not present.
- Projection/attribution/idempotency/FA labels covered by `AdminOrderFinancialHistoryTests`.

Artifacts: `r1-runtime-financial-events.json`, `r1-runtime-operational-history.json`.

USER_VISUAL_ACCEPTED=NO.
