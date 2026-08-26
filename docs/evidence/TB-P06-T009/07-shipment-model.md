# 07 — Shipment model (TB-P06-T009)

## Entities

| Entity | Role |
|---|---|
| `Shipment` | Carrier-scoped dispatch unit under one `FulfillmentUnit` |
| `ShipmentItem` | Line + quantity included in shipment |

## Fields (Shipment)

- `ShipmentId`, `FulfillmentId`, `Status`, `CarrierDisplayName`
- `TrackingReference` (required before dispatch)
- `DispatchedAt`, `DeliveredAt`, `CreatedAt`

## Lifecycle API (seller)

| Step | Endpoint |
|---|---|
| Create | `POST /v1/seller/fulfillments/{id}/shipments` |
| Assign tracking | `POST .../shipments/{shipmentId}/tracking` |
| Dispatch | `POST .../shipments/{shipmentId}/dispatch` |
| Deliver | `POST .../shipments/{shipmentId}/deliver` |

## Domain rules

- `CreateShipment` validates quantities against `FulfillmentItem.QuantityOrdered - QuantityShipped`.
- Dispatch updates `FulfillmentItem.QuantityShipped` and fulfillment unit status.
- Unique index on `shipments.tracking_reference` (when not null).

## Outbox events

- `shipment.dispatched.v1` on dispatch
- `shipment.delivered.v1` on deliver
