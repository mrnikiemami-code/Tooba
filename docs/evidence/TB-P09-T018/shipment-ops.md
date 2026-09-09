# Shipment Ops

Canonical domain. Runtime B/C/D/E on `01a085b3-7510-7000-8812-97a35e347d1f`:
- Pack 0.50 of 1.25
- Create shipment 0.50 — queue `primaryShipmentId` matches Order Detail
- Assign tracking; missing-tracking filter drops row
- `correct_tracking` + pre-dispatch void projected
- Dispatch 0.50; whole-order cancel blocked; stale dispatch/void 400 human FA
