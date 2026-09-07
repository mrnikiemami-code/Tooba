# R3 admin confirmation

Ops: `confirm_deposit` / `reject_deposit` (FA تأیید واریز / رد واریز) when latest payment is manual+Pending.

Permission: `payment.reconcile`.

Also `POST /v1/admin/payments/{id}/confirm-deposit|reject-deposit`.

Confirm → ApplyVerifiedSuccess → outbox → Order Paid + ReadyToFulfill → mark_processing appears.
