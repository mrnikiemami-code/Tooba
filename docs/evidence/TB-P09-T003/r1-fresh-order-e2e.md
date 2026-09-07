# R1 fresh order E2E

Checkout `01a0429e-f137-7000-885a-d50dd2bc876d`, actor `01a036c2-…`.

| Step | Ops shown | Execute |
|------|-----------|---------|
| ReadyToFulfill | cancel, mark_processing, mark_packed, create_shipment | mark_processing→packed→create_shipment |
| Shipment Created | assign_tracking (create_shipment gone) | assign_tracking unique ref |
| Tracked | dispatch_shipment | dispatch |
| Dispatched | deliver_shipment | deliver |
| Delivered | request_return (eligible) | request_return |
| Return Requested | approve/reject | approve_return → Completed+refund |

Payment path: existing Paid/ReadyToFulfill seed (wallet/fake success already applied). **No Admin card-to-card confirmation action exists** in current Payment architecture.
