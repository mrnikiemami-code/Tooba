# Fulfillment HTTP Ownership Audit

- Tooba.Fulfillment.Endpoints owns seller/admin/customer/shipping HTTP routes.
- Host maps once via `MapFulfillmentEndpoints()`; `MapShippingServiceEndpoints()` removed.
- Deleted Host `Fulfillment/FulfillmentEndpoints.cs`, `Fulfillment/FulfillmentPanelComposer.cs`, `Admin/ShippingServiceEndpoints.cs`.
- `GET /v1/admin/shipping-methods` moved to `ShippingMethodsEndpoints`; removed from `AdminOrderOperationsEndpoints`.
- State: MODULE_ENDPOINTS
