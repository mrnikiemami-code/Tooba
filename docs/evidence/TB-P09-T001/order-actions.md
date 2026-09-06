# Order actions (TB-P09-T001)

Backend projects lifecycle actions per seller order / fulfillment:

- `cancel`, `mark_processing`, `mark_packed`, `create_shipment`, `assign_tracking`, `dispatch_shipment`, `deliver_shipment`
- `request_return`, `approve_return`, `reject_return`, `retry_refund`

GET `/v1/admin/orders/{checkoutId}/operations` returns only currently valid + permitted actions.
POST executes one action; invalid transitions → `order.operation.invalid|failed`.
