# Promotion usage (Order)
| Location | Type | Role | Class |
|---|---|---|---|
| CheckoutDirectory (Infra) line build | IPromotionEvaluator.EvaluateAsync | checkout discount before tax | CHECKOUT_VALIDATION |
| Order.Application | ProjectReference only (no code use) | transitive for Infra | removable |
No promotion consume/release ledger lock in checkout path (evaluate-only).
