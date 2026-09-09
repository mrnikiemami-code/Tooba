# R2 Reservation Discovery — TB-P09-T020-R2

`fulfillment.items.reservation_id` is a **denormalized active reference** copied from Order handoff at Fulfillment create time. It is not an immutable historical snapshot and not Inventory ownership.

Cancel path (`CheckoutDirectory.Cancel*`): releases OrderLine reservation (Held → Released).

Restore path (`RestoreCancelledCheckoutAsync`): reacquires Inventory, `OrderLine.ReplaceReservation(newId)`. Fulfillment was previously left on the Released id.

Dispatch (`ConsumeInventoryForShipmentAsync`): consumes `fulfillmentItem.ReservationId` when shipped qty covers ordered — fails if Released (`inventory.reservation.not_active`).

Runtime evidence before fix on checkout `01a086d4-9b68-7000-a2f2-13fd249ba281`: fulfillment reservation Released; order_line reservation Held (replacement).
