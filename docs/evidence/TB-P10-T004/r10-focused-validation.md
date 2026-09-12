# TB-P10-T004-R10 — Focused Validation

| Check | Result |
| --- | --- |
| UnpaidOrderExpiryTests | 10 pass |
| Combined Host filter (UnpaidOrderExpiry + CartLifetime + Settings + OrderSupply + PaymentFoundation + PaidOrderReservation + OrderInventoryRecovery + OrderSupplyUx + QuantityDecimal + ManualPayment) | 55 pass |
| catalog-units-screen + customer-api | 12 pass |
| recovery guard | 4/4 (`CURRENT_TASK_ID=TB-P10-T004-R10`) |
| Runtime A–J | ok=true (`r10-runtime-raw.json`) |
| git diff --check | clean |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T005 | not created |
