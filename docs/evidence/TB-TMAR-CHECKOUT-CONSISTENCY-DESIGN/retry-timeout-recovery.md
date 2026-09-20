# Retry / timeout / recovery (design)

- Auto-retry: Outbox dispatch; idempotent reserve/release; payment webhook processing; unpaid expiry jobs
- Do not blind-retry: PRICE_CHANGED, tax no-rule, abuse limits, non-idempotent PSP capture without key
- Timeout ownership: reservation ExpiresAt / reservation-cycle policy; unpaid payment expiry; process-manager step deadlines (future)
- Stuck detection: workflow state age > policy; Held reservation without order link; PaymentPending past expiry
- Recovery: operator cancel unpaid; ReleaseExpiredBatch; manual ManualReview; replay Outbox
- Poison/DLQ: future messaging transport; today dispatcher + inbox
- Replay safety: inbox + idempotency keys required before extraction
