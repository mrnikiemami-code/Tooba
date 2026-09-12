# TB-P10-T004-R14 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| Frontend-only paid guard | CLEAN — Host overlays `CanInitiatePayment`; directory `HasSucceededPaymentForCheckoutAsync` is authoritative |
| Endpoint-specific inconsistent rules | CLEAN — initiate / evidence / manual-retry / unpaid-retry share Succeeded throw |
| Return 200 if already paid masking new initiation | CLEAN — new idempotency key after Succeeded is 409 `payment.already_succeeded`; same-key replay stays 200 |
| Duplicate attempt then cleanup | CLEAN — guard runs before `CustomerPayment.Open` / evidence mutate |
| Race window without backend check | CLEAN — Succeeded query before new Payment; `pending.Length==0` also `AlreadySucceeded` |
| Manual path bypassing capability | CLEAN — composer + directory both reject evidence/retry after Succeeded |
| Raw internal state/error in UI | CLEAN — mapped FA `پرداخت این سفارش قبلاً با موفقیت انجام شده است.` |
