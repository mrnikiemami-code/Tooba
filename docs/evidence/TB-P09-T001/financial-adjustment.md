# Financial adjustment (TB-P09-T001)

`SettlementEntry.PostDebitFromRefund` with `SourceType = refund`.
`AdjustFromRefundAsync` is idempotent (refund inbox + idempotency key).
Completed statements are not mutated by refund adjustment.
