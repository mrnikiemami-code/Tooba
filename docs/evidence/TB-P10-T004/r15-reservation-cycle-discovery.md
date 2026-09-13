# R15 reservation-cycle discovery

| Operation | Existing reservation state | New reservation? | Reason | New cycle? | Timer reset allowed? |
| --- | --- | --- | --- | --- | --- |
| AddToCart / cart read | none | No | LOCK-SF-048 | No | n/a |
| Order commit (`ReserveCartLinesForOrderAsync`) | none / leftover cart hold | Yes, new Held + Initial TTL | InitialPayment or ManualInitial | Cycle #1 | No after start |
| Online initiate / fail / retry inside Active | Held valid | No | Payment Attempt ≠ cycle | No | **Forbidden** |
| Manual evidence → review | Held Active | No (promote same row) or new if Released | ManualReview transition | Same cycle if Active; new only after ended | Review TTL is an explicit phase change |
| Unpaid / cycle expiry | Held past ExpiresAt | Release only | worker + `CloseExpiredDue` | Close Expired | No resurrection |
| Expired retry | Released historical | Yes via `EnsureOrderSupply` | RetryAfterExpiry | **Yes, N+1** if stock + under max | New ExpiresAt from Retry minutes |
| Retry without stock | Released | No | ReacquireFailed event | **No numbered cycle** | n/a |
| Max cycles reached | any ended | No | `inventory.reservation.retry_limit_reached` | No | n/a |
| Payment success | Held | Commit durable / EnsurePaidDurable | CommittedPaid | Close current; LatePaymentRecovery only if none Active | ExpiresAt cleared on inventory |
| Cancel last open seller order | Held | Release | ReleasedByCancel | Close | No |
| Restore | Released historical | New durable | Restore | New then CommittedPaid | No resurrect |
| Historical recovery | Released | New | HistoricalRecovery | New; CommittedPaid if class B | No resurrect |
| Settings change after start | snapshot on cycle | No | historical meaning frozen | No | No |

Canonical reacquire remains `EnsureOrderSupply`. Callers do not invent replacement holds except restore/recovery paths that already existed and still create **new** rows.
