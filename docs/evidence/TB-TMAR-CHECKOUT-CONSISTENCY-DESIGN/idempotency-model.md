# Idempotency model (design only)

| Operation | Key source | Storage owner | Lifetime | Duplicate response |
|---|---|---|---|---|
| Checkout submit | IdempotencyKey + CartId | Order CheckoutGroup | durable | return existing snapshot |
| Inventory reserve | clientKey cc-{cart}-{line} (code) | Inventory | reservation lifetime | reuse Held if valid |
| Cart convert | CartId + version | Cart | durable status | Converted no-op / reconcile |
| Order create | CheckoutId / IdempotencyKey | Order | durable | existing |
| Promotion consume | (future if quota write) | Promotion | campaign window | reject/reuse |
| Payment authorize/capture | payment attempt / provider intent id | Payment + webhook inbox | durable | inbox dedupe |
| Wallet debit | payment/wallet ledger key | Wallet | durable | ledger dedupe |
| Compensation commands | compensationId / original reservationId | owner module | durable | no-op if already released |
| Integration consumers | inbox keys (Payment/Order/Fulfillment/Settlement/Inventory return) | consumer module | durable | skip |
