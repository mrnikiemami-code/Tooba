# TB-P10-T004-R21-R1 — Concurrency

`unique(cart_id)` plus reservation idempotency `cc-{cart}-{line}`:

- Loser of checkout unique gets `checkout.conflict`, rolls back uncommitted work, returns winner snapshot.
- Loser of reservation unique gets `inventory.reservation.conflict` (aborted SQL state is not reused); after `CloseConnection` the winner checkout is returned.

`CheckoutOrderFoundationTests` concurrent different-key and same-key submits: one Checkout, one conversion, one stock decrement.
