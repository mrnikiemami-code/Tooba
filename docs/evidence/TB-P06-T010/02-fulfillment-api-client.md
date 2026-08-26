# 02 — Fulfillment API client inventory

Shared module: `src/frontend/app/fulfillment/fulfillment-api.ts`

| Surface | Loader / mutation | Host route |
| --- | --- | --- |
| Customer | `loadCustomerFulfillments` | BFF `/api/customer/orders/{checkoutId}/fulfillments` → `/v1/customer/...` |
| Seller list | `loadSellerFulfillments` | `/v1/seller/fulfillments` |
| Seller detail | `loadSellerFulfillmentDetail` | `/v1/seller/fulfillments/{id}` |
| Seller mutations | `sellerMarkProcessing`, `sellerMarkPacked`, `sellerCreateShipment`, `sellerAssignTracking`, `sellerDispatchShipment`, `sellerDeliverShipment` | POST under `/v1/seller/fulfillments/...` |
| Admin list | `loadAdminFulfillments` | `/v1/admin/fulfillments` |
| Admin detail | `loadAdminFulfillmentDetail` | `/v1/admin/fulfillments/{id}` |

No mock shipment/tracking data in mappers.
