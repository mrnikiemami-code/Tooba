# Cross-context transactions — TB-TMAR-CONTRACTS-W2

Machine-readable: `cross-context-transactions.json`

## Summary
- Total BeginTransaction/TransactionScope sites scanned (non-test): 15
- CROSS_CONTEXT_SHARED_ACID: 1 — `CheckoutDirectory.cs` TransactionScope spanning Order + Inventory reserve + Cart convert
- LOCAL_MODULE_TRANSACTION: 13
- NEEDS_REVIEW: 1 (outbox/building-blocks technical)

## Critical debt
Checkout shared-ACID TransactionScope remains migration debt. Redesign deferred to `TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN` (design-first).
