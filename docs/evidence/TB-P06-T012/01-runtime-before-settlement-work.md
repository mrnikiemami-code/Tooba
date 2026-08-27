# 01 — Runtime before settlement work (TB-P06-T012)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T011 (Returns & Refunds end-to-end) |
| Branch | `main` |
| Pipeline | BRIDGE-WAKE-V1 / `tooba-main` |

## Baseline checks (before Settlement module)

| Check | Result |
|---|---|
| Backend restore/build | PASS (0 warnings, 0 errors) |
| Backend tests | 233+ pass (pre-Settlement) |
| PostgreSQL dev instance | running |
| `/health/live` | 200 |
| `/health/ready` | 200 |

## Notes

- No `settlement` schema or settlement HTTP endpoints at task start.
- Payment `payment.succeeded.v1` and Returns `refund.succeeded.v1` integration events already exist from T008/T011; Settlement consumes them in this task.
- Vendor wallet route existed as honest unavailable placeholder (TB-P05-T023); no live balance/accrual UI before this task.
- P05 gate deferred settlement/payouts to Later Product Phase — this task delivers the first live marketplace settlement foundation.
