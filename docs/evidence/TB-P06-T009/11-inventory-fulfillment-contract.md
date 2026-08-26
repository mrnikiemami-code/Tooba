# 11 — Inventory–fulfillment contract (TB-P06-T009)

## Interface

```csharp
IFulfillmentInventoryGateway.ConsumeReservationAsync(reservationId, cancellationToken)
```

## Implementation

- `FulfillmentInventoryGateway` delegates to `IInventoryDirectory.ConsumeAsync`.
- Registered in `FulfillmentModule.AddServices`.

## When consumed

- On `DispatchShipmentAsync`, for each shipment line whose fulfillment item is fully shipped (`QuantityShipped >= QuantityOrdered`).
- Skips if `ReservationId` is null or `ReservationConsumed` is already true.
- Sets `FulfillmentItem.ReservationConsumed = true` after successful consume.

## What Fulfillment does NOT do

- No direct Inventory DbContext reference.
- No reservation create/release (owned by Cart/Order/Inventory at checkout).
- No consume at fulfillment creation (only at dispatch).

## Test evidence

- `RecordingInventoryGateway` in tests; dispatch asserts reservation GUID in `ConsumedReservations`.
