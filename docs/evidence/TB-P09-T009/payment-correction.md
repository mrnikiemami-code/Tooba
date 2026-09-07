# Payment Correction

`restore_deposit` — «بازگرداندن به انتظار تأیید واریز»

Allowed when: manual/card-to-card, latest payment Failed after `MANUAL_DEPOSIT_REJECTED`, no succeeding success, no dispatched/delivered shipment, no completed refund.

Path: `CustomerPayment.RestoreRejectedManualToPending` → new Initiated attempt, status Pending. No `PaymentSucceededDomainEvent`. No DB status patch.

Idempotent: second call while Pending after rejection returns the same Initiated attempt.

`تأیید واریز` / `رد واریز` reappear because `ConfirmDepositEligible`/`RejectDepositEligible` are Pending+manual again.
