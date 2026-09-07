# Audit History

History drafts (human FA, no delete of prior rows):

- `order_cancelled` / `order_restored`
- `payment_deposit_rejected` («رد واریز») / `payment_deposit_restored` («بازگرداندن به انتظار تأیید واریز»)
- `shipment_cancelled` («عدم پذیرش مرسوله»)
- `tracking_corrected` (old → new)

Payment restore emits `payment.manual_deposit.restored.v1`. Tracking correction emits `shipment.tracking.corrected.v1`.
