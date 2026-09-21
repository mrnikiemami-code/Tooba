# Order Fulfillment Operation Boundary — R3

## Before

- `HostAdminOrderFulfillmentOperations` (Host) implemented `IAdminOrderFulfillmentOperations`
- Wrapped `AdminOrderOperationsComposer.ExecuteAsync` and mapped `PlatformHttpException` / `InvalidOperationException`
- Fulfillment Application depended operationally on Host-registered adapter

## After

- Implementation: `Tooba.Order.Infrastructure.Fulfillment.AdminOrderFulfillmentOperations`
- Supporting Order-owned gates:
  - `AdminOrderFulfillmentCheckoutReader` (OrderDbContext)
  - `AdminOrderFulfillmentPermissionGate` (AccessControl)
- Reuses `IFulfillmentDirectory` + `ShippingMethodRegistry` (no Host types, no composer callback)
- DI registration in `OrderModule` only
- `HostAdminOrderFulfillmentOperations.cs` deleted; Host `Program.cs` registration removed

## Codes preserved

`mark_processing`, `mark_packed`, `create_shipment`, `cancel_shipment`, `assign_tracking`, `dispatch_shipment`, `deliver_shipment`
