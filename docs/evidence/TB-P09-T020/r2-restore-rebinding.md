# R2 Restore Rebinding — TB-P09-T020-R2

Composer order fixed:

1. `RestoreCancelledCheckoutAsync` (reacquire Held reservation on OrderLine)
2. `ReactivateAfterOrderRestoreAsync` (rebind Fulfillment from handoff + ReadyToFulfill)

Domain: `FulfillmentItem.RebindActiveReservation` / `FulfillmentUnit.RebindActiveReservations` — idempotent; skips consumed/fully shipped lines.

Cancelled pre-dispatch shipments stay Cancelled. Old Released reservation untouched.
