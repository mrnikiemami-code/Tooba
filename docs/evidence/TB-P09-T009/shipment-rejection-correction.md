# Shipment Rejection + Tracking Correction

Pre-dispatch `cancel_shipment` reused (`Created` even with tracking). Cancelled shipment is kept; tracking remains; open allocations release; replacement shipment is allowed.

`correct_tracking` only on `Created` + existing tracking + not dispatched. Old value stored in `PreviousTrackingReference`. After dispatch: `fulfillment.tracking.locked_after_dispatch`.

Assign-tracking still refuses silent rewrite.
