# Crash / retry characterization

- Duplicate begin after success returns same process (in-memory EF test)
- Process survives new DbContext
- Rollback/uncommitted Add not visible to new context
- Invalid transition after PaymentPending rejected
- Existing CheckoutOrderFoundationTests cover duplicate cart/idempotency + concurrent submit
