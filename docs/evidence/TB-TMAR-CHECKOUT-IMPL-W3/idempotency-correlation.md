# Idempotency / correlation
- ProcessId stable via CheckoutProcess / BeginAsync(idempotencyKey)
- CorrelationId = process.CorrelationId ?? checkout-submit:{idempotencyKey}
- Passed into CartConversionRequest and Inventory reservation request
- Duplicate submit cannot double-convert into duplicate business effects (Converted short-circuit + unique checkout constraints)
- No cache-only authority
