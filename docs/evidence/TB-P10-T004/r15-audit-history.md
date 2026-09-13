# R15 audit history

Events: Started, StartedAfterRetry, Expired, ReleasedByCancel, ReleasedByPolicy, ReacquireRequested, ReacquireFailed, CommittedPaid, RetryLimitReached, ManualReviewTransitioned.

Stored in `order.reservation_cycle_events` (append-only). Existing Admin operational history is not redesigned; cycle events are the canonical reservation-cycle audit.
