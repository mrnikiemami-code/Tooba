# TB-P10-T004-R12 — Settings effective policy

Resolution (R10 `CommerceHoldPolicy`): Payment Method override > Store `store_hold_policy_settings` > platform `Payment:Gateway` / `Cart:PersistenceHours`.

| Setting | Platform | Store | Method | UI |
| --- | --- | --- | --- | --- |
| Cart persistence | Cart:PersistenceHours=168 | optional | n/a | «مدت نگهداری سبد خرید» — not a hold |
| Online hold/timeout | OnlinePaymentHoldHours | store row | fake override | «مهلت پرداخت آنلاین» |
| Manual initial | ManualPaymentInitialHoldHours | store row | method override | «مهلت ثبت اطلاعات پرداخت کارت‌به‌کارت» |
| Manual review | ManualPaymentReviewHoldHours | store row | method override | «مهلت بررسی پرداخت کارت‌به‌کارت» |

GET/PUT `/v1/admin/settings/hold-policy` roundtrip 200 (runtime `settings-roundtrip`). Cart TTL uses `ICartPersistenceHoursSource`; unpaid worker uses `UnpaidTimeoutAt` from the same resolver. No new magic TTL constants added in R12.
