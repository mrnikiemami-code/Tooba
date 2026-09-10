# TB-P09-T021 Discovery

## Ownership
- ConsolidatedPackage lives in Fulfillment module, schema `fulfillment`.
- Reuses existing Seller `Shipment` lifecycle; does not replace it.

## Existing seams reused
- `IFulfillmentDirectory` / `FulfillmentDirectory`
- Shipment statuses Created/Dispatched/InTransit/Delivered/Failed/Cancelled
- `DispatchShipmentAsync` / `DeliverShipmentAsync` / `CancelShipmentAsync` / `CorrectTrackingAsync`
- `AbortForCheckoutCancelAsync` (now voids active Created packages first)
- `ShippingMethodRegistry`
- `UuidV7.New()`, `DateTimeOffset`
- Host tests via Testcontainers Postgres (`FulfillmentFoundationTests` pattern)

## No pre-existing ConsolidatedPackage
Fresh aggregate + tables `consolidated_packages` / `consolidated_package_members`.
