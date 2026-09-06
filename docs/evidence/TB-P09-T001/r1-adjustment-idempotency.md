# R1 — Adjustment idempotency

## Key

`refund-adjustment:{returnRequestId:N}` → `refund-adjustment:fbdc0eecede04deb9735ecf1f95b9724`

## Runtime

`AdjustFromRefundAsync` invoked twice with distinct event ids after Completed return/refund:

- Debit rows for seller order: **exactly 1**
- Debit net `315000` (gross refund `350000` − commission `35000`)
- Second call: inbox/idempotency short-circuit; no duplicate debit

## Automated

Settlement foundation test: Adjust then Adjust again → single Debit; statements unchanged.
