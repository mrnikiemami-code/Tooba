# Residual dependencies after W4
## Order.Application foreign Application
- Promotion.Application (evaluator) — SAFE_CONTRACT_CANDIDATE / W5
## Order.Infrastructure foreign Application
- Cart.Application (ICartDirectory / CartConversionAdapter fallback) — SAFE_CONTRACT_CANDIDATE (non-lifecycle)
- Catalog.Application — NON_CHECKOUT / lookup
- Payment.Application — TEMPORARY_ACCEPTABLE (payment contracts already separate)
- Inventory.Application — REMOVED
