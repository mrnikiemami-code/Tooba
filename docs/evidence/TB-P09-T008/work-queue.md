# Work Queue

- Endpoint: `POST /v1/admin/fulfillments/work-queue/query`
- Row: fulfillment-centric `AdminFulfillmentWorkQueueRow` (order ref, seller, shipment, qty, method label, status, tracking, recipient, city, created/updated, AvailableActionCodes)
- FE: AppDataGrid via `AdminFulfillmentWorkQueueScreen`; View → `/admin/orders/{checkoutId}`; kebab `AdminOrderOperationsMenu` scope=`fulfillment-queue`
- Legacy `POST /v1/admin/fulfillments/query` still wired through composer to same work-queue engine for compatibility
