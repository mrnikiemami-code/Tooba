# 13 — Tracking update contract (TB-P06-T009)

## API

```http
POST /v1/seller/fulfillments/{fulfillmentId}/shipments/{shipmentId}/tracking
Content-Type: application/json

{ "trackingReference": "TRK-001" }
```

## Domain rules

- Tracking required before dispatch (`EnsureDispatched` throws if missing).
- First assignment sets `TrackingReference`.
- Re-assigning **same** reference: idempotent no-op.
- Re-assigning **different** reference: `InvalidOperationException` (immutable after set).

## Database

- Unique partial index: `ix_shipments_tracking_reference` where `tracking_reference IS NOT NULL`.

## Preconditions

- Shipment must be in `Created` status for first assignment.
- Seller must own fulfillment (`SellerPanelAccess` + `SellerPartyId` match).

## Test coverage

- Assign `TRK-001` → success
- Re-assign `TRK-001` → success (idempotent)
- Assign `TRK-002` → throws
