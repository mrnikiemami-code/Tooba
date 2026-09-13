# TB-P10-T004-R21-R1 — Anti-Pattern Scan

CLEAN:

- Hide is not cancel; cancel is not UI-only hide.
- Hide SoT is `order.pending_payment_card_hides`, not localStorage-only.
- Cart Converted is inside `TransactionScope` before `Complete()`.
- FE persist/detach only after HTTP success.
- Payment initiate does not submit checkout.
- Convert-fail / unique-loser paths rely on transaction rollback, not test DB cleanup.
- `ReleaseAcquiredAsync` runs only if the outer catch still sees acquired ids after a non-conflict failure; conflict/reservation-unique exits via rollback + winner read on a fresh connection.
- No Login Gate / MaxOpenUnpaidOrders / TB-P10-T005.
