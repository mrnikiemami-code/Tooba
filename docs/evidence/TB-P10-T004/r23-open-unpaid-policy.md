# TB-P10-T004-R23 — Open unpaid policy

Canonical predicate: `OpenUnpaidOrderPredicate` / `IsOpenUnpaid`.

Statuses counted: `PendingPayment`, `Submitted`.

Not counted: `Paid`, `Cancelled`, `ReservationRequested`.

Scope: Store singleton settings + `Checkout.PlacedByUserId` (CustomerId). Count is SellerOrders joined to Checkouts. Hide table is never queried.

Setting: `MaxOpenUnpaidOrdersPerCustomer` default 2, min 1, max 20, no silent clamp.

Enforced in `CheckoutAbuseGate.EnsureCanStartInitialReservationAsync` before reservation. Error `checkout.open_unpaid_limit_reached` with FA copy, `currentCount`, `maxCount`.

Cancel frees the slot. Hide does not. Paid (after outbox projection) stops counting. Payment retries do not change the Order count.
