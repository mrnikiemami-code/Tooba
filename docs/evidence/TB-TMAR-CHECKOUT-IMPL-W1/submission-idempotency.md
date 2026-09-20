# Submission idempotency

- Reuses existing client `SubmitCheckoutCommand.IdempotencyKey` as `SubmissionIdempotencyKey`
- Durable unique index on submission key
- BeginAsync returns succeeded process for duplicate key after PaymentPending
- Existing checkout lookup by idempotency OR cartId retained
- No in-memory/cache-only authority
