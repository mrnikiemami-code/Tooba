# R15 reservation-cycle domain

Entity: `order.reservation_cycles` + append-only `order.reservation_cycle_events`.

Fields: CycleNumber, CheckoutId, Reason, StartedAt, ExpiresAt, EndedAt, Status, Actor, CorrelationId, PaymentAttemptId (optional, not identity), reservation id list, policy snapshot (hold minutes, max cycles, source).

Statuses: Active, Expired, ReleasedByCancel, ReleasedByPolicy, ReacquireFailed (event-only; not a numbered close), CommittedPaid.

Reasons: InitialPayment, RetryAfterExpiry, ManualInitial, ManualReview, Restore, HistoricalRecovery, LatePaymentRecovery.

Reason ≠ Status. CycleNumber is unique per checkout. One Active row (partial unique index). Failed reacquire writes an event without CycleNumber.
