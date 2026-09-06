# R2 — Idempotency

## Accrual

`SettlementPaymentSucceededHandler` → same `EventId` redelivery: single Credit (payment inbox).

## Adjustment

`SettlementRefundSucceededHandler`:

- same `EventId` redelivery → one Debit
- new `EventId`, same `returnRequestId` → still one Debit (`refund-adjustment:{returnRequestId}`)

Proven in `MarketplaceSettlementEventPathTests` and live marketplace smoke.
