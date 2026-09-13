# R15 concurrency

- PrepareStart returns the existing Active row (same-key / double submit).
- Unique `(checkout_id, cycle_number)` and unique Active-per-checkout.
- `StartAsync` on `DbUpdateException` reloads the winner.
- Retry vs success: paid close is CommittedPaid; later retry sees no Active and hits paid initiation guard (R14) / no new pay cycle.
- Retry vs cancel: ReleasedByCancel; Ensure then fails or creates a new cycle only if restore/retry policy allows — cancel of last seller order closes the cycle first.
- Expiry worker `CloseExpiredDue` then unpaid release; CloseActive is idempotent if already Expired.
- Failed reacquire does not insert a numbered cycle.
