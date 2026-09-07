# Bulk Actions

`POST /v1/admin/fulfillments/work-queue/bulk` → `AdminFulfillmentWorkQueueComposer.ExecuteBulkAsync` reuses Order Operations / fulfillment commands.

Safe bulk codes: mark_processing, mark_packed, dispatch_shipment, deliver_shipment (no assign_tracking / create_shipment needing unique input).

`AreBulkCompatible`: same SellerPartyId + every row has action code; backend revalidates each item; stops on first failure (no silent partial mutation beyond reported succeeded count).
