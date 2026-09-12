# TB-P10-T004-R11 — Focused Validation

| Check | Result |
| --- | --- |
| PaidProjectionFinancialTests | 5 pass |
| PaymentShippingAllocation + PaymentFoundation + UnpaidOrderExpiry + QuantityDecimal | pass (Host filter 24) |
| recovery guard | 4/4 (`CURRENT_TASK_ID=TB-P10-T004-R11`) |
| Runtime A–G + shipping-zero | ok=true (`r11-runtime-raw.json`) |
| git diff --check | clean |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T005 | not created |
