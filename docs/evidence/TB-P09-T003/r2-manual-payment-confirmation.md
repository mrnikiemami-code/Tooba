# R2 manual payment confirmation

- `ManualPaymentGateway` (`providerCode=manual`) registered non-Prod.
- Storefront initiate accepts `providerCode: "manual"`.
- Admin ops: `confirm_deposit` / `reject_deposit` (FA: تأیید واریز / رد واریز) via `payment.reconcile`.
- Also `POST /v1/admin/payments/{id}/confirm-deposit|reject-deposit`.
- Duplicate confirm: idempotent Succeeded / not double-credit (Verify short-circuits when already Succeeded).
- Wrong method → `payment.method.not_manual` human FA.
