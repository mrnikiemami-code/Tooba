# TB-P10-T004-R23 — Churn audit

Immutable row: `order.checkout_reservation_commits` / `CheckoutReservationCommit`.

Fields: EventId, StoreId, CustomerId, OrderId, CheckoutId, ReservationCycleNumber (=1), OccurredAt, Source=`order-commit`.

Written only from `PrepareInitialCommit` inside the atomic Cycle #1 commit. Not deleted on cancel, hide, payment fail, or payment success.

Runtime: first Cycle #1 for checkout `01a09987-c777-7000-9025-9776bc703662` wrote exactly one event; Cycle #2 retry left that count at 1; payment retries did not add rows.
