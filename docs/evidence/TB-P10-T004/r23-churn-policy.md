# TB-P10-T004-R23 — Churn policy

Settings (Store catalog singleton):

- `ReservationCommitWindowMinutes` default 30
- `MaxCheckoutCommitsPerCustomerInWindow` default 3

Both integers, min 1, no silent clamp.

Consumes quota: each successful new checkout that creates Reservation Cycle #1 (`CheckoutReservationCommit`).

Does not consume extra quota:

- payment retry on the same Order
- Cycle #2+ `retry-unpaid`
- hide / cancel / later payment success (event stays until it ages out of the window)

Enforced before reservation. Error `checkout.reservation_commit_limit_reached` with FA copy, `currentCount`, `maxCount`, `nextAvailableAt` from oldest event in window + minutes.

Offer-level flash-sale cap is documented as future only.
