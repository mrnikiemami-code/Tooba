# Order.Application hub decomposition plan (no refactor now)

Current App→App edges (baseline): Order→Cart, Order→Inventory, Order→Promotion. Offer/Pricing/Tax already Contracts.

| Edge | Class | Plan |
|---|---|---|
| Order→Cart | replace with Contracts + PM command | W2 |
| Order→Inventory | replace with Contracts + PM command | W2 |
| Order→Promotion | Contracts evaluate port | W1 |
| Offer/Pricing/Tax Contracts | retain sync query | W1 keep |
| Payment/Fulfillment | integration events (already post) | W3+ |

Phases:
- W1: low-risk query Contracts (Promotion) + workflow state/idempotency primitives in monolith
- W2: workflow commands behind Contracts still in-process
- W3: Process Manager cutover; remove TransactionScope
- W4: service extraction readiness
