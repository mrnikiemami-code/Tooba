# Discovery

Admin sidebar already had ارسال و تحویل → `/admin/fulfillments` with thin fulfillment list (recipient/city/status).

Gaps vs T008:
- not shipment/fulfillment work-queue oriented
- missing seller, method FA label, tracking, qty, quick filters, projected actions, safe bulk

Approach: extend existing Admin fulfillments surface into `AdminFulfillmentWorkQueueScreen` reusing T004–T007 order operations + fulfillment domain; new server grid engine maps `FulfillmentUnit` → `AdminFulfillmentWorkQueueRow` with batch party/order lookups (no N+1).
