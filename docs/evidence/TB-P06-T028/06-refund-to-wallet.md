# 06 — Refund to wallet

## Destination

`RefundDestination` enum on `ReturnRequest`:

- `OriginalPayment` (default, backward compatible)
- `Wallet`

Accepted on customer create and seller approve.

## Processing choice

When destination = `Wallet`:

- credit via `CreditRefundAsync` with key `wallet-refund-credit:{returnRequestId:D}`
- **PSP refund gateway is not called** (avoids double-credit)
- RefundAttempt provider ref = `wallet-refund:{returnRequestId}`

Admin `retry-refund` remains idempotent because ledger credit is create-if-absent by unique key.

## Partial

Credits exact `RefundAmount`; each ReturnRequestId once; existing qty caps unchanged.
