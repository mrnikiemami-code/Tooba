# DTO contract — TB-P09-T004

Additive Host fields on seller order detail:
- `AdminOrderLineView.OrderLineId`, `QuantityShipped`, `ImageUrl`, `OperationalStatus`
- `AdminSellerOrderView.FulfillmentId`, `FulfillmentStatus`, `Shipments[]`
- `AdminShipmentView` + line allocations

Composed via `IFulfillmentDirectory.ListForCheckoutAsync` in `AdminPanelComposer` (no cross-module SQL JOIN).

## Gaps deferred to T005 (documented, not invented)
- Line/quantity-scoped create_shipment, pack, dispatch command payloads
- Product image URL on order lines
- Packed/delivered quantity fields beyond shipped
- Return-policy snapshot fields on lines
- Provider-specific shipping forms
- Row-level ops menu enablement
