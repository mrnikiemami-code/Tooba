# 04 — Fulfillment state machine (TB-P06-T009)

## FulfillmentStatus (unit level)

| State | Meaning |
|---|---|
| `ReadyToFulfill` | Created from Paid SellerOrder |
| `Processing` | Warehouse processing started |
| `Packed` | Packed, ready for shipment creation |
| `Dispatched` | At least one shipment dispatched; not all lines delivered |
| `InTransit` | At least one shipment delivered; remaining lines pending |
| `Delivered` | All lines fully shipped and delivered |
| `Failed` | Terminal operational failure (reserved) |
| `Cancelled` | Terminal cancellation (reserved) |

## Allowed transitions (implemented)

```text
ReadyToFulfill → Processing → Packed
Packed → CreateShipment (status unchanged until dispatch)
Dispatch → Dispatched (or Delivered if all lines complete)
Deliver → InTransit (partial) or Delivered (all lines complete)
```

## ShipmentStatus (shipment level)

| State | Meaning |
|---|---|
| `Created` | Shipment record created |
| `Dispatched` | Tracking assigned + dispatch confirmed |
| `InTransit` | (implicit via fulfillment unit) |
| `Delivered` | Shipment marked delivered |
| `Failed` / `Cancelled` | Terminal (reserved) |

## Guards

- Terminal fulfillment (`Cancelled`, `Failed`) blocks further mutations.
- Dispatch requires non-empty tracking reference.
- Shipment quantity cannot exceed remaining ordered quantity per line.
