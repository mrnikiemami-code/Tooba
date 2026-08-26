# 08 — Multi-shipment readiness (TB-P06-T009)

## Design

- One `FulfillmentUnit` supports multiple `Shipment` records (`_shipments` collection).
- Partial line quantities per shipment via `ShipmentItem.Quantity`.
- `FulfillmentItem.QuantityShipped` accumulates across shipments.

## Partial fulfillment

```text
Line qty 2 → Shipment A (qty 1) → Shipment B (qty 1)
Status: Dispatched after first dispatch; Delivered only when all lines fully shipped
```

## Validation

- `Shipment.Create` rejects quantity exceeding remaining per line.
- Overflow attempt throws `InvalidOperationException` (tested).

## Test coverage

- `Multiple_shipments_and_seller_scoped_listing_work_on_postgres`: two shipments for qty-2 line; `second.Shipments.Count == 2`.

## Deferred

- Automatic carrier API integration.
- InTransit as explicit shipment status transition (fulfillment unit uses `InTransit` when partially delivered).
