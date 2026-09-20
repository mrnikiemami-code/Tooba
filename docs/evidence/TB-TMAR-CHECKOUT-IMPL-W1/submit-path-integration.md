# Submit path integration

- Optional `ICheckoutProcessTracker` on `CheckoutDirectory`
- Milestones marked around existing reserve/persist/convert steps
- Process rows SaveChanges inside the same TransactionScope as order/cart
- No participant commit split; no Saga runner; response semantics unchanged
