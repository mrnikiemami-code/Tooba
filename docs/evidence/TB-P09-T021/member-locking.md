# Member locking

While shipment is in an active package with status Created or Dispatched:
- `CancelShipmentAsync` rejects
- `CorrectTrackingAsync` rejects
- `DispatchShipmentAsync` rejects
- `DeliverShipmentAsync` rejects

Machine code: `fulfillment.shipment.locked_by_consolidated_package`

Package orchestration uses internal `DispatchShipmentCoreAsync` / `DeliverShipmentCoreAsync` (same lifecycle, skips lock).

Cancel package (Created only) sets `ReleasedAt` and releases locks. Historical Cancelled package remains.
