# 10 — Payout Safety

Task: `TB-P06-T012`

Verified in `SettlementFoundationTests`:

- payout amount ≤ available balance (over-payout rejected)
- duplicate idempotency key returns same request
- failed payout does not mark settlement paid
- successful dev payout posts durable debit entry

Gateway references unique per attempt; retry safe.
