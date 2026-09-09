# Discovery

Surfaces:
- Route `/admin/fulfillments` → `AdminFulfillmentWorkQueueScreen` (AppDataGrid)
- Query `POST /v1/admin/fulfillments/work-queue/query` (`AdminFulfillmentWorkQueueQueryEngine`)
- Bulk `POST /v1/admin/fulfillments/work-queue/bulk` → same `AdminOrderOperationsComposer` as Order Detail
- Row kebab: `AdminOrderOperationsMenu` scope=`fulfillment-queue`

Defects found:
1. Grid showed raw shipment/checkout/seller GUID slices
2. Quantity cell used `toLocaleString` without `formatQuantityDisplay`; mixed line-count + amount
3. `orderReference` column filter/sort used `CheckoutId`, not `SellerOrder.OrderNumber`
4. Kebab always rendered, including zero-capability rows
5. Cancelled fulfillment still projected forward codes from residual qty
6. `correct_tracking` missing from queue action projection
7. Stale dispatch/void English domain strings could leak through composer catch

Already correct: T008 engine (no N+1 page map), quick filters, bulk same-seller intersection, shipment=one seller, decimal pack via selections, T016 cancel after dispatch.
