# TB-P10-T004-R10 — Performance

Worker: bounded batch + `SKIP LOCKED` + index `(status, unpaid_timeout_at)`. No full-table scan per tick. No N+1 SupplyStatus in the worker. Admin Settings and customer retry are on-demand. No per-order frontend timer.
