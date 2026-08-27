# 04 — Refund adjustment contract (TB-P06-T012)

## Trigger

Integration event: `RefundSucceededIntegrationEvent` (`refund.succeeded.v1`)

Handler: `SettlementRefundSucceededHandler` → `SettlementDirectory.AdjustFromRefundAsync`

## Adjustment flow

1. Inbox dedupe on `eventId` (`settlement.refund_inbox`)
2. Load return snapshot via `ISettlementReturnsReader`
3. Idempotency key: `refund-adjustment:{returnRequestId}`
4. Post debit entry reversing seller net exposure for refunded amount
5. Record inbox + save

## Bridge contract (Returns → Settlement)

`ReturnSettlementBridge` / `IReturnSettlementReader` exposes:

- `ReturnRequestId`, `SellerOrderId`, `SellerPartyId`
- `RefundAmount`, `Currency`

No Returns DbContext leakage into Settlement queries.

## Relationship to Payment

Refund money movement remains in Payment/Returns modules. Settlement only adjusts seller ledger (Debit) after refund succeeds — does not initiate provider refunds.

## Idempotency

- Event inbox primary key on `event_id`
- Entry idempotency key per return request
- Verified in `Settlement_lifecycle_applies_commission_refund_and_payout_safety` integration test
