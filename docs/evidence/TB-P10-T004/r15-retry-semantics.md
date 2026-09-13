# R15 retry semantics

Inside Active: `CorrelatePaymentAttempt` may store an attempt id; `ExpiresAt` is unchanged. `EnsureRetryAfterExpiryAsync` with an Active cycle calls Ensure **without** `ReviewExpiresAt` so Inventory cannot promote/extend.

After close: count created vs Max. If at limit → `inventory.reservation.retry_limit_reached` + FA. Else `ReacquireRequested` → `EnsureOrderSupply(EnsureUnpaidRetryHold)` → success starts Cycle N+1 with Retry minutes; failure is `ReacquireFailed` with no new Active cycle and no Payment reopen.
