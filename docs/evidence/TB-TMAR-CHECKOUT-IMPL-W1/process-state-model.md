# Process state model

- Entity: `Tooba.Order.Domain.CheckoutProcess`
- Status enum: Started → Validating → InventoryReserving → OrderPersisting → CartCommitting → PaymentPending | Failed
- Keys: ProcessId, SubmissionIdempotencyKey (unique), CartId, CheckoutId?, CorrelationId, StartedAt, UpdatedAt, FailureCode?
- Separate from SellerOrder business status
- Guarded transitions; PaymentPending is terminal for submit success
