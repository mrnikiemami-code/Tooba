# TB-P10-T004-R10 — Expiry worker

`UnpaidOrderExpiryHostedService` (`Tooba:UnpaidOrderExpiry`).

- Indexed query on `(status, unpaid_timeout_at)`
- `FOR UPDATE SKIP LOCKED`
- Eligible: Created / Pending / Failed with due `UnpaidTimeoutAt`
- Domain `ExpireUnpaidTimeout` no-ops Succeeded, Cancelled, refund, and evidence-submitted Pending
- Releases Order holds via existing `ReleaseReservationsAfterManualRejectAsync` (history Released retained)
- Idempotent second pass
