# R19 financial invariants

`PaidProjectionFinancialTests` PASS. Reservation retry does not allocate a second payable.

- Payable = seller merchandise + StoreShipping
- StoreShipping excluded from seller payout
- Payment success projection idempotent
- Manual/online share one Order payable
- Decimal quantity 1.25 exact (`ReservationLifecycleIntegrationGateTests` + R15)
- Single-seller and multi-seller proofs remain the R11 suite (no first-seller shortcut)
