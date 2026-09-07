# Action Projection

`AdminFulfillmentQueueFilters.ProjectActionCodes` mirrors T005/T006 domain rules (packable/unpackable/unallocated shipment qty, shipment Created/Dispatched states).

UI kebab loads checkout operations API then filters to fulfillmentId + FULFILLMENT_QUEUE_OPERATION_CODES — eligibility not reinvented in FE.
