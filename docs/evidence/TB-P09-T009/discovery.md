# Discovery

Whole-order `cancel` was projected once per seller (`sellerOrderId` set). `filterOperationsForScope("whole-order")` did not exclude `cancel`, so multi-seller menus showed «لغو سفارش» N times.

Manual reject uses `ApplyVerifiedFailure(..., "MANUAL_DEPOSIT_REJECTED")` → `PaymentStatus.Failed`. No restore path existed. `RecordInitiation` already allows Failed → Pending without emitting success.

`cancel_shipment` already allows `Created` with tracking (`CancelPreDispatch`). Reused.

`AssignTracking` still rejects rewrite. New `CorrectTracking` is the pre-dispatch correction path.

`SellerOrder.Cancel` had no prior-status snapshot. Added `CancelledFromStatus` + `LastRestoredAt`.

Cancel releases inventory via `IInventoryDirectory.ReleaseAsync`. Restore reacquires via `FindReservationAsync` + `ReserveAsync`.
