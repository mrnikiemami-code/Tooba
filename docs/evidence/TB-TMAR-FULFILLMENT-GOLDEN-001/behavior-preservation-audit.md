# Behavior Preservation Audit

- Seller fulfillment list/get/mutations preserved via same URLs + SellerMutateFulfillmentCommand.
- Admin list/get/grid/work-queue/bulk preserved; grid Normalize moved to AdminFulfillmentGridQueryPolicy (same fields/default sort updatedAt desc).
- Shipping CRUD + ensure-seed + `{ ok: true }` success preserved.
- `GET /v1/admin/shipping-methods` preserved (raw tree JSON via api.From Result value).
- Customer checkout fulfillments + preferred tracking fields preserved; package selector moved to Application.
- Accidental redesign: none.
- State: VERIFIED
