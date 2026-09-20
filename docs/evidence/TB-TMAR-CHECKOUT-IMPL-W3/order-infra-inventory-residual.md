# Order.Infrastructure → Inventory.Application residual (audit only)
- Still present for Cancel/Restore/PaymentBridge paths (W2 residual)
- Not checkout submit critical (submit uses Inventory.Contracts)
- Compensation-related potential later; NOT activated in W3
- Should move to Inventory.Contracts in a later task (W4 candidate)
- Does not block Cart conversion seam
